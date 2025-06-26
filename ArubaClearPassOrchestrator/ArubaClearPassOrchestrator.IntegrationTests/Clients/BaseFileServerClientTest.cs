using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.UnitTests.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients;

public abstract class BaseFileServerClientTest<T> where T : IFileServerClient
{
    protected readonly T Client;
    protected readonly ILogger Logger;
    
    public BaseFileServerClientTest(ITestOutputHelper output, string type, string hostname, string username, string password)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(new XunitLoggerProvider(output));
        });
        
        Logger = loggerFactory.CreateLogger<BaseFileServerClientTest<T>>();
        
        var fileServerClientFactory = new FileServerClientFactory();
        var client = fileServerClientFactory.CreateFileServerClient(Logger, type, hostname, username, password);
        
        if (client == null)
        {
            throw new NotImplementedException(
                $"File server type {type} was not implemented in the file server factory");
        }

        Assert.IsAssignableFrom<T>(client);

        Client = (T)client;
    }
    
    protected X509Certificate2 FromPemString(string pem)
    {
        var base64 = ExtractBase64FromPem(pem);
        var rawData = Convert.FromBase64String(base64);
        return new X509Certificate2(rawData);
    }
    
    protected string ExtractBase64FromPem(string pem)
    {
        var match = Regex.Match(pem, "-----BEGIN CERTIFICATE-----([^-]*)-----END CERTIFICATE-----", RegexOptions.Singleline);
        if (!match.Success)
        {
            throw new FormatException("The PEM string does not contain a valid certificate.");
        }

        return match.Groups[1].Value
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();
    }
}
