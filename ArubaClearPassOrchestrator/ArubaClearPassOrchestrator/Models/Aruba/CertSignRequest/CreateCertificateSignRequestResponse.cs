using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;

public class CreateCertificateSignRequestResponse
{
    [JsonProperty("cert_sign_request")]
    public string CertificateSignRequest { get; set; }
}
