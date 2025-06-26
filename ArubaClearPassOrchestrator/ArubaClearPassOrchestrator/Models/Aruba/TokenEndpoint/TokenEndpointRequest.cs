using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Aruba.TokenEndpoint;

public class TokenEndpointRequest
{
    [JsonProperty("grant_type")]
    public string GrantType { get; set; }
    
    [JsonProperty("client_id")]
    public string ClientId { get; set; }
    
    [JsonProperty("client_secret")]
    public string ClientSecret { get; set; }
}
