using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;

namespace ArubaClearPassOrchestrator;

public class Inventory : IInventoryJobExtension
{
    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Inventory";
    private readonly ILogger _logger = LogHandler.GetClassLogger<Inventory>();

    public Inventory()
    {
    }

    public Inventory(ILogger logger)
    {
        _logger = logger;
    }
    
    public JobResult ProcessJob(InventoryJobConfiguration jobConfiguration, SubmitInventoryUpdate submitInventoryUpdate)
    {
        throw new NotImplementedException();
    }
}
