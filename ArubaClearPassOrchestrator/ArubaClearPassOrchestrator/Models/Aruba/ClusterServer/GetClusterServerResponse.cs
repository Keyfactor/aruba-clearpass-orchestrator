using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;

public class GetClusterServerResponse
{
    [JsonProperty("_embedded")]
    public EmbeddedServerItem Embedded { get; set; }
}

public class EmbeddedServerItem
{
    [JsonProperty("items")]
    public ICollection<ClusterServerItem> Items { get; set; }
}

public class ClusterServerItem
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("server_uuid")]
    public string ServerUuid { get; set; }
}
