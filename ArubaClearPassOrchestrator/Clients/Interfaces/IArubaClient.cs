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

using ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Aruba.ServerCert;
using ArubaClearPassOrchestrator.Models.Keyfactor;

namespace ArubaClearPassOrchestrator.Clients.Interfaces;

public interface IArubaClient
{
    /// <summary>
    /// Retrieves a list of cluster servers from the Aruba ClearPass API.
    /// </summary>
    /// <returns>A list of ClusterServerItem</returns>
    public Task<ICollection<ClusterServerItem>> GetClusterServers();
    
    /// <summary>
    /// Retrieves a server certificate based on the server's UUID and service name (HTTPS(RSA), HTTPS(ECC), etc.).
    /// </summary>
    /// <returns>A GetServerCertificateResponse object</returns>
    public Task<GetServerCertificateResponse> GetServerCertificate(string serverUuid, string serviceName);
    
    /// <summary>
    /// Creates a certificate signing request (CSR) using the specified subject information, private key type, and digest algorithm
    /// by interacting with the Aruba ClearPass API. The resulting CSR can be submitted to a Certificate Authority (CA) to issue a certificate.
    /// Note: The private key is generated and stored securely within Aruba ClearPass. The private key password is not returned in the response.
    /// </summary>
    /// <param name="subjectInformation">The subject details to include in the CSR, such as Common Name (CN), Organization (O), Country (C), etc. 
    /// These values are encoded into the Distinguished Name (DN) of the CSR.</param>
    /// <param name="sans">A string of SAN values (i.e. dns=example.com&ip=10.0.0.1)</param>
    /// <param name="privateKeyPassword">A password used to encrypt the generated private key stored in Aruba ClearPass. 
    /// This password is not included in the response and must be securely stored by the caller.</param>
    /// <param name="privateKeyType">The type of private key to generate for the CSR. This determines the key algorithm used in the request. Example values are "2048-bit rsa" and "nist/secg curve over a 256 bit prime field". Please consult the Aruba ClearPass API for possible values. https://developer.arubanetworks.com/cppm/reference/certsignrequestpost</param>
    /// <param name="digestAlgorithm">The hash algorithm used to sign the CSR. This determines the digest method applied during CSR creation. Example values include "SHA-1" or "SHA-512". Please consult the Aruba ClearPass API for possible values. https://developer.arubanetworks.com/cppm/reference/certsignrequestpost</param>
    /// <returns>A CreateCertificateSignRequestResponse object</returns>
    public Task<CreateCertificateSignRequestResponse> CreateCertificateSignRequest(CertificateSubjectInformation subjectInformation, string sans, string privateKeyPassword, string privateKeyType, string digestAlgorithm);
    
    /// <summary>
    /// Updates a server certificate in Aruba ClearPass for the provided server UUID and service name (i.e. HTTPS(RSA), HTTPS(ECC), etc.)
    /// with the provided certificate URL. The certificate URL must be accessible by the Aruba ClearPass server.
    /// </summary>
    /// <param name="serverUuid">The UUID of the server to update</param>
    /// <param name="serviceName">The service name of the server you wish to update</param>
    /// <param name="certificateUrl">An HTTPS URL for the certificate contents.</param>
    /// <returns></returns>
    public Task UpdateServerCertificate(string serverUuid, string serviceName, string certificateUrl);
}
