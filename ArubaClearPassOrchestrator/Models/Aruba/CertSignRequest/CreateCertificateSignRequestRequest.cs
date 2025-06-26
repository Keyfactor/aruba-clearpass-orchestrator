using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;

public class CreateCertificateSignRequestRequest
{
    [JsonProperty("subject_CN")]
    public string SubjectCN { get; set; }
    
    [JsonProperty("private_key_password")]
    public string PrivateKeyPassword { get; set; }
    
    [JsonProperty("private_key_type")]
    public string PrivateKeyType { get; set; }
    
    [JsonProperty("digest_algorithm")]
    public string DigestAlgorithm { get; set; }
}
