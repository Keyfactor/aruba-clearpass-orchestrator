using Microsoft.Extensions.Logging;

namespace ArubaClearPassOrchestrator.Clients.Interfaces;

public interface IFileServerClientFactory
{
    public IFileServerClient CreateFileServerClient(ILogger logger, string type, string fileServerHost, string fileServerUsername, string fileServerPassword);
}
