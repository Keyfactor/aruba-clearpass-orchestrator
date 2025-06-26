using ArubaClearPassOrchestrator.Clients.Interfaces;

namespace ArubaClearPassOrchestrator.Clients;

public class FileServerClientFactory : IFileServerClientFactory
{
    public IFileServerClient CreateFileServerClient(string type, string fileServerHost, string fileServerUsername,
        string fileServerPassword)
    {
        throw new NotImplementedException();
    }
}
