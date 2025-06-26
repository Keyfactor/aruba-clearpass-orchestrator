using ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Aruba.ServerCert;

namespace ArubaClearPassOrchestrator.Clients.Interfaces;

public interface IArubaClient
{
    public ICollection<ClusterServerItem> GetClusterServers(string servername);
    public GetServerCertificateResponse GetServerCertificate(string serverUuid, string serviceName);
    public CreateCertificateSignRequestResponse CreateCertificateSignRequest(string servername, string privateKeyType, string digestAlgorithm);
    public void UpdateServerCertificate(string serverUuid, string serviceName, string certificateUrl);
    public void EnableServerCertificate(string serverUuid, string serviceName);
    public void DisableServerCertificate(string serverUuid, string serviceName);
}
