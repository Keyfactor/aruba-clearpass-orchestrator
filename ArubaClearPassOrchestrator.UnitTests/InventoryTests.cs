// Copyright 2025 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        };
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(properties),
                StorePath = "clearpass.localhost;HTTPS(RSA)",
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
    public void ProcessJob_WhenStorePathDoesNotIncludeServiceName_ReturnsJobFailureStatus()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
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
        var response = _sut.ProcessJob(config, _submitInventoryUpdateMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("Service name could not be parsed from store path 'clearpass.localhost'", response.FailureMessage);
    }

    [Fact]
    public void ProcessJob_WhenCertificateRetrievalFails_ReturnsJobFailureStatus()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
        };
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(properties),
                StorePath = "clearpass.localhost;HTTPS(RSA)",
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
        };
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(properties),
                StorePath = "clearpass.localhost;HTTPS(RSA)",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockServerCertificateReturns(_mockCertificate);

        var expected = new CurrentInventoryItem()
        {
            Alias = "clearpass.localhost HTTPS(RSA)",
            ItemStatus = OrchestratorInventoryItemStatus.Unknown,
            PrivateKeyEntry = false,
            Certificates = new[] { _mockCertificate }
        };
        
        _sut.ProcessJob(config, _submitInventoryUpdateMock.Object);

        _submitInventoryUpdateMock.Verify(p => p.Invoke(It.IsAny<IEnumerable<CurrentInventoryItem>>()), Times.Once);
        var invocation = (List<CurrentInventoryItem>) _submitInventoryUpdateMock.Invocations[0].Arguments[0];
        Assert.Single(invocation);
            
        var submittedInventory = invocation.Single();
        
        Assert.Equal(expected.Alias, submittedInventory.Alias);
        Assert.Equal(expected.ItemStatus, submittedInventory.ItemStatus);
        Assert.Equal(expected.PrivateKeyEntry, submittedInventory.PrivateKeyEntry);
        Assert.Equal(expected.Certificates, submittedInventory.Certificates);
    }

    [Fact]
    public void ProcessJob_WhenSuccessful_ReturnsSuccessfulJob()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
        };
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(properties),
                StorePath = "clearpass.localhost;HTTPS(RSA)",
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
