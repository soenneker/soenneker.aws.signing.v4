namespace Soenneker.Aws.Signing.V4.Dtos;

/// <summary>
/// Credentials used to create an AWS Signature Version 4 signature.
/// </summary>
public sealed class AwsSignatureV4Credentials
{
    /// <summary>
    /// Gets or sets the access key identifier.
    /// </summary>
    public required string AccessKeyId { get; init; }

    /// <summary>
    /// Gets or sets the secret access key.
    /// </summary>
    public required string SecretAccessKey { get; init; }

    /// <summary>
    /// Gets or sets the optional session token used with temporary credentials.
    /// </summary>
    public string? SessionToken { get; init; }
}
