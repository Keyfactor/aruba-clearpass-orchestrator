namespace ArubaClearPassOrchestrator.Clients.Interfaces;

public interface IFileServerClientFactory
{
    public IFileServerClient CreateFileServerClient(string type, string fileServerHost, string fileServerUsername, string fileServerPassword);
}
