using System.Security.Cryptography.X509Certificates;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.UnitTests;

public class ReenrollmentTests : BaseOrchestratorTest
{
    private readonly Reenrollment _sut;
    private readonly Mock<IFileServerClient> _fileServerClientMock = new();
    private readonly Mock<IFileServerClientFactory> _fileServerClientFactoryMock = new();
    private readonly Mock<SubmitReenrollmentCSR> _submitReenrollmentCSRMock = new();

    public ReenrollmentTests(ITestOutputHelper output) : base(output)
    {
        _fileServerClientFactoryMock
            .Setup(p => p.CreateFileServerClient(It.IsAny<ILogger>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(_fileServerClientMock.Object);
        _sut = new Reenrollment(Logger, ArubaClientMock.Object, _fileServerClientFactoryMock.Object, PAMResolverMock.Object);
    }

    [Fact]
    public void ExtensionName_MatchesExpectedValue()
    {
        Assert.Equal("Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Reenrollment", _sut.ExtensionName);
    }
    
    [Fact]
    public void ProcessJob_WhenServerDoesNotExist_ReturnsJobFailureStatus()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "SomethingElse",
                    ServerUuid = "abc123"
                }
            });
        var response = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("Unable to find store 'clearpass.localhost' in Aruba system", response.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenCSRGenerationFails_ReturnsJobFailureStatus()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest("clearpass.localhost", "2048-bit rsa", "SHA-256"))
            .Throws(new HttpRequestException("oops!"));
        
        var response = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("An error occurred while performing CSR Generation in Aruba. Error: oops!", response.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenCSRIsGenerated_CallsSubmitReenrollmentDelegate()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest("clearpass.localhost", "2048-bit rsa", "SHA-256"))
            .Returns(new CreateCertificateSignRequestResponse()
            {
                CertificateSignRequest = "-----BEGIN CERTIFICATE REQUEST-----\\nMIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIw\\n-----END CERTIFICATE REQUEST-----\\n"
            });
        
        _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);

        _submitReenrollmentCSRMock.Verify(p => p.Invoke("-----BEGIN CERTIFICATE REQUEST-----\\nMIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIw\\n-----END CERTIFICATE REQUEST-----\\n"), Times.Once);
    }
    
    [Fact]
    public void ProcessJob_WhenFileServerTypeCannotBeMapped_ReturnsJobFailure()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "RandomString",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest("clearpass.localhost", "2048-bit rsa", "SHA-256"))
            .Returns(new CreateCertificateSignRequestResponse()
            {
                CertificateSignRequest = "-----BEGIN CERTIFICATE REQUEST-----\\nMIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIw\\n-----END CERTIFICATE REQUEST-----\\n"
            });
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal("Unable to find a matching file server type for 'RandomString'", result.FailureMessage);
    }
    
    [Theory]
    [InlineData("S3")]
    // [InlineData("File Server")] // TODO: This needs to be implemented
    public void ProcessJob_WhenFileServerIsResolved_UploadsCertificateToServer(string fileServerType)
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = fileServerType,
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest("clearpass.localhost", "2048-bit rsa", "SHA-256"))
            .Returns(new CreateCertificateSignRequestResponse()
            {
                CertificateSignRequest = "-----BEGIN CERTIFICATE REQUEST-----\\nMIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIw\\n-----END CERTIFICATE REQUEST-----\\n"
            });
        
        _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        _fileServerClientMock.Verify(p => p.UploadCertificate("clearpass.localhost_HTTPS(RSA).pfx", It.IsAny<X509Certificate2>()), Times.Once);
    }
    
    [Fact]
    public void ProcessJob_WhenFileServerIsResolved_ButFileServerClientFactoryReturnsNull_UploadsCertificateToServer()
    {
        // This should never happen but doesn't hurt to check this edge case!
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        _fileServerClientFactoryMock
            .Setup(p => p.CreateFileServerClient(It.IsAny<ILogger>(),It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns((IFileServerClient) null); // If we made it here, we did something wrong!

        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal($"Matching file server type 'S3' was found but FileServerClientFactory did not provide a FileServerClient reference", result.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenFileServerIsResolved_FileUploadFails_ReturnsJobFailure()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest("clearpass.localhost", "2048-bit rsa", "SHA-256"))
            .Returns(new CreateCertificateSignRequestResponse()
            {
                CertificateSignRequest = "-----BEGIN CERTIFICATE REQUEST-----\\nMIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIw\\n-----END CERTIFICATE REQUEST-----\\n"
            });
        _fileServerClientMock.Setup(p => p.UploadCertificate(It.IsAny<string>(), It.IsAny<X509Certificate2>()))
            .Throws(new HttpRequestException("that shouldn't happen"));
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal($"An error occurred while uploading certificate contents to file server: that shouldn't happen", result.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenFileIsUploaded_UpdatesCertificateInAruba()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest("clearpass.localhost", "2048-bit rsa", "SHA-256"))
            .Returns(new CreateCertificateSignRequestResponse()
            {
                CertificateSignRequest = "-----BEGIN CERTIFICATE REQUEST-----\\nMIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIw\\n-----END CERTIFICATE REQUEST-----\\n"
            });
        _fileServerClientMock.Setup(p => p.UploadCertificate(It.IsAny<string>(), It.IsAny<X509Certificate2>()))
            .ReturnsAsync("https://access-your-file-here/mycert.crt");
        
        _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        ArubaClientMock.Verify(p => p.UpdateServerCertificate("fizzbuzz", "HTTPS(RSA)", "https://access-your-file-here/mycert.crt"), Times.Once);
    }
    
    [Fact]
    public void ProcessJob_WhenUpdatingCertificateFails_ReturnsJobFailure()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest("clearpass.localhost", "2048-bit rsa", "SHA-256"))
            .Returns(new CreateCertificateSignRequestResponse()
            {
                CertificateSignRequest = "-----BEGIN CERTIFICATE REQUEST-----\\nMIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIw\\n-----END CERTIFICATE REQUEST-----\\n"
            });
        _fileServerClientMock.Setup(p => p.UploadCertificate(It.IsAny<string>(), It.IsAny<X509Certificate2>()))
            .ReturnsAsync("https://access-your-file-here/mycert.crt");
        ArubaClientMock
            .Setup(p => p.UpdateServerCertificate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new HttpRequestException("uh oh"));
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal("An error occurred while updating certificate in Aruba: uh oh", result.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenSuccessful_ReturnsSuccessfulJob()
    {
        var properties = new ArubaCertificateStoreProperties()
        {
            ServiceName = "HTTPS(RSA)",
            FileServerType = "S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        var config = new ReenrollmentJobConfiguration()
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
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .Returns(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = "clearpass.localhost",
                    ServerUuid = "fizzbuzz"
                }
            });
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest("clearpass.localhost", "2048-bit rsa", "SHA-256"))
            .Returns(new CreateCertificateSignRequestResponse()
            {
                CertificateSignRequest = "-----BEGIN CERTIFICATE REQUEST-----\\nMIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIw\\n-----END CERTIFICATE REQUEST-----\\n"
            });
        _fileServerClientMock.Setup(p => p.UploadCertificate(It.IsAny<string>(), It.IsAny<X509Certificate2>()))
            .ReturnsAsync("https://access-your-file-here/mycert.crt");
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Success, result.Result);
        Assert.Null(result.FailureMessage);
    }
}
