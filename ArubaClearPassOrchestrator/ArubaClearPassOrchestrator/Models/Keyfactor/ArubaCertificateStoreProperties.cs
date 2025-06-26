namespace ArubaClearPassOrchestrator.Models.Keyfactor;

public class ArubaCertificateStoreProperties
{
    public string ServerUseSsl { get; set; }
    public bool ServerUseSslEnabled => ServerUseSsl == "true";
    public string ServiceName { get; set; }
}
