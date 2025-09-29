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

using System.Text;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Mappers;

public static class ReenrollmentKeyPropertyFieldsMapper
{
    /// <summary>
    /// Parses the job properties dictionary and converts into a model. If required fields are missing, will report a job failure
    /// with list of missing properties.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="properties"></param>
    /// <param name="jobHistoryId"></param>
    /// <returns></returns>
    public static JobOperation<ReenrollmentKeyPropertyFields> MapKeyPropertyFields(ILogger logger, Dictionary<string, object> properties, long? jobHistoryId)
    {
        logger.MethodEntry();
        var errors = new StringBuilder();

        logger.LogTrace("Parsing subjectText from job properties");
        if (!properties.TryGetValue("subjectText", out var subjectText))
        {
            errors.AppendLine("SubjectText was not found in job properties. ");
        }

        logger.LogTrace("Parsing keyType from job properties");
        if (!properties.TryGetValue("keyType", out var keyType))
        {
            errors.AppendLine("KeyType was not found in job properties. ");
        }

        logger.LogTrace("Parsing keySize from job properties");
        if (!properties.TryGetValue("keySize", out var keySize))
        {
            errors.AppendLine("KeySize was not found in job properties. ");
        }

        string sans = null;
        logger.LogTrace("Parsing SANs from job properties");
        if (!properties.TryGetValue("SAN", out var sansValue))
        {
            logger.LogDebug("SANs not found in job properties");
        }
        else
        {
            logger.LogDebug($"Parsed SANs from job properties: {sansValue}");
            sans = sansValue.ToString();
        }

        if (errors.Length > 0)
        {
            logger.MethodExit();
            return JobOperation<ReenrollmentKeyPropertyFields>.Fail(errors.ToString(), jobHistoryId);
        }

        var result = new ReenrollmentKeyPropertyFields()
        {
            SubjectText = $"{subjectText}",
            KeySize = $"{keySize}",
            KeyType = $"{keyType}",
            SANs = sans,
        };

        logger.MethodExit();
        return JobOperation<ReenrollmentKeyPropertyFields>.Success(result);
    }
}
