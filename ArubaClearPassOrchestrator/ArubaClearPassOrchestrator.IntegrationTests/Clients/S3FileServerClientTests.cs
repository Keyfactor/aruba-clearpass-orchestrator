using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients;

public class S3FileServerClientTests : BaseFileServerClientTest<S3FileServerClient>
{
    public S3FileServerClientTests(ITestOutputHelper output) : base(
        output: output,
        type: "S3",
        hostname: Environment.GetEnvironmentVariable("S3_BUCKET_NAME"),
        username: Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
        password: Environment.GetEnvironmentVariable("S3_SECRET_ACCESS_KEY"))
    {
        SkipTestUnlessEnvEnabled("S3_RUN_TESTS");
    }
    
    public async Task UploadCertificate_WhenACertificateIsUploaded_ReturnsAPreSignedUrl()
    {
        var certificate = CertificateGenerator.GenerateCertificate("com.example", "foobarbaz", true);
        var certificateUrl = await Client.UploadCertificate("test_example.pfx", certificate);
        Assert.NotNull(certificateUrl);
        Logger.LogInformation($"Certificate URL: {certificateUrl}");
    }
    
    
}
