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

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Constants;

public static class KeyfactorConstants
{
    public static class JobProperties
    {
        public const string SubjectText = "subjectText";
        public const string KeyType = "keyType";
        public const string KeySize = "keySize";
        public const string San = "SAN";
    }
    
    public static class KeyTypes
    {
        public const string RSA = "RSA";
        public const string ECC = "ECC";
    }
    
    public static class RsaKeySizes
    {
        public const string Size2048 = "2048";
        public const string Size3072 = "3072";
        public const string Size4096 = "4096";
    }
    
    public static class EccCurves
    {
        // P-256 aliases
        public const string P256 = "P-256";
        public const string Prime256v1 = "prime256v1";
        public const string Secp256r1 = "secp256r1";
        public const string P256Combined = "P-256/prime256v1/secp256r1";
        
        // P-384 aliases
        public const string P384 = "P-384";
        public const string Secp384r1 = "secp384r1";
        public const string P384Combined = "P-384/secp384r1";
        
        // P-521 aliases
        public const string P521 = "P-521";
        public const string Secp521r1 = "secp521r1";
        public const string P521Combined = "P-521/secp521r1";
    }
    
    public static class SanTypes
    {
        public const string DnsName = "dnsname";
        public const string IpAddress = "ipaddress";
        public const string Email = "rfc822name";
        public const string Upn = "upn";
    }
}
