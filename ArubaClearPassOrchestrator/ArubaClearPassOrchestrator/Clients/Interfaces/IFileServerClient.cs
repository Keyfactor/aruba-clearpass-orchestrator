using System.Security.Cryptography.X509Certificates;

namespace ArubaClearPassOrchestrator.Clients.Interfaces;

public interface IFileServerClient
{
    public string UploadCertificate(string key, X509Certificate2 certificate);
}
