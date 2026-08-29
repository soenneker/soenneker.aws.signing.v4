using Soenneker.Aws.Signing.V4.Dtos;
using System;

namespace Soenneker.Aws.Signing.V4.Abstract;

/// <summary>
/// A dependency-free .NET implementation of AWS Signature Version 4 request signing.
/// </summary>
public interface IAwsSignatureV4Signer
{
    /// <summary>
    /// Creates an HTTP URL authenticated with AWS Signature Version 4 query parameters.
    /// Signing is performed locally and does not send a request.
    /// </summary>
    /// <param name="request">The request components, credentials, and expiration to sign.</param>
    /// <returns>The absolute presigned URL.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> or one of its required reference members is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A required string is blank, the endpoint is not an absolute HTTP or HTTPS URI, the path is invalid, or a reserved signing parameter or header was supplied.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The expiration is shorter than one second or longer than seven days.</exception>
    string PresignUrl(AwsSignatureV4PresignRequest request);
}
