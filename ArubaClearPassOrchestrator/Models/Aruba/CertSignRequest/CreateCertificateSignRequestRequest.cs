using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;

public class CreateCertificateSignRequestRequest
{
    [JsonProperty("subject_CN")]
    public string SubjectCN { get; set; }
    
    [JsonProperty("subject_O")]
    public string SubjectO { get; set; }
    
    [JsonProperty("subject_OU")]
    public string SubjectOU { get; set; }
    
    [JsonProperty("subject_L")]
    public string SubjectL { get; set; }
    
    [JsonProperty("subject_ST")]
    public string SubjectST { get; set; }
    
    [JsonProperty("subject_C")]
    public string SubjectC { get; set; }
    
    [JsonProperty("private_key_password")]
    public string PrivateKeyPassword { get; set; }
    
    [JsonProperty("private_key_type")]
    public string PrivateKeyType { get; set; }
    
    [JsonProperty("digest_algorithm")]
    public string DigestAlgorithm { get; set; }
}
