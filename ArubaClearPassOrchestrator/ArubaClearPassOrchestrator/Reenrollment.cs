using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Exception = System.Exception;

namespace ArubaClearPassOrchestrator;

public class Reenrollment : BaseOrchestratorJob, IReenrollmentJobExtension
{
    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Reenrollment";
    private readonly ILogger _logger = LogHandler.GetClassLogger<Reenrollment>();

    private IArubaClient? _arubaClient;
    private IFileServerClient _fileServerClient;
    private readonly IPAMSecretResolver _resolver;
    
    public Reenrollment(IPAMSecretResolver resolver)
    {
        _logger = LogHandler.GetClassLogger<Inventory>();
        _resolver = resolver;
    }

    public Reenrollment(ILogger logger, IArubaClient arubaClient, IFileServerClient fileServerClient, IPAMSecretResolver resolver)
    {
        _logger = logger;
        _arubaClient = arubaClient;
        _fileServerClient = fileServerClient;
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
            
            var servername = jobConfiguration.CertificateStoreDetails.StorePath;
            var service = properties.ServiceName;
            var encryptionAlgorithm = "2048-bit rsa"; // TODO: Resolve this from certificate template?
            var digestAlgorithm = properties.DigestAlgorithm;

            _arubaClient = GetArubaClient(_logger, _resolver, _arubaClient, jobConfiguration,
                jobConfiguration.CertificateStoreDetails, properties);

            var (serverInfo, jobResult) = GetArubaServerInfo(_logger, _arubaClient, jobConfiguration,
                jobConfiguration.CertificateStoreDetails);
            if (serverInfo == null)
            {
                return jobResult!;
            }

            var fileServerType = properties.FileServerType;
            switch (fileServerType)
            {
                case "S3":
                    // TODO: Create S3 Client
                    break;
                case "File Server":
                    // TODO: Create File Server Client
                    break;
                default:
                    return new JobResult()
                    {
                        Result = OrchestratorJobStatusJobResult.Failure,
                        FailureMessage = $"Unable to find a matching file server type for '{fileServerType}'",
                    };
            }
            
            _logger.LogDebug($"Successfully resolved file server type {fileServerType}");

            string certificateSignRequest = null;
            try
            {
                _logger.LogDebug($"Creating CSR request in Aruba for server {servername} with encryption algorithm {encryptionAlgorithm} and {digestAlgorithm}");
                var response = _arubaClient.CreateCertificateSignRequest(servername, encryptionAlgorithm, digestAlgorithm);
                certificateSignRequest = response.CertificateSignRequest;
                _logger.LogDebug($"CSR request completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create certificate sign request");
                return new JobResult()
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    FailureMessage =
                        $"An error occurred while performing CSR Generation in Aruba. Error: {ex.Message}"
                };
            }

            _logger.LogDebug($"Submitting enrollment CSR to Command");
            var certificate = submitReenrollmentUpdate.Invoke(certificateSignRequest);
            _logger.LogDebug($"CSR Enrollment completed successfully");

            // TODO: We need to convert the certificate object into a string somehow

            string certificateUrl = null;
            try
            {
                var key = $"{servername}_{service}.pfx";
                _logger.LogDebug($"Uploading certificate to file server under key {key}");
                certificateUrl = _fileServerClient.UploadCertificate(key, certificate);
                _logger.LogInformation($"Successfully uploaded certificate to file server under key {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to upload certificate to file server");
                return new JobResult()
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    FailureMessage =
                        $"An error occurred while uploading certificate contents to file server: {ex.Message}"
                };
            }

            try
            {
                _logger.LogDebug($"Updating certificate in Aruba for server {servername} and service {service}");
                _arubaClient.UpdateServerCertificate(serverInfo.ServerUuid, service, certificateUrl);
                _logger.LogInformation($"Successfully updated certificate in Aruba for server {servername} and service {service}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to update certificate in Aruba");
                return new JobResult()
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    FailureMessage =
                        $"An error occurred while updating certificate in Aruba: {ex.Message}"
                };
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
            return new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage = $"An unexpected error occurred in inventory job: {ex.Message}",
                JobHistoryId = jobConfiguration.JobHistoryId,
            };
        }
    }
}
