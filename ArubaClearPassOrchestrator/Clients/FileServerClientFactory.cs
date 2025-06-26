using ArubaClearPassOrchestrator.Clients.Interfaces;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace ArubaClearPassOrchestrator.Clients;

public class FileServerClientFactory : IFileServerClientFactory
{
    public IFileServerClient CreateFileServerClient(ILogger logger, string type, string fileServerHost, string fileServerUsername,
        string fileServerPassword)
    {
        logger.MethodEntry();
        
        IFileServerClient? fileServerClient = null;

        switch (type)
        {
            case "S3":
                fileServerClient = new S3FileServerClient(logger, fileServerHost, fileServerUsername, fileServerPassword);
                break;
            default:
                logger.LogWarning($"No server type mapping found for '{type}'. Returning a null file server client.");
                break;
        }

        return fileServerClient;
    }
}
