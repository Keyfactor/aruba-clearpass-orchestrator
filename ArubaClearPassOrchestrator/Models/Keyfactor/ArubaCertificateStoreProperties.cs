namespace ArubaClearPassOrchestrator.Models.Keyfactor;

public class ArubaCertificateStoreProperties
{
    public string ServerUseSsl { get; set; }
    public bool ServerUseSslEnabled => ServerUseSsl == "true";
    public string ServiceName { get; set; }
    public string FileServerType { get; set; }
    public string FileServerHost { get; set; }
    public string FileServerUsername { get; set; }
    public string FileServerPassword { get; set; }
    public string DigestAlgorithm { get; set; }
}
