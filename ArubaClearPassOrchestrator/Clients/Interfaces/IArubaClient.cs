using ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Models.Aruba.ServerCert;

namespace ArubaClearPassOrchestrator.Clients.Interfaces;

public interface IArubaClient
{
    public Task<ICollection<ClusterServerItem>> GetClusterServers();
    public Task<GetServerCertificateResponse> GetServerCertificate(string serverUuid, string serviceName);
    public Task<CreateCertificateSignRequestResponse> CreateCertificateSignRequest(string subjectCN, string privateKeyType, string digestAlgorithm);
    public Task UpdateServerCertificate(string serverUuid, string serviceName, string certificateUrl);
    public Task EnableServerCertificate(string serverUuid, string serviceName);
    public Task DisableServerCertificate(string serverUuid, string serviceName);
}
