using Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests;

public class InventoryTests : BaseIntegrationTest
{
    private readonly Inventory _sut;
    
    public InventoryTests(ITestOutputHelper output) : base(output)
    {
        SkipTestUnlessEnvEnabled("INVENTORY_RUN_TESTS");

        var arubaClient = GetArubaClient();

        _sut = new Inventory(Logger, arubaClient, PamResolverMock.Object);
    }

    [Fact]
    public void Hello()
    {
        
    }
}
