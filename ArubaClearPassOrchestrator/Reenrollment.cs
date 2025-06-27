using System.Security.Cryptography.X509Certificates;
using System.Text;
using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Exception = System.Exception;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator;

public class Reenrollment : BaseOrchestratorJob, IReenrollmentJobExtension
{
    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Reenrollment";
    private readonly ILogger _logger = LogHandler.GetClassLogger<Reenrollment>();

    private IArubaClient? _arubaClient;
    private IFileServerClientFactory _fileServerClientFactory;
    private readonly IPAMSecretResolver _resolver;
    
    public Reenrollment(IPAMSecretResolver resolver)
    {
        _logger = LogHandler.GetClassLogger<Reenrollment>();
        _resolver = resolver;
        _fileServerClientFactory = new FileServerClientFactory();
    }

    public Reenrollment(ILogger logger, IArubaClient arubaClient, IFileServerClientFactory fileServerClientFactory, IPAMSecretResolver resolver)
    {
        _logger = logger;
        _arubaClient = arubaClient;
        _fileServerClientFactory = fileServerClientFactory;
        _resolver = resolver;
    }
    
    public JobResult ProcessJob(ReenrollmentJobConfiguration jobConfiguration, SubmitReenrollmentCSR submitReenrollmentUpdate)
    {
        try
        {
            _logger.MethodEntry();
            _logger.LogInformation("Starting Re-Enrollment (ODKG) job");
            
            var properties =
                JsonConvert.DeserializeObject<ArubaCertificateStoreProperties>(jobConfiguration.CertificateStoreDetails
                    .Properties);
            
            var host = jobConfiguration.CertificateStoreDetails.ClientMachine;
            var servername = jobConfiguration.CertificateStoreDetails.StorePath;
            var serviceName = properties.ServiceName;
            
            _logger.LogInformation($"Re-Enrollment job target: Host: {host}, Server Name: {servername}, Service Name: {serviceName}");
            _logger.LogDebug($"Re-Enrollment job properties: {JsonConvert.SerializeObject(jobConfiguration.JobProperties)}");
            
            var (jobProperties, jobPropertiesFailure) = ParseJobPropertyFields(jobConfiguration.JobProperties);
            if (jobPropertiesFailure != null)
            {
                return jobPropertiesFailure;
            }
            
            _logger.LogDebug($"Parsed job properties: CN: {jobProperties.CommonName}, keyType: {jobProperties.KeyType}, keySize: {jobProperties.KeySize}");
            
            var encryptionAlgorithm = "2048-bit rsa"; // TODO: Resolve this from certificate template?
            var digestAlgorithm = properties.DigestAlgorithm;
            
            _logger.LogInformation($"Encryption alogrithm: {encryptionAlgorithm}, Digest Algorithm {digestAlgorithm}");

            var (client, clientError) = GetArubaClient(_logger, _resolver, _arubaClient, jobConfiguration,
                jobConfiguration.CertificateStoreDetails, properties);
            if (clientError != null)
            {
                return clientError;
            }
            _arubaClient = client!;

            var (serverInfo, serverInfoFailure) = GetArubaServerInfo(_logger, _arubaClient, jobConfiguration,
                jobConfiguration.CertificateStoreDetails);
            if (serverInfoFailure != null)
            {
                return serverInfoFailure;
            }

            var (fileServerClient, fileServerFailure) = GetFileServerClient(properties);
            if (fileServerFailure != null)
            {
                return fileServerFailure;
            }

            var (csr, csrFailure) = GetCertificateSigningRequest(jobProperties.CommonName, encryptionAlgorithm, digestAlgorithm);
            if (csrFailure != null)
            {
                return csrFailure;
            }

            var (certificate, certificateFailure) = SubmitReenrollmentUpdate(submitReenrollmentUpdate, csr);
            if (certificateFailure != null)
            {
                return certificateFailure;
            }

            var (certificateUrl, certificateUploadFailure) =
                UploadCertificateAndGetUrl(servername, serviceName, fileServerClient, certificate);
            if (certificateUploadFailure != null)
            {
                return certificateUploadFailure;
            }
            
            var uploadFailure = UpdateServerCertificate(servername, serverInfo!.ServerUuid, serviceName, certificateUrl!);
            if (uploadFailure != null)
            {
                return uploadFailure;
            }  
            
            _logger.LogInformation("Re-Enrollment (ODKG) job completed successfully");
            _logger.MethodExit();
            
            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Success,
                JobHistoryId = jobConfiguration.JobHistoryId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Re-Enrollment (ODKG) job failed with an unexpected error. {ex.Message} {ex.StackTrace}");
            return new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage = $"An unexpected error occurred in inventory job: {ex.Message}",
                JobHistoryId = jobConfiguration.JobHistoryId,
            };
        }
    }

    /// <summary>
    /// Gets the appropriate IFileServerClient interface filtered by name.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns>Returns a IFileServerClient if the name matches an existing interface. Otherwise, it returns null with a failed JobResult.</returns>
    private (IFileServerClient?, JobResult?) GetFileServerClient(ArubaCertificateStoreProperties properties)
    {
        var fileServerType = properties.FileServerType;
        var host = _resolver.Resolve(properties.FileServerHost);
        var username = _resolver.Resolve(properties.FileServerUsername);
        var password = _resolver.Resolve(properties.FileServerPassword);
        
        IFileServerClient? client;
        switch (fileServerType)
        {
            case "S3":
                client = _fileServerClientFactory.CreateFileServerClient(_logger, fileServerType, host, username, password);
                break;
            default:
                _logger.LogError($"Unable to find a matching file server type for '{fileServerType}'. Please check your certificate store properties and configure the File Server Type to an accepted value. " +
                                 $"Please consult the orchestrator documentation for more information.");
                
                return (null, new JobResult()
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    FailureMessage = $"Unable to find a matching file server type for '{fileServerType}'",
                });
        }

        if (client == null)
        {
            _logger.LogError($"A matching file server type '{fileServerType}' was not provided by the FileServerClientFactory! Please contact Keyfactor Support for assistance.");
            
            // This is an edge case where the IFileServerClientFactory was not properly updated with the accepted FileServerType
            return (null, new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage =
                    $"Matching file server type '{fileServerType}' was found but FileServerClientFactory did not provide a FileServerClient reference"
            });
        }
            
        _logger.LogDebug($"Successfully resolved file server type {fileServerType}");
        return (client, null);
    }

    /// <summary>
    /// Creates a certificate signing request within Aruba and returns the resulting certificate result
    /// </summary>
    /// <param name="properties"></param>
    /// <returns>Returns a CSR string if the request succeeds. Otherwise, it returns a null string with a failed JobResult.</returns>
    private (string?, JobResult?) GetCertificateSigningRequest(string subjectCN, string encryptionAlgorithm, string digestAlgorithm)
    {
        string? certificateSignRequest = null;
        try
        {
            _logger.LogDebug($"Creating CSR request in Aruba for subject CN {subjectCN} with encryption algorithm {encryptionAlgorithm} and {digestAlgorithm}");
            var response = _arubaClient.CreateCertificateSignRequest(subjectCN, encryptionAlgorithm, digestAlgorithm).GetAwaiter().GetResult();
            certificateSignRequest = response.CertificateSignRequest;
            
            ParseAndLogCsrMetadata(certificateSignRequest);
            _logger.LogDebug($"CSR request completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unable to create certificate sign request. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            return (null, new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage =
                    $"An error occurred while performing CSR Generation in Aruba. Error: {ex.Message}"
            });
        }

        return (certificateSignRequest, null);
    }

    /// <summary>
    /// Uploads the certificate contents to the remote file server client.
    /// </summary>
    /// <param name="servername"></param>
    /// <param name="service"></param>
    /// <param name="fileServerClient"></param>
    /// <param name="certificate"></param>
    /// <returns>Returns the certificate URL if the request succeeds. Otherwise, it returns a null string with a failed JobResult.</returns>
    private (string?, JobResult?) UploadCertificateAndGetUrl(string servername, string service, IFileServerClient fileServerClient, X509Certificate2 certificate)
    {
        string? certificateUrl = null;
        try
        {
            var key = $"{servername}_{service}.pfx";
            _logger.LogDebug($"Uploading certificate to file server under key {key}");
            certificateUrl = fileServerClient.UploadCertificate(key, certificate).GetAwaiter().GetResult();
            _logger.LogInformation($"Successfully uploaded certificate to file server under key {key}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unable to upload certificate to file server. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            return (null, new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage =
                    $"An error occurred while uploading certificate contents to file server: {ex.Message}"
            });
        }

        return (certificateUrl, null);
    }

    /// <summary>
    /// Updates the server certificate in Aruba for the appropriate service.
    /// </summary>
    /// <param name="servername"></param>
    /// <param name="serverUuid"></param>
    /// <param name="service"></param>
    /// <param name="certificateUrl"></param>
    /// <returns>Returns a null JobResult object if the request succeeds. Otherwise, it returns a failed JobResult.</returns>
    private JobResult? UpdateServerCertificate(string servername, string serverUuid, string service, string certificateUrl)
    {
        try
        {
            _logger.LogDebug($"Updating certificate in Aruba for server {servername} and service {service}");
            _arubaClient!.UpdateServerCertificate(serverUuid, service, certificateUrl);
            _logger.LogInformation($"Successfully updated certificate in Aruba for server {servername} and service {service}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unable to update certificate in Aruba. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            return new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage =
                    $"An error occurred while updating certificate in Aruba: {ex.Message}"
            };
        }

        return null;
    }

    private (X509Certificate2?, JobResult?) SubmitReenrollmentUpdate(SubmitReenrollmentCSR submitReenrollmentUpdate, string csr)
    {
        try
        {
            _logger.LogDebug($"Submitting enrollment CSR to Command");
            var certificate = submitReenrollmentUpdate.Invoke(csr);

            if (certificate == null)
            {
                _logger.LogError($"Command returned a null certificate from the CSR.");
                
                return (null, new JobResult()
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    FailureMessage =
                        $"Command returned a null certificate from the CSR."
                });
            }
            
            _logger.LogDebug($"CSR Enrollment completed successfully");
            return (certificate, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unable to submit CSR to Command. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            return (null, new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage =
                    $"An error occurred while submitting re-enrollment update: {ex.Message}"
            });
        }
        
    }

    /// <summary>
    /// To troubleshoot failures sending CSR to Command, this utility function will help log the CSR metadata
    /// </summary>
    /// <param name="csrPem"></param>
    private void ParseAndLogCsrMetadata(string csrPem)
    {
        _logger.MethodEntry();
        // Read CSR
        Pkcs10CertificationRequest csr;
        using (var reader = new StringReader(csrPem))
        {
            var pemReader = new PemReader(reader);
            csr = (Pkcs10CertificationRequest)pemReader.ReadObject();
        }

        _logger.LogDebug("===== Parsed CSR Information =====");

        // Subject
        var subject = csr.GetCertificationRequestInfo().Subject;
        _logger.LogDebug($"Subject: {subject}");

        // Public Key
        AsymmetricKeyParameter publicKey = csr.GetPublicKey();
        var pubKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
        var alg = pubKeyInfo.AlgorithmID.Algorithm.Id;

        _logger.LogDebug($"Public Key Algorithm: {alg}");
        if (publicKey is Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters rsa)
        {
            _logger.LogDebug($"RSA Key Size: {rsa.Modulus.BitLength} bits");
        }

        // Signature algorithm
        var sigAlg = csr.SignatureAlgorithm.Algorithm.Id;
        _logger.LogDebug($"Signature Algorithm: {sigAlg}");

        _logger.LogDebug("==================================");
        _logger.MethodExit();
    }

    private (JobPropertyFields?, JobResult?) ParseJobPropertyFields(Dictionary<string, object> properties)
    {
        _logger.MethodEntry();
        var errors = new StringBuilder();

        _logger.LogTrace("Parsing subjectText from job properties");
        if (!properties.TryGetValue("subjectText", out var subjectText))
        {
            errors.AppendLine("SubjectText was not found in job properties. ");
        }
        
        _logger.LogTrace("Parsing keyType from job properties");
        if (!properties.TryGetValue("keyType", out var keyType))
        {
            errors.AppendLine("KeyType was not found in job properties. ");
        }

        _logger.LogTrace("Parsing keySize from job properties");
        if (!properties.TryGetValue("keySize", out var keySize))
        {
            errors.AppendLine("KeySize was not found in job properties. ");
        }

        if (errors.Length > 0)
        {
            return (null, new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage = errors.ToString()
            });
        }

        var result = new JobPropertyFields()
        {
            CommonName = GetCommonNameField(subjectText.ToString()),
            KeySize = (Int64)keySize,
            KeyType = keyType.ToString(),
        };
        _logger.MethodExit();
        return (result, null);
    }

    private string GetCommonNameField(string subjectText)
    {
        _logger.MethodEntry();
        var dn = new X500DistinguishedName(subjectText);
        var cn = dn.Name
            .Split(',')
            .Select(kvp => kvp.Trim())
            .FirstOrDefault(kvp => kvp.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            ?.Substring(3);
        _logger.LogDebug($"Parsed common name from subject text: {cn}");
        _logger.MethodExit();
        return cn;
    }
}
