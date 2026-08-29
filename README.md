[![](https://img.shields.io/nuget/v/soenneker.aws.signing.v4.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.aws.signing.v4/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.aws.signing.v4/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.aws.signing.v4/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.aws.signing.v4.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.aws.signing.v4/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Aws.Signing.V4
### A dependency-free .NET implementation of AWS Signature Version 4 request signing.

## Installation

```
dotnet add package Soenneker.Aws.Signing.V4
```

## Registration

```csharp
using Soenneker.Aws.Signing.V4.Registrars;

services.AddAwsSignatureV4SignerAsSingleton();
```

## Usage

```csharp
using Soenneker.Aws.Signing.V4.Abstract;
using Soenneker.Aws.Signing.V4.Dtos;

var request = new AwsSignatureV4PresignRequest
{
    Endpoint = new Uri("https://examplebucket.s3.amazonaws.com"),
    Path = "/private/report.pdf",
    Method = HttpMethod.Get,
    Region = "us-east-1",
    Service = "s3",
    Credentials = new AwsSignatureV4Credentials
    {
        AccessKeyId = accessKeyId,
        SecretAccessKey = secretAccessKey
    },
    Expires = TimeSpan.FromMinutes(15)
};

string url = signer.PresignUrl(request);
```

Signing is performed locally and does not send a request. Existing query parameters, additional signed headers, and temporary credentials with a session token are supported. `SigningTime` can be supplied for deterministic tests.
