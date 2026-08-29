using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Soenneker.Aws.Signing.V4.Dtos;

/// <summary>
/// Describes an HTTP request to authenticate with AWS Signature Version 4 query parameters.
/// </summary>
public sealed class AwsSignatureV4PresignRequest
{
    /// <summary>
    /// Gets or sets the endpoint containing the scheme, host, and optional port. Any path or query on this URI is ignored.
    /// </summary>
    public required Uri Endpoint { get; init; }

    /// <summary>
    /// Gets or sets the unescaped absolute request path. It must begin with <c>/</c>.
    /// </summary>
    public string Path { get; init; } = "/";

    /// <summary>
    /// Gets or sets the HTTP method to authorize.
    /// </summary>
    public required HttpMethod Method { get; init; }

    /// <summary>
    /// Gets or sets the AWS region included in the credential scope.
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    /// Gets or sets the AWS service code included in the credential scope, such as <c>s3</c> or <c>execute-api</c>.
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// Gets or sets the credentials used to sign the request.
    /// </summary>
    public required AwsSignatureV4Credentials Credentials { get; init; }

    /// <summary>
    /// Gets or sets how long the presigned URL remains valid. The supported range is one second through seven days.
    /// </summary>
    public required TimeSpan Expires { get; init; }

    /// <summary>
    /// Gets or sets the UTC signing time. When omitted, the current UTC time is used.
    /// </summary>
    public DateTimeOffset? SigningTime { get; init; }

    /// <summary>
    /// Gets or sets query parameters that must be included in and protected by the signature.
    /// Parameters may contain duplicate names.
    /// </summary>
    public IReadOnlyCollection<KeyValuePair<string, string>>? QueryParameters { get; init; }

    /// <summary>
    /// Gets or sets additional headers that the caller will send with the request and that must be protected by the signature.
    /// The <c>host</c> header is added automatically.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets or sets the lowercase hexadecimal SHA-256 payload hash, or a service-supported sentinel such as <c>UNSIGNED-PAYLOAD</c>.
    /// </summary>
    public string PayloadHash { get; init; } = "UNSIGNED-PAYLOAD";

    /// <summary>
    /// Gets or sets whether slash characters in <see cref="Path"/> are percent-encoded. This is normally <see langword="false"/> for URI path separators.
    /// </summary>
    public bool EncodePathSlashes { get; init; }
}
