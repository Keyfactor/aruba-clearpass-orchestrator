using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Moq;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.UnitTests;

public class InventoryTests : BaseOrchestratorTest
{
    private readonly Inventory _sut;
    
    private readonly Mock<SubmitInventoryUpdate> _submitInventoryUpdateMock = new();

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
        ArubaClientMock.Setup(p => p.GetClusterServers(It.IsAny<string>()))
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "SomethingElse",
                    ServerUuid = "abc123"
                }
            });
        var response = _sut.ProcessJob(new InventoryJobConfiguration(), _submitInventoryUpdateMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("Fizz buzz", response.FailureMessage);
    }

    [Fact]
    public void ProcessJob_WhenCertificateRetrievalFails_ReturnsJobFailureStatus()
    {
        ArubaClientMock.Setup(p => p.GetClusterServers(It.IsAny<string>()))
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "MyServerName",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.GetServerCertificate("MyServerName", "HTTPS(RSA)"))
            .Throws(new HttpRequestException("That didn't work!"));
        var response = _sut.ProcessJob(new InventoryJobConfiguration(), _submitInventoryUpdateMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("Fizz buzz", response.FailureMessage);
    }

    [Fact]
    public void ProcessJob_WhenSuccessful_SubmitsCertificateToInventory()
    {
        ArubaClientMock.Setup(p => p.GetClusterServers(It.IsAny<string>()))
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "MyServerName",
                    ServerUuid = "fizzbuzz"
                }
            });
        
        _sut.ProcessJob(new InventoryJobConfiguration(), _submitInventoryUpdateMock.Object);

        _submitInventoryUpdateMock.Verify(p => p.Invoke(It.IsAny<IEnumerable<CurrentInventoryItem>>()), Times.Once);
    }

    [Fact]
    public void ProcessJob_WhenSuccessful_ReturnsSuccessfulJob()
    {
        ArubaClientMock.Setup(p => p.GetClusterServers(It.IsAny<string>()))
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "MyServerName",
                    ServerUuid = "fizzbuzz"
                }
            });
        var result = _sut.ProcessJob(new InventoryJobConfiguration(), _submitInventoryUpdateMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Success, result.Result);
        Assert.Equal("", result.FailureMessage);
    }
}
