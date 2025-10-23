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

using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Constants;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Mappers;

public static class ArubaSansMapper
{
    /// <summary>
    /// Returns a Aruba-accepted SAN string from either the ReenrollmentJobConfiguration object or the job properties.
    /// The SANs given in the ReenrollmentJobConfiguration object will take precedence over the job properties.
    /// </summary>
    /// <param name="jobConfig"></param>
    /// <param name="jobProperties"></param>
    /// <returns></returns>
    public static string MapSANs(
        ILogger logger,
        ReenrollmentJobConfiguration jobConfig,
        ReenrollmentKeyPropertyFields jobProperties)
    {
        logger.MethodEntry();
        
        try
        {
            var jobConfigSans = GetSansFromJobConfig(logger, jobConfig);
            if (!string.IsNullOrEmpty(jobConfigSans))
            {
                logger.LogDebug($"Using SANs from job config: {jobConfigSans}");
                logger.MethodExit();
                return jobConfigSans;
            }
        }
        catch (Exception)
        {
            // Older versions of UO may throw an exception when trying to access the SANs from the ReenrollmentJobConfiguration object
        } 
        
        var result = jobProperties.SANs;
        
        logger.LogDebug($"No SANs found in job config. SANs value in job properties: {result}");

        // If SAN string is received with an '&', convert it to ',' which is the accepted delimiter for Aruba
        // If SAN string is received with "=" for key-value pair, map it to ":" which is the accepted KV delimiter for Aruba
        result = result?
            .Replace('&', ArubaClearPassConstants.Delimiters.Sans)
            .Replace("=", ":");
        
        logger.LogDebug($"Using SANs value: {result}");
        
        logger.MethodExit();
        
        return result;
    }

    /// <summary>
    /// Parses the SANs from the ReenrollmentJobConfiguration object and converts it to an Aruba-accepted SAN string.
    /// </summary>
    /// <param name="jobConfig"></param>
    /// <returns></returns>
    private static string GetSansFromJobConfig(ILogger logger, ReenrollmentJobConfiguration jobConfig)
    {
        logger.MethodEntry();

        try
        {
            var sansDictionary = jobConfig.SANs;

            if (sansDictionary is null || sansDictionary.Count == 0)
            {
                logger.LogDebug($"No SANs found in job config.");
                return null;
            }

            var sansList = new List<string>();

            sansDictionary.TryGetValue(KeyfactorConstants.SanTypes.DnsName, out var dnsNames);
            sansDictionary.TryGetValue(KeyfactorConstants.SanTypes.IpAddress, out var ipAddress);
            sansDictionary.TryGetValue(KeyfactorConstants.SanTypes.Email, out var email);
            sansDictionary.TryGetValue(KeyfactorConstants.SanTypes.Upn, out var userPrincipalName);

            logger.LogDebug($"Pulled SANs from job configuration: {JsonConvert.SerializeObject(sansDictionary)}");

            if (dnsNames != null)
            {
                sansList.AddRange(dnsNames.Select(p => $"{ArubaClearPassConstants.SanPrefixes.Dns}:{p}"));
            }

            if (ipAddress != null)
            {
                sansList.AddRange(ipAddress.Select(p => $"{ArubaClearPassConstants.SanPrefixes.IpAddress}:{p}"));
            }

            if (email != null)
            {
                sansList.AddRange(email.Select(p => $"{ArubaClearPassConstants.SanPrefixes.Email}:{p}"));
            }

            if (userPrincipalName != null)
            {
                logger.LogWarning($"Aruba ClearPass does not support UPN SAN entries.");
            }

            var result = string.Join(ArubaClearPassConstants.Delimiters.Sans, sansList);

            logger.LogDebug($"Resulting SANs string from job configuration: {result}");
            
            return result;
        }
        catch (Exception)
        {
            // Older versions of Command / UO might throw an exception if SANs are retrieved from job configuration
            logger.LogDebug($"SANs could not be parsed from job config.");
            return null;
        }
        finally
        {
            logger.MethodExit();
        }
    }
}
