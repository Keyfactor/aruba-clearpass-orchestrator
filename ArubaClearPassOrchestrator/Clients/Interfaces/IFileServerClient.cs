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

namespace ArubaClearPassOrchestrator.Clients.Interfaces;

public interface IFileServerClient
{
    /// <summary>
    /// Uploads a certificate to the file server and returns the URL where it can be accessed.
    /// </summary>
    /// <param name="fileName">The name of the file (not including file extension)</param>
    /// <param name="certificate">The X509Certificate2 certificate to store in the file server</param>
    /// <returns></returns>
    public Task<string> UploadCertificate(string fileName, X509Certificate2 certificate);
}
