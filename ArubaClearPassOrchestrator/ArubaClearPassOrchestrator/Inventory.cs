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

public class Inventory : IInventoryJobExtension
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
            var hostname = jobConfiguration.CertificateStoreDetails.ClientMachine;
            var serverUsername = ResolvePAMField(jobConfiguration.ServerUsername, "Server Username");
            var serverPassword = ResolvePAMField(jobConfiguration.ServerPassword, "Server Password");

            if (_arubaClient == null)
            {
                _logger.LogTrace("Instantiating the Aruba Client");
                _arubaClient = new ArubaClient(_logger, properties.ServerUseSslEnabled, hostname, serverUsername, serverPassword);
                _logger.LogTrace("Aruba Client instantiated successfully");
            }

            _logger.LogTrace("Getting server information from Aruba");

            var servers = _arubaClient.GetClusterServers();
            var storePath = jobConfiguration.CertificateStoreDetails.StorePath;

            _logger.LogDebug($"Number of servers found in Aruba: {servers.Count}");
            _logger.LogDebug($"Server names found in Aruba: {string.Join(", ", servers.Select(p => $"'{p.Name}'"))}");

            var serverInfo = servers.FirstOrDefault(p => p.Name == storePath);
            if (serverInfo == null)
            {
                _logger.LogError($"ERROR: Unable to find store '{storePath}' in Aruba system");

                return new JobResult()
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    JobHistoryId = jobConfiguration.JobHistoryId,
                    FailureMessage = $"Unable to find store '{storePath}' in Aruba system"
                };
            }

            _logger.LogDebug($"Successfully found server '{storePath}' in Aruba. Store UUID: {serverInfo.ServerUuid}");

            var certificate = _arubaClient.GetServerCertificate(serverInfo.ServerUuid, properties.ServiceName);

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

            submitInventoryUpdate.Invoke(new List<CurrentInventoryItem>(){ certificateEntry });

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

    private string ResolvePAMField(string key, string description)
    {
        _logger.MethodEntry();
        _logger.LogDebug($"Fetching {description} value from PAM");
        var value = _resolver.Resolve(key);
        _logger.LogDebug($"Successfully fetched {description} value from PAM");
        _logger.MethodExit();
        return value;
    }
}
