using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Tests.Common.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients;

[Trait("Category", "Integration")]
public class ArubaClientTests : BaseIntegrationTest
{
    private readonly IArubaClient _sut;

    private readonly string _serverName;
    private readonly string _serverUuid;
    private readonly string _serviceName;
    
    public ArubaClientTests(ITestOutputHelper output)
    {
        SkipTestUnlessEnvEnabled("ARUBA_RUN_TESTS");
        
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(new XunitLoggerProvider(output));
        });
        
        var arubaHost = Environment.GetEnvironmentVariable("ARUBA_HOST");
        var arubaClientId = Environment.GetEnvironmentVariable("ARUBA_CLIENT_ID");
        var arubaClientSecret = Environment.GetEnvironmentVariable("ARUBA_CLIENT_SECRET");
        
        _serverName = Environment.GetEnvironmentVariable("ARUBA_SERVER_NAME");
        _serverUuid = Environment.GetEnvironmentVariable("ARUBA_SERVER_UUID");
        _serviceName = Environment.GetEnvironmentVariable("ARUBA_SERVICE_NAME");

        var logger = loggerFactory.CreateLogger<ArubaClientTests>();
        _sut = new ArubaClient(logger, false, arubaHost, arubaClientId, arubaClientSecret);
    }

    [Fact]
    public async Task GetClusterServers_WhenCalled_ShouldReturnANonEmptyList()
    {
        var result = await _sut.GetClusterServers();
        Assert.NotEmpty(result);
    }
    
    [Fact]
    public async Task GetServerCertificate_WhenCalled_ShouldReturnAServerCertificate()
    {
        var result = await _sut.GetServerCertificate(_serverUuid, _serviceName);
        Assert.NotNull(result);
        Assert.NotEmpty(result.CertFile);
    }
    
    [Fact]
    public async Task CreateCertificateSignRequest_WhenCalled_ShouldReturnACertificateSigningRequest()
    {
        var result = await _sut.CreateCertificateSignRequest(_serverName, "2048-bit rsa", "SHA-512");
        Assert.NotNull(result);
        Assert.NotEmpty(result.CertificateSignRequest);
    }

    [Fact]
    public async Task EnableServerCertificate_WhenCalled_ShouldNotThrowException()
    {
        await _sut.EnableServerCertificate(_serverUuid, _serviceName);
    }
    
    [Fact]
    public async Task DisableServerCertificate_WhenCalled_ShouldNotThrowException()
    {
        await _sut.DisableServerCertificate(_serverUuid, _serviceName);
        
        // Make sure we enable the server certificate after we're done
        await _sut.EnableServerCertificate(_serverUuid, _serviceName);
    }
}
