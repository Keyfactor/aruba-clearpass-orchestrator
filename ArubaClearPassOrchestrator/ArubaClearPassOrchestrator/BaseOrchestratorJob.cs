using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArubaClearPassOrchestrator;

public abstract class BaseOrchestratorJob
{
    /// <summary>
    /// If an IArubaClient instance is not provided by the constructor, then instantiate a new instance using
    /// the certificate store properties
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="resolver"></param>
    /// <param name="arubaClient"></param>
    /// <param name="jobConfiguration"></param>
    /// <param name="certificateStore"></param>
    /// <param name="properties"></param>
    /// <returns>The existing IArubaClient if not null, a new IArubaClient if null</returns>
    protected IArubaClient GetArubaClient(ILogger logger, IPAMSecretResolver resolver, IArubaClient? arubaClient, JobConfiguration jobConfiguration, CertificateStore certificateStore, ArubaCertificateStoreProperties properties)
    {
        var serverUsername = ResolvePAMField(logger, resolver, jobConfiguration.ServerUsername, "Server Username");
        var serverPassword = ResolvePAMField(logger, resolver, jobConfiguration.ServerPassword, "Server Password");
        var hostname = certificateStore.ClientMachine;
        var useSsl = properties.ServerUseSslEnabled;

        IArubaClient result;
        
        if (arubaClient == null)
        {
            logger.LogTrace("Instantiating the Aruba Client");
            result = new ArubaClient(logger, useSsl, hostname, serverUsername, serverPassword);
            logger.LogTrace("Aruba Client instantiated successfully");
        }
        else
        {
            logger.LogTrace("Using the constructor-provided Aruba Client");
            result = arubaClient;
        }

        return result;
    }

    /// <summary>
    /// Retrieves the Aruba server information given the store path provided by the job configuration.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="arubaClient"></param>
    /// <param name="jobConfiguration"></param>
    /// <param name="certificateStore"></param>
    /// <returns>If successful, the ClusterServerItem will be populated with a null JobResult object
    /// If not successful, the ClusterServerItem will be null with a populated JobResult with failure information.</returns>
    protected (ClusterServerItem?, JobResult?) GetArubaServerInfo(ILogger logger, IArubaClient arubaClient, JobConfiguration jobConfiguration, CertificateStore certificateStore)
    {
        logger.LogTrace("Getting server information from Aruba");
        
        var servers = arubaClient.GetClusterServers().GetAwaiter().GetResult();
        var storePath = certificateStore.StorePath;

        logger.LogDebug($"Number of servers found in Aruba: {servers.Count}");
        logger.LogDebug($"Server names found in Aruba: {string.Join(", ", servers.Select(p => $"'{p.Name}'"))}");

        var serverInfo = servers.FirstOrDefault(p => p.Name == storePath);
        if (serverInfo == null)
        {
            logger.LogError($"ERROR: Unable to find store '{storePath}' in Aruba system");

            var jobResult = new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                JobHistoryId = jobConfiguration.JobHistoryId,
                FailureMessage = $"Unable to find store '{storePath}' in Aruba system"
            };

            return (null, jobResult);
        }

        logger.LogDebug($"Successfully found server '{storePath}' in Aruba. Store UUID: {serverInfo.ServerUuid}");
        return (serverInfo, null);
    }
    
    /// <summary>
    /// Resolves a secret field using the PAM provider
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="resolver"></param>
    /// <param name="key"></param>
    /// <param name="description"></param>
    /// <returns>The secret string value for the associated key</returns>
    protected string ResolvePAMField(ILogger logger, IPAMSecretResolver resolver, string key, string description)
    {
        logger.MethodEntry();
        logger.LogDebug($"Fetching {description} value from PAM");
        var value = resolver.Resolve(key);
        logger.LogDebug($"Successfully fetched {description} value from PAM");
        logger.MethodExit();
        return value;
    }
}
