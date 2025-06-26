using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Aruba.ServerCert;

public class GetServerCertificateResponse
{
    [JsonProperty("service_id")]
    public int ServiceId { get; set; }
    
    [JsonProperty("service_name")]
    public string ServiceName { get; set; }
    
    [JsonProperty("certificate_type")]
    public string CertificateType { get; set; }
    
    [JsonProperty("subject")]
    public string Subject { get; set; }
    
    [JsonProperty("expiry_date")]
    public string ExpiryDate { get; set; }
    
    [JsonProperty("issue_date")]
    public string IssueDate { get; set; }
    
    [JsonProperty("issued_by")]
    public string IssuedBy { get; set; }
    
    [JsonProperty("root_ca_cert")]
    public CertificateAuthorityInformation RootCaCert { get; set; }
    
    [JsonProperty("intermediate_ca_cert")]
    public ICollection<CertificateAuthorityInformation> IntermediateCaCerts { get; set; }
    
    [JsonProperty("validity")]
    public string Validity { get; set; }
    
    [JsonProperty("cert_file")]
    public string CertFile { get; set; }
    
    [JsonProperty("enabled")]
    public bool Enabled { get; set; }
    
    [JsonProperty("public_key_algorithm")]
    public string PublicKeyAlgorithm { get; set; }
}

public class CertificateAuthorityInformation
{
    [JsonProperty("subject")]
    public string Subject { get; set; }
    
    [JsonProperty("expiry_date")]
    public string ExpiryDate { get; set; }

    [JsonProperty("issue_date")]
    public string IssueDate { get; set; }

    [JsonProperty("issued_by")]
    public string IssuedBy { get; set; }

    [JsonProperty("validity")]
    public string Validity { get; set; }

    [JsonProperty("public_key_algorithm")]
    public string PublicKeyAlgorithm { get; set; }
}
