using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Aruba.ServerCert;

public class UpdateServerCertificateRequest
{
    [JsonProperty("certificate_url")]
    public string CertificateUrl { get; set; }
}
