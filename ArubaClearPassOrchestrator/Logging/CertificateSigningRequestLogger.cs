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

using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Logging;

public class CertificateSigningRequestLogger
{
    /// <summary>
    /// To troubleshoot failures sending CSR to Command, this utility function will help log the CSR metadata
    /// </summary>
    /// <param name="csrPem"></param>
    public static void ParseCsrMetadata(ILogger logger, string csrPem)
    {
        logger.MethodEntry();
        // Read CSR
        Pkcs10CertificationRequest csr;
        using (var reader = new StringReader(csrPem))
        {
            var pemReader = new PemReader(reader);
            csr = (Pkcs10CertificationRequest)pemReader.ReadObject();
        }

        logger.LogDebug("===== Parsed CSR Information =====");

        // Subject
        var subject = csr.GetCertificationRequestInfo().Subject;
        logger.LogDebug($"Subject: {subject}");

        // Public Key
        AsymmetricKeyParameter publicKey = csr.GetPublicKey();
        var pubKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
        var alg = pubKeyInfo.AlgorithmID.Algorithm.Id;

        logger.LogDebug($"Public Key Algorithm: {alg}");
        if (publicKey is Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters rsa)
        {
            logger.LogDebug($"RSA Key Size: {rsa.Modulus.BitLength} bits");
        }

        // Signature algorithm
        var sigAlg = csr.SignatureAlgorithm.Algorithm.Id;
        logger.LogDebug($"Signature Algorithm: {sigAlg}");

        logger.LogDebug("==================================");
        logger.MethodExit();
    }
}
