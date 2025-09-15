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

using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator;

public class Inventory : BaseOrchestratorJob, IInventoryJobExtension
{
    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Inventory";

    private IArubaClient _arubaClient;
    
    private readonly ILogger _logger;
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
            JobHistoryId = jobConfiguration.JobHistoryId;

            var configResult = ParseCertificateStoreConfiguration(_logger, jobConfiguration.CertificateStoreDetails);
            if (!configResult.IsSuccessful)
            {
                return configResult.JobResult;
            }

            var properties =
                JsonConvert.DeserializeObject<ArubaCertificateStoreProperties>(jobConfiguration.CertificateStoreDetails
                    .Properties);
            
            _logger.LogInformation($"Inventory job target: Host: {ClientMachine}, Server Name: {ServerName}, Service Name: {ServerName}");

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

            var certificate = _arubaClient.GetServerCertificate(serverInfo.ServerUuid, ServiceName).GetAwaiter().GetResult();

            var certificateEntry = new CurrentInventoryItem()
            {
                Alias = $"{ServerName} {ServiceName}",
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
                JobHistoryId = JobHistoryId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in inventory job. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");

            return new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage = $"An unexpected error occurred in inventory job: {ex.Message}",
                JobHistoryId = JobHistoryId,
            };
        }
    }
}
