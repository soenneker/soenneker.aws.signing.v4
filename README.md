[![](https://img.shields.io/nuget/v/soenneker.aws.signing.v4.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.aws.signing.v4/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.aws.signing.v4/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.aws.signing.v4/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.aws.signing.v4.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.aws.signing.v4/)

# Soenneker.Aws.Signing.V4

A dependency-free AWS Signature Version 4 implementation for creating presigned HTTP URLs locally.

## Installation

```bash
dotnet add package Soenneker.Aws.Signing.V4
```

## Usage

```csharp
using Soenneker.Aws.Signing.V4;
using Soenneker.Aws.Signing.V4.Dtos;

var signer = new AwsSignatureV4Signer();

string url = signer.PresignUrl(new AwsSignatureV4PresignRequest
{
    Endpoint = new Uri("https://examplebucket.s3.amazonaws.com"),
    Path = "/private/report.pdf",
    Method = HttpMethod.Get,
    Region = "us-east-1",
    Service = "s3",
    Credentials = new AwsSignatureV4Credentials
    {
        AccessKeyId = accessKeyId,
        SecretAccessKey = secretAccessKey,
        SessionToken = sessionToken
    },
    Expires = TimeSpan.FromMinutes(15)
});

using HttpResponseMessage response = await httpClient.GetAsync(
    url,
    cancellationToken);
```

Signing does not send a request. The caller is responsible for sending the same method, path, query parameters, and signed headers represented by the request.

## Dependency injection

```csharp
using Soenneker.Aws.Signing.V4.Registrars;

builder.Services.AddAwsSignatureV4SignerAsSingleton();
```

Inject `IAwsSignatureV4Signer` into the consuming service.

## Request details

- `Expires` must be between one second and seven days.
- `Path` must start with `/` and is treated as an unescaped path. Slash characters remain path separators unless `EncodePathSlashes` is enabled.
- Existing `QueryParameters` are included in the signature. `X-Amz-*` names are reserved and rejected.
- `Headers` contains additional headers to sign. The caller must send the same normalized values; `host` is added automatically and `authorization` is rejected.
- `PayloadHash` defaults to `UNSIGNED-PAYLOAD`. Confirm that the target AWS service and operation support it.
- Set `SigningTime` for deterministic tests or clock-controlled signing.

Presigned URLs are bearer credentials until they expire. Do not log them, expose them in analytics, or return longer expirations than the recipient needs. Credential values remain the caller's responsibility and should come from a secure provider.
