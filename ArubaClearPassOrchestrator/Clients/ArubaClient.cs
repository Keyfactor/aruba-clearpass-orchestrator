using System.Text;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Exceptions;
using ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Aruba.ServerCert;
using ArubaClearPassOrchestrator.Models.Aruba.TokenEndpoint;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Security;

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

        response.EnsureSuccessStatusCode();

        _logger.LogDebug("Getting cluster servers request completed successfully.");

        var responseJson = await response.Content.ReadAsStringAsync();
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

        response.EnsureSuccessStatusCode();

        _logger.LogDebug("Server certificate request completed successfully.");

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetServerCertificateResponse>(responseJson);

        _logger.MethodExit();
        return result;
    }

    public async Task<CreateCertificateSignRequestResponse> CreateCertificateSignRequest(string servername,
        string privateKeyType,
        string digestAlgorithm)
    {
        _logger.MethodEntry();

        var url = $"/api/cert-sign-request";
        _logger.LogDebug($"Creating a certificate signing request to {_baseUrl}{url}");

        var password = GenerateSecurePassword(16);
        var request = new CreateCertificateSignRequestRequest()
        {
            SubjectCN = servername,
            PrivateKeyPassword = password,
            PrivateKeyType = privateKeyType,
            DigestAlgorithm = digestAlgorithm
        };

        var response = await _httpClient.PostAsync(url,
            new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
        _logger.LogDebug(
            $"Certificate signing request complete. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        response.EnsureSuccessStatusCode();

        _logger.LogDebug($"Certificate signing request completed successfully.");

        var responseJson = await response.Content.ReadAsStringAsync();
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

        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            $"Server certificate for server UUID {serverUuid} on service {serviceName} updated successfully.");

        _logger.MethodExit();
    }

    public async Task EnableServerCertificate(string serverUuid, string serviceName)
    {
        _logger.MethodEntry();

        var url = $"/api/server-cert/name/{serverUuid}/{serviceName}/enable";
        _logger.LogDebug($"Enabling server certificate at {_baseUrl}{url}");

        var response = await _httpClient.PatchAsync(url, null);
        _logger.LogDebug(
            $"Server certificate enable request completed successfully. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        response.EnsureSuccessStatusCode();

        _logger.MethodExit();
    }

    public async Task DisableServerCertificate(string serverUuid, string serviceName)
    {
        _logger.MethodEntry();

        var url = $"/api/server-cert/name/{serverUuid}/{serviceName}/enable";
        _logger.LogDebug($"Disabling server certificate at {_baseUrl}{url}");

        var response = await _httpClient.PatchAsync(url, null);
        _logger.LogDebug(
            $"Server certificate disable request completed successfully. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        response.EnsureSuccessStatusCode();

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
            var httpMessage = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new ArubaAuthenticationException(httpMessage);
        }

        var responseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
    
    private string GenerateSecurePassword(int length)
    {
        _logger.MethodEntry();
        _logger.LogDebug($"Generating a secure password with {length} characters");
        const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_";
        var random = new SecureRandom();
        var password = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(allowedChars.Length);
            password.Append(allowedChars[index]);
        }
        
        _logger.MethodExit();
        return password.ToString();
    }
}
