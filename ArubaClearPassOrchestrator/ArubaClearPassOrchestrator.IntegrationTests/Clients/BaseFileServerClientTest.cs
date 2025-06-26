using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Tests.Common.Exceptions;
using ArubaClearPassOrchestrator.Tests.Common.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients;

[Trait("Category", "Integration")]
public abstract class BaseFileServerClientTest<T> : BaseIntegrationTest where T : IFileServerClient
{
    protected readonly T Client;
    protected readonly ILogger Logger;

    public BaseFileServerClientTest(ITestOutputHelper output, string type, string hostname, string username,
        string password)
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

    
}
