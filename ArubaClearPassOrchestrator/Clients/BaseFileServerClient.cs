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

using System.Security.Cryptography.X509Certificates;

namespace ArubaClearPassOrchestrator.Clients;

public abstract class BaseFileServerClient
{
    protected string ConvertToPem(X509Certificate2 cert)
    {
        return "-----BEGIN CERTIFICATE-----\n" +
               Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks) +
               "\n-----END CERTIFICATE-----\n";
    }
}
