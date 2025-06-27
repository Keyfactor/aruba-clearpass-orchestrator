using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Tests.Common.Exceptions;
using ArubaClearPassOrchestrator.Tests.Common.TestUtilities;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests;

[Trait("Category", "Integration")]
public abstract class BaseIntegrationTest
{
    protected ILogger Logger;
    protected readonly Mock<IPAMSecretResolver> PamResolverMock = new ();
    
    public BaseIntegrationTest(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(new XunitLoggerProvider(output));
        });
        
        Logger = loggerFactory.CreateLogger<BaseIntegrationTest>();

        PamResolverMock.Setup(p => p.Resolve(It.IsAny<string>())).Returns("bogus");
    }
    
    protected void SkipTestUnlessEnvEnabled(string envName)
    {
        var enabled = Environment.GetEnvironmentVariable(envName);
        if (enabled != "1")
        {
            throw new SkipTestException($"Env variable {envName} != '1'. Value: '{enabled}'");
        }
    }

    protected IArubaClient GetArubaClient()
    {
        var arubaHost = Environment.GetEnvironmentVariable("ARUBA_HOST");
        var arubaClientId = Environment.GetEnvironmentVariable("ARUBA_CLIENT_ID");
        var arubaClientSecret = Environment.GetEnvironmentVariable("ARUBA_CLIENT_SECRET");
        
        return new ArubaClient(Logger, false, arubaHost, arubaClientId, arubaClientSecret);
    }
}
