using Soenneker.Aws.Signing.V4.Abstract;
using Soenneker.Aws.Signing.V4.Dtos;
using Soenneker.Tests.HostedUnit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Soenneker.Aws.Signing.V4.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class AwsSignatureV4SignerTests : HostedUnitTest
{
    private readonly IAwsSignatureV4Signer _util;

    public AwsSignatureV4SignerTests(Host host) : base(host)
    {
        _util = Resolve<IAwsSignatureV4Signer>(true);
    }

    [Test]
    public async Task PresignUrl_should_match_Aws_S3_test_vector()
    {
        var request = new AwsSignatureV4PresignRequest
        {
            Endpoint = new Uri("https://examplebucket.s3.amazonaws.com"),
            Path = "/test.txt",
            Method = HttpMethod.Get,
            Region = "us-east-1",
            Service = "s3",
            Credentials = new AwsSignatureV4Credentials
            {
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
            },
            Expires = TimeSpan.FromDays(1),
            SigningTime = new DateTimeOffset(2013, 5, 24, 0, 0, 0, TimeSpan.Zero)
        };

        string result = _util.PresignUrl(request);

        const string expected =
            "https://examplebucket.s3.amazonaws.com/test.txt?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAIOSFODNN7EXAMPLE%2F20130524%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20130524T000000Z&X-Amz-Expires=86400&X-Amz-SignedHeaders=host&X-Amz-Signature=aeeed9bbccd4d02ee5c0109b86d86835f995330da4c265957d157751f604d404";

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task PresignUrl_should_include_existing_query_headers_and_session_token()
    {
        var request = new AwsSignatureV4PresignRequest
        {
            Endpoint = new Uri("https://storage.example.com"),
            Path = "/bucket/report 2026.pdf",
            Method = HttpMethod.Get,
            Region = "auto",
            Service = "s3",
            Credentials = new AwsSignatureV4Credentials
            {
                AccessKeyId = "temporary-key",
                SecretAccessKey = "temporary-secret",
                SessionToken = "temporary/token+value"
            },
            Expires = TimeSpan.FromMinutes(5),
            SigningTime = new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero),
            QueryParameters =
            [
                new KeyValuePair<string, string>("response-content-disposition", "attachment; filename=report.pdf")
            ],
            Headers = new Dictionary<string, string>
            {
                ["X-Custom-Header"] = "  one   two  "
            }
        };

        string result = _util.PresignUrl(request);

        await Assert.That(result).StartsWith("https://storage.example.com/bucket/report%202026.pdf?");
        await Assert.That(result).Contains("response-content-disposition=attachment%3B%20filename%3Dreport.pdf");
        await Assert.That(result).Contains("X-Amz-Security-Token=temporary%2Ftoken%2Bvalue");
        await Assert.That(result).Contains("X-Amz-SignedHeaders=host%3Bx-custom-header");
    }

    [Test]
    public async Task PresignUrl_should_reject_reserved_query_parameters()
    {
        var request = new AwsSignatureV4PresignRequest
        {
            Endpoint = new Uri("https://storage.example.com"),
            Method = HttpMethod.Get,
            Region = "auto",
            Service = "s3",
            Credentials = new AwsSignatureV4Credentials
            {
                AccessKeyId = "key",
                SecretAccessKey = "secret"
            },
            Expires = TimeSpan.FromMinutes(5),
            QueryParameters = [new KeyValuePair<string, string>("X-Amz-Date", "forged")]
        };

        await Assert.ThrowsAsync<ArgumentException>(() => Task.FromResult(_util.PresignUrl(request)));
    }
}
