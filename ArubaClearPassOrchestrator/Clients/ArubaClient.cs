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
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Exceptions;
using ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Aruba.ServerCert;
using ArubaClearPassOrchestrator.Models.Aruba.TokenEndpoint;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Clients;

public class ArubaClient : IArubaClient
{
    private readonly string _baseUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private readonly ILogger _logger;
    private readonly HttpClientHandler _handler;

    private string _accessToken;
    private HttpClient _httpClient;

    public ArubaClient(ILogger logger, bool useSsl, string hostname, string clientId, string clientSecret)
    {
        var sanitizedHostname = hostname.Replace("https://", "").Replace("http://", "");

        _baseUrl = $"https://{sanitizedHostname}";
        _clientId = clientId;
        _clientSecret = clientSecret;

        _logger = logger;

        _handler = new HttpClientHandler();

        if (!useSsl)
        {
            _logger.LogWarning("Disabling SSL validation for Aruba client");
            _handler.ServerCertificateCustomValidationCallback = ((message, certificate2, arg3, arg4) => true);
        }

        _logger.LogDebug(
            $"Provided hostname: {hostname}. Sanitized hostname: {sanitizedHostname}. Base URL: {_baseUrl}");

        Authenticate();
    }

    public async Task<ICollection<ClusterServerItem>> GetClusterServers()
    {
        _logger.MethodEntry();

        var url = "/api/cluster/server";
        _logger.LogDebug($"Getting cluster servers from {_baseUrl}{url}");

        var response = await _httpClient.GetAsync(url);
        _logger.LogDebug(
            $"Cluster server request complete. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        EnsureRequestSucceeded(response);

        _logger.LogDebug("Getting cluster servers request completed successfully.");

        ReadHttpResponseContent(response, out var responseJson);
        var result = JsonConvert.DeserializeObject<GetClusterServerResponse>(responseJson);
        var items = result!.Embedded.Items;

        _logger.LogDebug($"Cluster server data: {JsonConvert.SerializeObject(items)}");

        _logger.MethodExit();
        return items;
    }

    public async Task<GetServerCertificateResponse> GetServerCertificate(string serverUuid, string serviceName)
    {
        _logger.MethodEntry();

        var url = $"/api/server-cert/name/{serverUuid}/{serviceName}";
        _logger.LogDebug($"Getting server certificate from {_baseUrl}{url}");

        var response = await _httpClient.GetAsync(url);
        _logger.LogDebug(
            $"Server certificate request complete. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        EnsureRequestSucceeded(response);

        _logger.LogDebug("Server certificate request completed successfully.");

        ReadHttpResponseContent(response, out var responseJson);
        var result = JsonConvert.DeserializeObject<GetServerCertificateResponse>(responseJson);

        _logger.MethodExit();
        return result;
    }

    public async Task<CreateCertificateSignRequestResponse> CreateCertificateSignRequest(CertificateSubjectInformation subjectInformation,
        string privateKeyPassword,
        string privateKeyType,
        string digestAlgorithm)
    {
        _logger.MethodEntry();

        var url = $"/api/cert-sign-request";
        _logger.LogDebug($"Creating a certificate signing request to {_baseUrl}{url}");
        
        var request = new CreateCertificateSignRequestRequest()
        {
            SubjectCN = subjectInformation.CommonName,
            SubjectO = subjectInformation.Organization,
            SubjectOU = subjectInformation.OrganizationalUnit,
            SubjectC = subjectInformation.CountryRegion,
            SubjectL = subjectInformation.CityLocality,
            SubjectST = subjectInformation.StateProvince,
            PrivateKeyPassword = privateKeyPassword,
            PrivateKeyType = privateKeyType,
            DigestAlgorithm = digestAlgorithm
        };

        var response = await _httpClient.PostAsync(url,
            new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
        _logger.LogDebug(
            $"Certificate signing request complete. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        EnsureRequestSucceeded(response);

        _logger.LogDebug($"Certificate signing request completed successfully.");

        ReadHttpResponseContent(response, out var responseJson);
        var result = JsonConvert.DeserializeObject<CreateCertificateSignRequestResponse>(responseJson);

        _logger.MethodExit();
        return result;
    }

    public async Task UpdateServerCertificate(string serverUuid, string serviceName, string certificateUrl)
    {
        _logger.MethodEntry();

        var url = $"/api/server-cert/name/{serverUuid}/{serviceName}";
        _logger.LogDebug($"Updating server certificate at {_baseUrl}{url}");

        var request = new UpdateServerCertificateRequest()
        {
            CertificateUrl = certificateUrl
        };

        var response = await _httpClient.PutAsync(url,
            new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
        _logger.LogDebug(
            $"Server certificate update request complete. Status code {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        EnsureRequestSucceeded(response);

        _logger.LogInformation(
            $"Server certificate for server UUID {serverUuid} on service {serviceName} updated successfully.");

        _logger.MethodExit();
    }

    private void Authenticate()
    {
        _logger.MethodEntry();

        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri(_baseUrl)
        };

        var url = "/api/oauth";

        var body = new TokenEndpointRequest()
        {
            GrantType = "client_credentials",
            ClientId = _clientId,
            ClientSecret = _clientSecret
        };

        var json = JsonConvert.SerializeObject(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation($"Sending access token request to {_baseUrl}{url}");

        var response = httpClient.PostAsync(url, content).GetAwaiter().GetResult();

        _logger.LogDebug(
            $"Access token request complete. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            ReadHttpResponseContent(response, out var errorMessage);
            
            throw new ArubaAuthenticationException(errorMessage);
        }

        ReadHttpResponseContent(response, out var responseJson);
        var authenticationResponse = JsonConvert.DeserializeObject<TokenEndpointResponse>(responseJson);

        _accessToken = authenticationResponse!.AccessToken;

        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri(_baseUrl),
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _logger.LogInformation("Sending access token request completed successfully. Authentication successful.");

        _logger.MethodExit();
    }

    private void EnsureRequestSucceeded(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ReadHttpResponseContent(response, out var errorMessage);
        
        _logger.LogError($"The Aruba API request did not succeed. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}. Error: {errorMessage}");
        _logger.LogError($"API URL that failed: {response.RequestMessage?.Method.Method} {response.RequestMessage?.RequestUri}");
        throw new HttpRequestException($"The Aruba API request did not succeed. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}. Error: {errorMessage}");
    }
    
    private void ReadHttpResponseContent(HttpResponseMessage response, out string content)
    {
        content = "";
        try
        {
            content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"An error occurred reading content from the Aruba API response message: {ex.Message}");
        }
    }
}
