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
using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Mappers;

public static class ArubaEncryptionAlgorithmMapper
{
    /// <summary>
    /// Maps the key type and key size from the JobProperties to an Aruba-accepted encryption algorithm.
    /// Acceptable encryption algorithms can be found in the Aruba API documentation: https://developer.arubanetworks.com/cppm/reference/certsignrequestpost
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="keyType"></param>
    /// <param name="keySize"></param>
    /// <param name="jobHistoryId"></param>
    /// <returns></returns>
    /// 
    public static JobOperation<string> MapEncryptionAlgorithm(ILogger logger, string keyType, string keySize, long? jobHistoryId)
    {
        logger.MethodEntry();

        var acceptedArubaValues = new[]
        {
            ArubaClearPassConstants.EncryptionAlgorithms.Rsa2048, 
            ArubaClearPassConstants.EncryptionAlgorithms.Rsa3072, 
            ArubaClearPassConstants.EncryptionAlgorithms.Rsa4096, 
            
            ArubaClearPassConstants.EncryptionAlgorithms.Ecc256, 
            ArubaClearPassConstants.EncryptionAlgorithms.Ecc384, 
            ArubaClearPassConstants.EncryptionAlgorithms.Ecc521
        };
        string encryptionAlgorithm = null;

        switch (keyType)
        {
            case KeyfactorConstants.KeyTypes.RSA:
                switch (keySize)
                {
                    case KeyfactorConstants.RsaKeySizes.Size2048:
                        encryptionAlgorithm = ArubaClearPassConstants.EncryptionAlgorithms.Rsa2048;
                        break;
                    case KeyfactorConstants.RsaKeySizes.Size3072:
                        encryptionAlgorithm = ArubaClearPassConstants.EncryptionAlgorithms.Rsa3072;
                        break;
                    case KeyfactorConstants.RsaKeySizes.Size4096:
                        encryptionAlgorithm = ArubaClearPassConstants.EncryptionAlgorithms.Rsa4096;
                        break;
                }

                break;

            case KeyfactorConstants.KeyTypes.ECC:
                switch (keySize)
                {
                    case KeyfactorConstants.EccCurves.P256:
                    case KeyfactorConstants.EccCurves.Prime256v1:
                    case KeyfactorConstants.EccCurves.Secp256r1:
                    case KeyfactorConstants.EccCurves.P256Combined:
                        encryptionAlgorithm = ArubaClearPassConstants.EncryptionAlgorithms.Ecc256;
                        break;

                    case KeyfactorConstants.EccCurves.P384:
                    case KeyfactorConstants.EccCurves.Secp384r1:
                    case KeyfactorConstants.EccCurves.P384Combined:
                        encryptionAlgorithm = ArubaClearPassConstants.EncryptionAlgorithms.Ecc384;
                        break;

                    case KeyfactorConstants.EccCurves.P521:
                    case KeyfactorConstants.EccCurves.Secp521r1:
                    case KeyfactorConstants.EccCurves.P521Combined:
                        encryptionAlgorithm = ArubaClearPassConstants.EncryptionAlgorithms.Ecc521;
                        break;
                }

                break;
        }

        logger.MethodExit();

        // If no match found, return failure
        if (encryptionAlgorithm == null)
        {
            return JobOperation<string>.Fail(
                $"Unable to map key type '{keyType}' and key size '{keySize}' to an accepted Aruba mapping. Aruba allows the following algorithms: {string.Join(", ", acceptedArubaValues)}",
                jobHistoryId);
        }

        return JobOperation<string>.Success(encryptionAlgorithm);
    }
}
