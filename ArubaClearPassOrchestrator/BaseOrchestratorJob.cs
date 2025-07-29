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

using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Exceptions;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator;

public abstract class BaseOrchestratorJob
{
    protected string ClientMachine { get; set; }
    protected string ServerName { get; set; }
    protected string ServiceName { get; set; }
    protected long JobHistoryId { get; set; } = 0;

    /// <summary>
    /// Parses the CertificateStore setup and sets up the Client Machine, Server Name, and Service Name.
    /// It validates the certificate store path is configured as expected, which should be a semicolon-delimited string
    /// with the format `[server-name];[service-name]`
    /// </summary>
    /// <param name="certStore"></param>
    /// <returns></returns>
    protected JobOperation ParseCertificateStoreConfiguration(ILogger logger, CertificateStore certStore)
    {
        logger.MethodEntry();
        
        ClientMachine = certStore.ClientMachine;
        
        var storePath = certStore.StorePath;
        
        if (!storePath.Contains(';'))
        {
            logger.LogError($"Service name could not be parsed from store path '{storePath}'. Please consult the orchestrator documentation for store path setup details.");
            logger.MethodExit();
            
            return JobOperation.Fail($"Service name could not be parsed from store path '{storePath}'");
        }

        var split = storePath.Split(';');

        ServerName = split[0];
        ServiceName = split[1];

        logger.MethodExit();
        return JobOperation.Success();
    }
    
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
    /// <returns>A JobOperation object wrapping around the ArubaClient and a JobResult</returns>
    protected JobOperation<IArubaClient> GetArubaClient(ILogger logger, IPAMSecretResolver resolver, IArubaClient arubaClient, JobConfiguration jobConfiguration, CertificateStore certificateStore, ArubaCertificateStoreProperties properties)
    {
        logger.MethodEntry();

        try
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

            return JobOperation<IArubaClient>.Success(result);
        }
        catch (ArubaAuthenticationException ex)
        {
            logger.LogError(ex, $"Aruba authentication failed. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");

            return JobOperation<IArubaClient>.Fail($"Unable to authenticate to Aruba instance. Message: {ex.Message}",
                JobHistoryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                $"An unexpected error occurred while getting Aruba client. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");

            return JobOperation<IArubaClient>.Fail(
                $"An unexpected error occurred while getting Aruba client. Message: {ex.Message}", JobHistoryId);
        }
        finally
        {
            logger.MethodExit();
        }
    }

    /// <summary>
    /// Retrieves the Aruba server information given the store path provided by the job configuration.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="arubaClient"></param>
    /// <param name="jobConfiguration"></param>
    /// <param name="certificateStore"></param>
    /// <returns>A JobOperation object wrapping around the ClusterServerItem and a JobResult</returns>
    protected JobOperation<ClusterServerItem> GetArubaServerInfo(ILogger logger, IArubaClient arubaClient, JobConfiguration jobConfiguration, CertificateStore certificateStore)
    {
        logger.MethodEntry();
        logger.LogDebug("Getting server information from Aruba");
        
        var servers = arubaClient.GetClusterServers().GetAwaiter().GetResult();
        var storePath = certificateStore.StorePath;

        logger.LogDebug($"Number of servers found in Aruba: {servers.Count}");
        logger.LogDebug($"Server names found in Aruba: {string.Join(", ", servers.Select(p => $"'{p.Name}'"))}");

        var serverInfo = servers.FirstOrDefault(p => p.Name == storePath);
        if (serverInfo == null)
        {
            logger.LogError($"ERROR: Unable to find store '{storePath}' in Aruba system");
            
            logger.MethodExit();

            return JobOperation<ClusterServerItem>.Fail($"Unable to find store '{storePath}' in Aruba system", JobHistoryId);
        }

        logger.MethodExit();

        logger.LogDebug($"Successfully found server '{storePath}' in Aruba. Store UUID: {serverInfo.ServerUuid}");
        return JobOperation<ClusterServerItem>.Success(serverInfo);
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
