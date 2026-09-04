using Soenneker.Aws.Signing.V4.Abstract;
using Soenneker.Aws.Signing.V4.Dtos;
using Soenneker.Hashing.Sha256;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Soenneker.Utils.PooledStringBuilders;

namespace Soenneker.Aws.Signing.V4;

/// <inheritdoc cref="IAwsSignatureV4Signer" />
public sealed class AwsSignatureV4Signer : IAwsSignatureV4Signer
{
    private static readonly Sha256HashingUtil _sha256 = new();

    private const string _algorithm = "AWS4-HMAC-SHA256";
    private const string _requestType = "aws4_request";
    private const string _hostHeader = "host";

    private static readonly TimeSpan _minimumExpiration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _maximumExpiration = TimeSpan.FromDays(7);

    public string PresignUrl(AwsSignatureV4PresignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Endpoint);
        ArgumentNullException.ThrowIfNull(request.Method);
        ArgumentNullException.ThrowIfNull(request.Credentials);
        Validate(request);

        DateTimeOffset signingTime = request.SigningTime?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
        var date = signingTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var timestamp = signingTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var scope = $"{date}/{request.Region}/{request.Service}/{_requestType}";
        string host = request.Endpoint.Authority;
        string canonicalUri = UriEncode(request.Path, request.EncodePathSlashes);

        (string canonicalHeaders, string signedHeaders) = BuildHeaders(host, request.Headers);
        List<KeyValuePair<string, string>> query = BuildSigningQuery(request, scope, timestamp, signedHeaders);
        string canonicalQuery = BuildCanonicalQuery(query);
        var canonicalRequest =
            $"{request.Method.Method.ToUpperInvariant()}\n{canonicalUri}\n{canonicalQuery}\n{canonicalHeaders}\n{signedHeaders}\n{request.PayloadHash}";
        var stringToSign = $"{_algorithm}\n{timestamp}\n{scope}\n{Sha256Hex(canonicalRequest)}";
        string signature = CalculateSignature(request.Credentials.SecretAccessKey, date, request.Region,
            request.Service, stringToSign);

        return
            $"{request.Endpoint.Scheme}://{request.Endpoint.Authority}{canonicalUri}?{canonicalQuery}&X-Amz-Signature={signature}";
    }

    private static void Validate(AwsSignatureV4PresignRequest request)
    {
        if (!request.Endpoint.IsAbsoluteUri || request.Endpoint.Scheme is not ("http" or "https"))
            throw new ArgumentException("The endpoint must be an absolute HTTP or HTTPS URI.", nameof(request));

        if (string.IsNullOrEmpty(request.Path) || request.Path[0] != '/')
            throw new ArgumentException("The path must be an absolute path beginning with '/'.", nameof(request));

        ArgumentException.ThrowIfNullOrWhiteSpace(request.Method.Method);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Region);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Service);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PayloadHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Credentials.AccessKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Credentials.SecretAccessKey);

        if (request.Expires < _minimumExpiration || request.Expires > _maximumExpiration)
            throw new ArgumentOutOfRangeException(nameof(request), request.Expires,
                "The expiration must be between one second and seven days.");

        if (request.Headers?.Keys.Any(static key =>
                string.Equals(key, "authorization", StringComparison.OrdinalIgnoreCase)) == true)
            throw new ArgumentException("The Authorization header cannot be included in a presigned URL.",
                nameof(request));

        if (request.QueryParameters is not null)
        {
            foreach ((string key, string value) in request.QueryParameters)
            {
                ArgumentNullException.ThrowIfNull(key);
                ArgumentNullException.ThrowIfNull(value);

                if (key.StartsWith("X-Amz-", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("X-Amz-* query parameters are reserved for the signer.",
                        nameof(request));
            }
        }
    }

    private static List<KeyValuePair<string, string>> BuildSigningQuery(AwsSignatureV4PresignRequest request,
        string scope, string timestamp, string signedHeaders)
    {
        List<KeyValuePair<string, string>> result = request.QueryParameters is null
            ? []
            : [.. request.QueryParameters];
        result.Add(new KeyValuePair<string, string>("X-Amz-Algorithm", _algorithm));
        result.Add(new KeyValuePair<string, string>("X-Amz-Credential", $"{request.Credentials.AccessKeyId}/{scope}"));
        result.Add(new KeyValuePair<string, string>("X-Amz-Date", timestamp));
        result.Add(new KeyValuePair<string, string>("X-Amz-Expires",
            ((long)request.Expires.TotalSeconds).ToString(CultureInfo.InvariantCulture)));
        result.Add(new KeyValuePair<string, string>("X-Amz-SignedHeaders", signedHeaders));

        if (!string.IsNullOrWhiteSpace(request.Credentials.SessionToken))
            result.Add(new KeyValuePair<string, string>("X-Amz-Security-Token", request.Credentials.SessionToken));

        return result;
    }

    private static (string CanonicalHeaders, string SignedHeaders) BuildHeaders(string host,
        IReadOnlyDictionary<string, string>? headers)
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            [_hostHeader] = host
        };

        if (headers is not null)
        {
            foreach ((string name, string value) in headers)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);
                ArgumentNullException.ThrowIfNull(value);

                string normalizedName = name.Trim().ToLowerInvariant();
                string normalizedValue = NormalizeHeaderValue(value);

                if (normalizedName == _hostHeader && !string.Equals(normalizedValue, host, StringComparison.Ordinal))
                    throw new ArgumentException("The supplied host header does not match the endpoint.",
                        nameof(headers));

                values[normalizedName] = normalizedValue;
            }
        }

        string canonicalHeaders = string.Concat(values.Select(static pair => $"{pair.Key}:{pair.Value}\n"));
        string signedHeaders = string.Join(';', values.Keys);
        return (canonicalHeaders, signedHeaders);
    }

    private static string NormalizeHeaderValue(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string BuildCanonicalQuery(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join('&',
            parameters
                .Select(static pair =>
                    new KeyValuePair<string, string>(UriEncode(pair.Key, true), UriEncode(pair.Value, true)))
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .ThenBy(static pair => pair.Value, StringComparer.Ordinal)
                .Select(static pair => $"{pair.Key}={pair.Value}"));
    }

    private static string UriEncode(string value, bool encodeSlash)
    {
        int maximumByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        byte[]? rented = null;
        Span<byte> bytes = maximumByteCount <= 512
            ? stackalloc byte[maximumByteCount]
            : (rented = ArrayPool<byte>.Shared.Rent(maximumByteCount));

        try
        {
            int byteCount = Encoding.UTF8.GetBytes(value, bytes);
            using var builder = new PooledStringBuilder(byteCount);

            foreach (byte valueByte in bytes[..byteCount])
            {
                var character = (char)valueByte;
                bool isUnreserved = character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-'
                    or '_' or '.' or '~';

                if (isUnreserved || (!encodeSlash && character == '/'))
                    builder.Append(character);
                else
                {
                    builder.Append('%');
                    builder.Append(valueByte.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            return builder.ToString();
        }
        finally
        {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static string CalculateSignature(string secretAccessKey, string date, string region, string service,
        string stringToSign)
    {
        byte[] rootKey = Encoding.UTF8.GetBytes($"AWS4{secretAccessKey}");
        byte[] dateKey = HmacSha256(rootKey, date);
        byte[] regionKey = HmacSha256(dateKey, region);
        byte[] serviceKey = HmacSha256(regionKey, service);
        byte[] signingKey = HmacSha256(serviceKey, _requestType);

        try
        {
            return Convert.ToHexStringLower(HmacSha256(signingKey, stringToSign));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rootKey);
            CryptographicOperations.ZeroMemory(dateKey);
            CryptographicOperations.ZeroMemory(regionKey);
            CryptographicOperations.ZeroMemory(serviceKey);
            CryptographicOperations.ZeroMemory(signingKey);
        }
    }

    private static string Sha256Hex(string value) => _sha256.Hash(value);

    private static byte[] HmacSha256(byte[] key, string value) =>
        HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value));
}
