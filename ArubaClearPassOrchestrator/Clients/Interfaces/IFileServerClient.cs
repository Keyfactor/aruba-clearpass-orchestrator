using System.Security.Cryptography.X509Certificates;

namespace ArubaClearPassOrchestrator.Clients.Interfaces;

public interface IFileServerClient
{
    public Task<string> UploadCertificate(string key, X509Certificate2 certificate);
}
