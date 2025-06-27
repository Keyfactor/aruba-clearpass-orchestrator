using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Keyfactor;

public class JobPropertyFields
{
    // [JsonProperty("subjectText")]
    public string CommonName { get; set; }
    
    // [JsonProperty("keyType")]
    public string KeyType { get; set; }
    
    // [JsonProperty("keySize")]
    public Int64 KeySize { get; set; }
}
