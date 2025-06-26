using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Tests.Common.TestUtilities;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.UnitTests;

public abstract class BaseOrchestratorTest
{
    protected readonly ILogger Logger;
    protected readonly Mock<IArubaClient> ArubaClientMock = new();
    protected readonly Mock<IPAMSecretResolver> PAMResolverMock = new ();

    protected BaseOrchestratorTest(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(new XunitLoggerProvider(output));
        });
        
        Logger = loggerFactory.CreateLogger<BaseOrchestratorTest>();
        
        PAMResolverMock.Setup(p => p.Resolve("ServerUsername")).Returns("foo");
        PAMResolverMock.Setup(p => p.Resolve("ServerPassword")).Returns("bar");
    }
}
