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

using System.Security.Cryptography.X509Certificates;
using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Generators;
using Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Logging;
using Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Mappers;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Exception = System.Exception;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator;

public class Reenrollment : BaseOrchestratorJob, IReenrollmentJobExtension
{
    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Reenrollment";

    private IArubaClient _arubaClient;

    private readonly ILogger _logger;
    private readonly IFileServerClientFactory _fileServerClientFactory;
    private readonly IPAMSecretResolver _resolver;

    private const int SecurePasswordLength = 32;

    public Reenrollment(IPAMSecretResolver resolver)
    {
        _logger = LogHandler.GetClassLogger<Reenrollment>();
        _resolver = resolver;
        _fileServerClientFactory = new FileServerClientFactory();
    }

    public Reenrollment(ILogger logger, IArubaClient arubaClient, IFileServerClientFactory fileServerClientFactory,
        IPAMSecretResolver resolver)
    {
        _logger = logger;
        _arubaClient = arubaClient;
        _fileServerClientFactory = fileServerClientFactory;
        _resolver = resolver;
    }

    public JobResult ProcessJob(ReenrollmentJobConfiguration jobConfiguration,
        SubmitReenrollmentCSR submitReenrollmentUpdate)
    {
        try
        {
            _logger.MethodEntry();
            _logger.LogInformation("Starting Re-Enrollment (ODKG) job");
            JobHistoryId = jobConfiguration.JobHistoryId;

            var configResult = ParseCertificateStoreConfiguration(_logger, jobConfiguration.CertificateStoreDetails);
            if (!configResult.IsSuccessful)
            {
                return configResult.JobResult;
            }

            var properties =
                JsonConvert.DeserializeObject<ArubaCertificateStoreProperties>(jobConfiguration.CertificateStoreDetails
                    .Properties);

            _logger.LogInformation(
                $"Re-Enrollment job target: Host: {ClientMachine}, Server Name: {ServerName}, Service Name: {ServiceName}");
            _logger.LogDebug(
                $"Re-Enrollment job properties: {JsonConvert.SerializeObject(jobConfiguration.JobProperties)}");

            var jobPropertiesResult = ReenrollmentKeyPropertyFieldsMapper.MapKeyPropertyFields(_logger, jobConfiguration.JobProperties, JobHistoryId);
            if (!jobPropertiesResult.IsSuccessful)
            {
                return jobPropertiesResult.JobResult;
            }

            var jobProperties = jobPropertiesResult.Value;

            _logger.LogDebug(
                $"Parsed job properties: SubjectText: {jobProperties.SubjectText}, keyType: {jobProperties.KeyType}, keySize: {jobProperties.KeySize}");

            var subjectInformation = CertificateSubjectInformation.ParseFromSubjectText(jobProperties.SubjectText);

            _logger.LogInformation($"Parsed subject information: {JsonConvert.SerializeObject(subjectInformation)}");

            if (subjectInformation.Email != null)
            {
                _logger.LogWarning(
                    $"The certificate subject field Email (E) was found on the ODKG request. This may cause a failure as Aruba does not support the email field on the CSR request!");
            }

            var encryptionAlgorithmResult =
                ArubaEncryptionAlgorithmMapper.MapEncryptionAlgorithm(_logger, jobProperties.KeyType, jobProperties.KeySize, JobHistoryId);

            if (!encryptionAlgorithmResult.IsSuccessful)
            {
                return encryptionAlgorithmResult.JobResult;
            }

            var encryptionAlgorithm = encryptionAlgorithmResult.Value;
            var digestAlgorithm = properties.DigestAlgorithm;

            _logger.LogInformation($"Encryption algorithm: {encryptionAlgorithm}, Digest Algorithm {digestAlgorithm}");

            var clientResult = GetArubaClient(_logger, _resolver, _arubaClient, jobConfiguration,
                jobConfiguration.CertificateStoreDetails, properties);
            if (!clientResult.IsSuccessful)
            {
                return clientResult.JobResult;
            }

            _arubaClient = clientResult.Value;

            var serverInfoResult = GetArubaServerInfo(_logger, _arubaClient, ServerName);
            if (!serverInfoResult.IsSuccessful)
            {
                return serverInfoResult.JobResult;
            }

            var serverInfo = serverInfoResult.Value;

            var fileServerClientResult = GetFileServerClient(properties);
            if (!fileServerClientResult.IsSuccessful)
            {
                return fileServerClientResult.JobResult;
            }

            var fileServerClient = fileServerClientResult.Value;

            var sans = ArubaSansMapper.MapSANs(_logger, jobConfiguration, jobProperties);

            var csrResult =
                GetCertificateSigningRequest(subjectInformation, sans, encryptionAlgorithm, digestAlgorithm);
            if (!csrResult.IsSuccessful)
            {
                return csrResult.JobResult;
            }

            var csr = csrResult.Value;

            var certificateResult = SubmitReenrollmentUpdate(submitReenrollmentUpdate, csr, jobProperties.SubjectText);
            if (!certificateResult.IsSuccessful)
            {
                return certificateResult.JobResult;
            }

            var certificate = certificateResult.Value;

            var certificateUrlResult =
                UploadCertificateAndGetUrl(ServerName, ServiceName, fileServerClient, certificate);
            if (!certificateUrlResult.IsSuccessful)
            {
                return certificateUrlResult.JobResult;
            }

            var certificateUrl = certificateUrlResult.Value;

            var uploadResult =
                UpdateServerCertificate(ServerName, serverInfo!.ServerUuid, ServiceName, certificateUrl!);
            if (!uploadResult.IsSuccessful)
            {
                return uploadResult.JobResult;
            }

            _logger.LogInformation("Re-Enrollment (ODKG) job completed successfully");

            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Success,
                JobHistoryId = JobHistoryId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Re-Enrollment (ODKG) job failed with an unexpected error. {ex.Message} {ex.StackTrace}");
            return new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage = $"An unexpected error occurred in inventory job: {ex.Message}",
                JobHistoryId = JobHistoryId,
            };
        }
        finally
        {
            _logger.MethodExit();
        }
    }

    /// <summary>
    /// Gets the appropriate IFileServerClient interface filtered by name.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns>Returns a IFileServerClient if the name matches an existing interface. Otherwise, it returns null with a failed JobResult.</returns>
    private JobOperation<IFileServerClient> GetFileServerClient(ArubaCertificateStoreProperties properties)
    {
        _logger.MethodEntry();

        var fileServerType = properties.FileServerType;
        var host = _resolver.Resolve(properties.FileServerHost);
        var username = _resolver.Resolve(properties.FileServerUsername);
        var password = _resolver.Resolve(properties.FileServerPassword);

        IFileServerClient client;
        switch (fileServerType)
        {
            case "Amazon S3":
                client = _fileServerClientFactory.CreateFileServerClient(_logger, fileServerType, host, username,
                    password);
                break;
            default:
                _logger.LogError(
                    $"Unable to find a matching file server type for '{fileServerType}'. Please check your certificate store properties and configure the File Server Type to an accepted value. " +
                    $"Please consult the orchestrator documentation for more information.");

                return JobOperation<IFileServerClient>.Fail(
                    $"Unable to find a matching file server type for '{fileServerType}'", JobHistoryId);
        }

        if (client == null)
        {
            _logger.MethodExit();

            _logger.LogError(
                $"A matching file server type '{fileServerType}' was not provided by the FileServerClientFactory! Please contact Keyfactor Support for assistance.");

            // This is an edge case where the IFileServerClientFactory was not properly updated with the accepted FileServerType
            return JobOperation<IFileServerClient>.Fail(
                $"Matching file server type '{fileServerType}' was found but FileServerClientFactory did not provide a FileServerClient reference",
                JobHistoryId);
        }

        _logger.MethodExit();

        _logger.LogDebug($"Successfully resolved file server type {fileServerType}");
        return JobOperation<IFileServerClient>.Success(client);
    }

    /// <summary>
    /// Creates a certificate signing request within Aruba and returns the resulting certificate result
    /// </summary>
    /// <param name="properties"></param>
    /// <returns>Returns a CSR string if the request succeeds. Otherwise, it returns a null string with a failed JobResult.</returns>
    private JobOperation<string> GetCertificateSigningRequest(CertificateSubjectInformation subjectInformation,
        string sans, string encryptionAlgorithm, string digestAlgorithm)
    {
        _logger.MethodEntry();

        try
        {
            var password = PasswordGenerator.Generate(_logger, SecurePasswordLength);
            _logger.LogDebug(
                $"Creating CSR request in Aruba for subject CN {subjectInformation.CommonName} with encryption algorithm {encryptionAlgorithm} and {digestAlgorithm}");
            var response = _arubaClient
                .CreateCertificateSignRequest(subjectInformation, sans, password, encryptionAlgorithm, digestAlgorithm)
                .GetAwaiter()
                .GetResult();
            var certificateSignRequest = response.CertificateSignRequest;

            CertificateSigningRequestLogger.ParseCsrMetadata(_logger, certificateSignRequest);
            _logger.LogDebug($"CSR request completed successfully.");
            _logger.LogTrace($"CSR content: {certificateSignRequest}");

            return JobOperation<string>.Success(certificateSignRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unable to create certificate sign request. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            return JobOperation<string>.Fail(
                $"An error occurred while performing CSR Generation in Aruba. Error: {ex.Message}", JobHistoryId);
        }
        finally
        {
            _logger.MethodExit();
        }
    }

    /// <summary>
    /// Uploads the certificate contents to the remote file server client.
    /// </summary>
    /// <param name="servername"></param>
    /// <param name="service"></param>
    /// <param name="fileServerClient"></param>
    /// <param name="certificate"></param>
    /// <returns>Returns the certificate URL if the request succeeds. Otherwise, it returns a null string with a failed JobResult.</returns>
    private JobOperation<string> UploadCertificateAndGetUrl(string servername, string service,
        IFileServerClient fileServerClient, X509Certificate2 certificate)
    {
        _logger.MethodEntry();

        string certificateUrl = null;
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileName = $"{servername}_{service}_{timestamp}";
            _logger.LogDebug($"Uploading certificate to file server under filename {fileName}");
            certificateUrl = fileServerClient.UploadCertificate(fileName, certificate).GetAwaiter().GetResult();
            _logger.LogInformation($"Successfully uploaded certificate to file server under key {fileName}");
            _logger.LogDebug($"Certificate URL: {certificateUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unable to upload certificate to file server. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");

            return JobOperation<string>.Fail(
                $"An error occurred while uploading certificate contents to file server: {ex.Message}", JobHistoryId);
        }
        finally
        {
            _logger.MethodExit();
        }

        return JobOperation<string>.Success(certificateUrl);
    }

    /// <summary>
    /// Updates the server certificate in Aruba for the appropriate service.
    /// </summary>
    /// <param name="servername"></param>
    /// <param name="serverUuid"></param>
    /// <param name="service"></param>
    /// <param name="certificateUrl"></param>
    /// <returns>Returns a null JobResult object if the request succeeds. Otherwise, it returns a failed JobResult.</returns>
    private JobOperation UpdateServerCertificate(string servername, string serverUuid, string service,
        string certificateUrl)
    {
        _logger.MethodEntry();

        try
        {
            _logger.LogDebug($"Updating certificate in Aruba for server {servername} and service {service}");
            _arubaClient!.UpdateServerCertificate(serverUuid, service, certificateUrl).GetAwaiter().GetResult();
            _logger.LogInformation(
                $"Successfully updated certificate in Aruba for server {servername} and service {service}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unable to update certificate in Aruba. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            return JobOperation.Fail($"An error occurred while updating certificate in Aruba: {ex.Message}",
                JobHistoryId);
        }
        finally
        {
            _logger.MethodExit();
        }

        return JobOperation.Success("Server certificate updated successfully");
    }

    /// <summary>
    /// Send the CSR request to Command synchronously. If successful, Command will return an X509Certificate2 object
    /// If there is an issue with the update (i.e. CSR subject information does not match ODKG definition), report a job failure.
    /// </summary>
    /// <param name="submitReenrollmentUpdate"></param>
    /// <param name="csr"></param>
    /// <param name="subjectText"></param>
    /// <returns></returns>
    private JobOperation<X509Certificate2> SubmitReenrollmentUpdate(SubmitReenrollmentCSR submitReenrollmentUpdate,
        string csr, string subjectText)
    {
        _logger.MethodEntry();

        try
        {
            _logger.LogDebug($"Submitting enrollment CSR to Command");
            var certificate = submitReenrollmentUpdate.Invoke(csr);

            if (certificate == null)
            {
                _logger.LogError($"Command returned a null certificate from the CSR.");

                return JobOperation<X509Certificate2>.Fail(
                    $"Command returned a null certificate from the CSR. Did the subject information included in the CSR match the subject information on the ODKG request? Subject text: \"{subjectText}\". Including email in subject may cause issues. Check the Keyfactor Command logs or workflow instance logs for error information.",
                    JobHistoryId);
            }

            _logger.LogDebug($"CSR Enrollment completed successfully");

            return JobOperation<X509Certificate2>.Success(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unable to submit CSR to Command. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");

            return JobOperation<X509Certificate2>.Fail(
                $"An error occurred while submitting re-enrollment update: {ex.Message}. Check the Keyfactor Command logs for more information.",
                JobHistoryId);
        }
        finally
        {
            _logger.MethodExit();
        }
    }
}