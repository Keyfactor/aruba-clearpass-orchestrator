using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator;

public class Inventory : BaseOrchestratorJob, IInventoryJobExtension
{
    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Inventory";
    private readonly ILogger _logger;

    private IArubaClient? _arubaClient;
    private readonly IPAMSecretResolver _resolver;

    public Inventory(IPAMSecretResolver resolver)
    {
        _logger = LogHandler.GetClassLogger<Inventory>();
        _resolver = resolver;
    }

    public Inventory(ILogger logger, IArubaClient arubaClient, IPAMSecretResolver resolver)
    {
        _logger = logger;
        _arubaClient = arubaClient;
        _resolver = resolver;
    }

    public JobResult ProcessJob(InventoryJobConfiguration jobConfiguration, SubmitInventoryUpdate submitInventoryUpdate)
    {
        try
        {
            _logger.MethodEntry();
            _logger.LogInformation("Starting inventory job");

            var properties =
                JsonConvert.DeserializeObject<ArubaCertificateStoreProperties>(jobConfiguration.CertificateStoreDetails
                    .Properties);
            var storePath = jobConfiguration.CertificateStoreDetails.StorePath;

            _arubaClient = GetArubaClient(_logger, _resolver, _arubaClient, jobConfiguration,
                jobConfiguration.CertificateStoreDetails, properties);

            var (serverInfo, jobResult) = GetArubaServerInfo(_logger, _arubaClient, jobConfiguration,
                jobConfiguration.CertificateStoreDetails);
            if (serverInfo == null)
            {
                return jobResult;
            }

            var certificate = _arubaClient.GetServerCertificate(serverInfo.ServerUuid, properties.ServiceName).GetAwaiter().GetResult();

            var certificateEntry = new CurrentInventoryItem()
            {
                Alias = $"{storePath} {properties.ServiceName}",
                ItemStatus = OrchestratorInventoryItemStatus.Unknown,
                PrivateKeyEntry = false,
                Certificates = new[]
                {
                    certificate.CertFile
                }
            };

            _logger.LogTrace("Submitting certificate information to inventory");

            submitInventoryUpdate.Invoke(new List<CurrentInventoryItem>() { certificateEntry });

            _logger.LogInformation("Inventory job completed successfully");
            _logger.MethodExit();

            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Success,
                JobHistoryId = jobConfiguration.JobHistoryId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in inventory job");

            return new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage = $"An unexpected error occurred in inventory job: {ex.Message}",
                JobHistoryId = jobConfiguration.JobHistoryId,
            };
        }
    }
}
