using ArubaClearPassOrchestrator.Tests.Common.Exceptions;

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients;

public abstract class BaseIntegrationTest
{
    protected void SkipTestUnlessEnvEnabled(string envName)
    {
        var enabled = Environment.GetEnvironmentVariable(envName);
        if (enabled != "1")
        {
            throw new SkipTestException($"Env variable {envName} != '1'. Value: '{enabled}'");
        }
    }
}
