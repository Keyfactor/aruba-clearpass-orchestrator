using ArubaClearPassOrchestrator.Models.Aruba.ServerCert;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Moq;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.UnitTests;

public class InventoryTests : BaseOrchestratorTest
{
    private readonly Inventory _sut;
    private readonly Mock<SubmitInventoryUpdate> _submitInventoryUpdateMock = new();

    private readonly string _mockCertificate =
        "-----BEGIN CERTIFICATE-----\\nMIIGnjCCBIagAwIBAgIUWVsbKtLOVZcDGrUO29kcrK02p2wwDQ\\n-----END CERTIFICATE-----";

    public InventoryTests(ITestOutputHelper output) : base(output)
    {
        _sut = new Inventory(Logger, ArubaClientMock.Object, PAMResolverMock.Object);
    }

    [Fact]
    public void ExtensionName_MatchesExpectedValue()
    {
        Assert.Equal("Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Inventory", _sut.ExtensionName);
    }

    [Fact]
    public void ProcessJob_WhenServerDoesNotExist_ReturnsJobFailureStatus()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
        };
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(properties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
        };
        MockClusterServerReturns("SomethingElse", "abc123");
        var response = _sut.ProcessJob(config, _submitInventoryUpdateMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("Unable to find store 'clearpass.localhost' in Aruba system", response.FailureMessage);
    }

    [Fact]
    public void ProcessJob_WhenCertificateRetrievalFails_ReturnsJobFailureStatus()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
        };
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(properties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        ArubaClientMock.Setup(p => p.GetServerCertificate("fizzbuzz", "HTTPS(RSA)"))
            .Throws(new HttpRequestException("That didn't work!"));
        var response = _sut.ProcessJob(config, _submitInventoryUpdateMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("An unexpected error occurred in inventory job: That didn't work!", response.FailureMessage);
    }

    [Fact]
    public void ProcessJob_WhenSuccessful_SubmitsCertificateToInventory()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
        };
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(properties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockServerCertificateReturns(_mockCertificate);
        
        _sut.ProcessJob(config, _submitInventoryUpdateMock.Object);

        _submitInventoryUpdateMock.Verify(p => p.Invoke(It.IsAny<IEnumerable<CurrentInventoryItem>>()), Times.Once);
    }

    [Fact]
    public void ProcessJob_WhenSuccessful_ReturnsSuccessfulJob()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
        };
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(properties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobHistoryId = 123,
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockServerCertificateReturns(_mockCertificate);
        var result = _sut.ProcessJob(config, _submitInventoryUpdateMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Success, result.Result);
        Assert.Null(result.FailureMessage);
        Assert.Equal(123, result.JobHistoryId);
    }

    private void MockServerCertificateReturns(string certificate)
    {
        ArubaClientMock.Setup(p => p.GetServerCertificate("fizzbuzz", "HTTPS(RSA)")).ReturnsAsync(
            new GetServerCertificateResponse()
            {
                CertFile = _mockCertificate
            });
    }
}
