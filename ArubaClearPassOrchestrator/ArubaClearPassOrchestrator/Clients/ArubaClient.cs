using System.Text;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Aruba.ServerCert;
using ArubaClearPassOrchestrator.Models.Aruba.TokenEndpoint;
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
        
        _logger.LogDebug($"Provided hostname: {hostname}. Sanitized hostname: {sanitizedHostname}. Base URL: {_baseUrl}");
        
        Authenticate();
    }

    public ICollection<ClusterServerItem> GetClusterServers()
    {
        _logger.MethodEntry();
        
        var url = "/api/cluster/server";
        _logger.LogInformation($"Getting cluster servers from {_baseUrl}{url}");
        
        var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
        _logger.LogDebug($"Cluster server request complete. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");

        response.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Getting cluster servers request completed successfully.");
        
        var responseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var result = JsonConvert.DeserializeObject<GetClusterServerResponse>(responseJson);
        var items = result!.Embedded.Items;

        _logger.MethodExit();
        return items;
    }

    public GetServerCertificateResponse GetServerCertificate(string serverUuid, string serviceName)
    {
        throw new NotImplementedException();
    }

    public CreateCertificateSignRequestResponse CreateCertificateSignRequest(string servername, string privateKeyType,
        string digestAlgorithm)
    {
        throw new NotImplementedException();
    }

    public void UpdateServerCertificate(string serverUuid, string serviceName, string certificateUrl)
    {
        throw new NotImplementedException();
    }

    public void EnableServerCertificate(string serverUuid, string serviceName)
    {
        throw new NotImplementedException();
    }

    public void DisableServerCertificate(string serverUuid, string serviceName)
    {
        throw new NotImplementedException();
    }

    private void Authenticate()
    {
        // TODO: Wrap in a try-catch in case connection is refused
        
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
        
        _logger.LogDebug($"Access token request complete. Status code: {response.StatusCode}. Successful?: {response.IsSuccessStatusCode}");
        
        response.EnsureSuccessStatusCode(); // TODO: Maybe throw a custom exception?

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
}
