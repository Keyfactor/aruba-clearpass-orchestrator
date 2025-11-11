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

public static class ArubaClearPassConstants
{
    public static class EncryptionAlgorithms
    {
        public const string Rsa2048 = "2048-bit rsa";
        public const string Rsa3072 = "3072-bit rsa";
        public const string Rsa4096 = "4096-bit rsa";
        public const string Ecc256 = "nist/secg curve over a 256 bit prime field";
        public const string Ecc384 = "nist/secg curve over a 384 bit prime field";
        public const string Ecc521 = "nist/secg curve over a 521 bit prime field";
        
        public static readonly string[] All = new[]
        {
            Rsa2048, Rsa3072, Rsa4096,
            Ecc256, Ecc384, Ecc521
        };
    }
    
    public static class SanPrefixes
    {
        public const string Dns = "DNS";
        public const string IpAddress = "IP";
        public const string Email = "email";
    }
    
    public static class Delimiters
    {
        public const char Sans = ',';
    }
}