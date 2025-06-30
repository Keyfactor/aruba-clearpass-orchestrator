using System.Security.Cryptography.X509Certificates;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.UnitTests;

public class ReenrollmentTests : BaseOrchestratorTest, IClassFixture<SharedTestContext>
{
    private readonly Reenrollment _sut;
    private readonly Mock<IFileServerClient> _fileServerClientMock = new();
    private readonly Mock<IFileServerClientFactory> _fileServerClientFactoryMock = new();
    private readonly Mock<SubmitReenrollmentCSR> _submitReenrollmentCSRMock = new();

    private readonly string _mockCsr =
        @"-----BEGIN CERTIFICATE REQUEST-----MIICvDCCAaQCAQAwHjEcMBoGA1UEAwwTY2xlYXJwYXNzLmxvY2FsaG9zdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALcysdLgGAMiT2DmCOXYwkcJDPRszJ2MKaFc4rioD37hURSSbaR4rmBEpYquA6sNAYqo0DTToVJH+47MSRoY9+masoGlHsQuyZSeVY3G86zjYPLjq93dLaag6KjAMeG/JODYWdYR522T7eR3HLVtezgygY19ngesxw9UZqs2tJNjlt85mVOUowXleuLCKMA6Ko3lufRETZQucwTj0pW/zRIQWXDu2k07f6O8f6v1Iza5EX47sj0Da+/2D6XirpsKyiDeJI7FGX4/9aNOLANNB7Rrh/zc0iBm3Q4d1zWanXFzqtvK+O783torxTgbsYze8jeDwOZMSo0wWdjuyMev06cCAwEAAaBZMFcGCSqGSIb3DQEJDjFKMEgwJwYDVR0lBCAwHgYIKwYBBQUHAwEGCCsGAQUFBwMDBggrBgEFBQcDDjAdBgNVHQ4EFgQUJsrxxWO7tMwM7eqTSD8M/16MMA4wDQYJKoZIhvcNAQENBQADggEBAHzwIw1MHxBgxloNOnIlmVtbEUBtEoC9lNe+N/8IBrFThVlgs7HqGK+UyaA788rWLa9RKA3AmXL8hTG17WyyqxkibSvmSxcZEbvSjEXXXwbzOzEHTL1R/p/r0mPY9JKsUOenxJ8U4FZ3a6DVFTzAlJa4c8j13noAhLgCkHb3zNQzOGb3zI7rAAbTJ+Q4nDNUZeOd5EufuIjSQvvk1Jb7Le6Bf0iwoNPzX/kuGWvLd1xKk/v/fwzqMOcCfu8nua5Y8bYRKdIRJpJFlWahLwJnlrl3TgyuwVdLtnW5m4VIOnagqEc8NTL/l6xMOxAv1N//3vrjJ0y8AbEjr5lyxF8O/Ko=-----END CERTIFICATE REQUEST-----";

    private readonly ArubaCertificateStoreProperties _testStoreProperties = new ()
    {
        ServiceName = "HTTPS(RSA)",
        FileServerType = "S3",
        FileServerHost = "bogus.com",
        FileServerUsername = "hocus",
        FileServerPassword = "pocus",
        DigestAlgorithm = "SHA-256",
    };

    private readonly Dictionary<string, object> _testJobProperties = new ()
    {
        { "subjectText", "CN=com.example" },
        { "keyType", "RSA" },
        { "keySize", 2048 },
    };
    
    public ReenrollmentTests(SharedTestContext context, ITestOutputHelper output) : base(output)
    {
        _fileServerClientFactoryMock
            .Setup(p => p.CreateFileServerClient(It.IsAny<ILogger>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(_fileServerClientMock.Object);
        
        _submitReenrollmentCSRMock.Setup(p => p.Invoke(It.IsAny<string>()))
            .Returns(context.TestCertificate);
        
        _sut = new Reenrollment(Logger, ArubaClientMock.Object, _fileServerClientFactoryMock.Object, PAMResolverMock.Object);
    }

    [Fact]
    public void ExtensionName_MatchesExpectedValue()
    {
        Assert.Equal("Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Reenrollment", _sut.ExtensionName);
    }
    
    [Fact]
    public void ProcessJob_JobPropertiesDoesNotContainSubjectCN_ReturnsJobFailureStatus()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = new Dictionary<string, object>()
            {
                {"keyType", "RSA"},
                {"keySize", 2048},
            }
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        var response = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
    
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Contains("SubjectText was not found in job properties", response.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_JobPropertiesDoesNotContainKeyType_ReturnsJobFailureStatus()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = new Dictionary<string, object>()
            {
                {"subjectText", "CN=com.example"},
                {"keySize", 2048},
            }
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        var response = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
    
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Contains("KeyType was not found in job properties", response.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_JobPropertiesDoesNotContainKeySize_ReturnsJobFailureStatus()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = new Dictionary<string, object>()
            {
                {"subjectText", "CN=com.example"},
                {"keyType", "RSA"},
            }
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        var response = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
    
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Contains("KeySize was not found in job properties", response.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenServerDoesNotExist_ReturnsJobFailureStatus()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("SomethingElse", "abc123");
        var response = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("Unable to find store 'clearpass.localhost' in Aruba system", response.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenCSRGenerationFails_ReturnsJobFailureStatus()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest(It.IsAny<CertificateSubjectInformation>(), It.IsAny<string>(), "2048-bit rsa", "SHA-256"))
            .Throws(new HttpRequestException("oops!"));
        
        var response = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);

        Assert.Equal(OrchestratorJobStatusJobResult.Failure, response.Result);
        Assert.Equal("An error occurred while performing CSR Generation in Aruba. Error: oops!", response.FailureMessage);
    }
    
    [Theory]
    [InlineData("RSA", 2048, "2048-bit rsa")]
    [InlineData("RSA", 3072, "3072-bit rsa")]
    [InlineData("RSA", 4096, "4096-bit rsa")]
    [InlineData("ECC", "P-256", "nist/secg curve over a 256 bit prime field")]
    [InlineData("ECC", "prime256v1", "nist/secg curve over a 256 bit prime field")]
    [InlineData("ECC", "secp256r1", "nist/secg curve over a 256 bit prime field")]
    [InlineData("ECC", "P-256/prime256v1/secp256r1", "nist/secg curve over a 256 bit prime field")]
    [InlineData("ECC", "P-384", "nist/secg curve over a 384 bit prime field")]
    [InlineData("ECC", "secp384r1", "nist/secg curve over a 384 bit prime field")]
    [InlineData("ECC", "P-384/secp384r1", "nist/secg curve over a 384 bit prime field")]
    [InlineData("ECC", "P-521", "nist/secg curve over a 521 bit prime field")]
    [InlineData("ECC", "secp521r1", "nist/secg curve over a 521 bit prime field")]
    [InlineData("ECC", "P-521/secp521r1", "nist/secg curve over a 521 bit prime field")]
    public void ProcessJob_WhenKeyTypeAndSizeAreProvided_CallsArubaApiWithMappedEncryptionAlgorithm(string keyType, object keySize, string expectedEncryptionAlgorithm)
    {
        _testJobProperties["keyType"] = keyType;
        _testJobProperties["keySize"] = keySize;
        
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        
        _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);

        ArubaClientMock.Verify(p => p.CreateCertificateSignRequest(It.IsAny<CertificateSubjectInformation>(), It.IsAny<string>(), expectedEncryptionAlgorithm, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void ProcessJob_WhenUnsupportedKeyTypeAndSizeAreProvided_ReturnsJobFailure()
    {
        _testJobProperties["keyType"] = "RSA";
        _testJobProperties["keySize"] = 1024;
        
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Contains("Unable to map key type 'RSA' and key size '1024' to an accepted Aruba mapping", result.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenCSRIsGenerated_CallsSubmitReenrollmentDelegate()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        
        _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);

        _submitReenrollmentCSRMock.Verify(p => p.Invoke(_mockCsr), Times.Once);
    }
    
    [Fact]
    public void ProcessJob_WhenSubmitReenrollmentReturnsNull_ReturnsJobFailure()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        _submitReenrollmentCSRMock.Setup(p => p.Invoke(It.IsAny<string>())).Returns((X509Certificate2) null);
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal("Command returned a null certificate from the CSR. Did the subject information included in the CSR match the subject information on the ODKG request? Check the Keyfactor Command logs for error information.", result.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenSubmitReenrollmentThrowsAnException_ReturnsJobFailure()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        _submitReenrollmentCSRMock.Setup(p => p.Invoke(It.IsAny<string>())).Throws(new Exception("failed!"));
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal("An error occurred while submitting re-enrollment update: failed!. Check the Keyfactor Command logs for more information.", result.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenFileServerTypeCannotBeMapped_ReturnsJobFailure()
    {
        _testStoreProperties.FileServerType = "RandomString";
        
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal("Unable to find a matching file server type for 'RandomString'", result.FailureMessage);
    }
    
    [Theory]
    [InlineData("S3")]
    // [InlineData("File Server")] // TODO: This needs to be implemented
    public void ProcessJob_WhenFileServerIsResolved_UploadsCertificateToServer(string fileServerType)
    {
        _testStoreProperties.FileServerType = fileServerType;
        
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        
        _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        _fileServerClientMock.Verify(p => p.UploadCertificate("clearpass.localhost_HTTPS(RSA).pfx", It.IsAny<X509Certificate2>()), Times.Once);
    }
    
    [Fact]
    public void ProcessJob_WhenFileServerIsResolved_ButFileServerClientFactoryReturnsNull_UploadsCertificateToServer()
    {
        // This should never happen but doesn't hurt to check this edge case!
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
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
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        _fileServerClientMock.Setup(p => p.UploadCertificate(It.IsAny<string>(), It.IsAny<X509Certificate2>()))
            .Throws(new HttpRequestException("that shouldn't happen"));
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal($"An error occurred while uploading certificate contents to file server: that shouldn't happen", result.FailureMessage);
    }
    
    [Fact]
    public void ProcessJob_WhenFileIsUploaded_UpdatesCertificateInAruba()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        MockUploadCertificateReturns("https://access-your-file-here/mycert.crt");
        
        _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        ArubaClientMock.Verify(p => p.UpdateServerCertificate("fizzbuzz", "HTTPS(RSA)", "https://access-your-file-here/mycert.crt"), Times.Once);
    }
    
    [Fact]
    public void ProcessJob_WhenUpdatingCertificateFails_ReturnsJobFailure()
    {
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        MockUploadCertificateReturns("https://access-your-file-here/mycert.crt");
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
        var config = new ReenrollmentJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                ClientMachine = "example.com",
                Properties = JsonConvert.SerializeObject(_testStoreProperties),
                StorePath = "clearpass.localhost",
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _testJobProperties,
            JobHistoryId = 123
        };
        MockClusterServerReturns("clearpass.localhost", "fizzbuzz");
        MockCertificateSignRequestReturns(_mockCsr);
        MockUploadCertificateReturns("https://access-your-file-here/mycert.crt");
        
        var result = _sut.ProcessJob(config, _submitReenrollmentCSRMock.Object);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Success, result.Result);
        Assert.Null(result.FailureMessage);
        Assert.Equal(123, result.JobHistoryId);
    }

    private void MockCertificateSignRequestReturns(string csr)
    {
        ArubaClientMock.Setup(p => p.CreateCertificateSignRequest(It.IsAny<CertificateSubjectInformation>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CreateCertificateSignRequestResponse()
            {
                CertificateSignRequest = csr
            });
    }

    private void MockUploadCertificateReturns(string url)
    {
        _fileServerClientMock.Setup(p => p.UploadCertificate(It.IsAny<string>(), It.IsAny<X509Certificate2>()))
            .ReturnsAsync(url);
    }
}
