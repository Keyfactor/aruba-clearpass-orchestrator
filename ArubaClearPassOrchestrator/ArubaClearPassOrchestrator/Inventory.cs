using ArubaClearPassOrchestrator.Clients.Interfaces;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArubaClearPassOrchestrator;

public class Inventory : IInventoryJobExtension
{
    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Inventory";
    private readonly ILogger _logger = LogHandler.GetClassLogger<Inventory>();
    
    private IArubaClient _arubaClient;
    private readonly IPAMSecretResolver _resolver;

    public Inventory(IPAMSecretResolver resolver)
    {
        _resolver = resolver;
    }

    public Inventory(ILogger logger, IArubaClient arubaClient, IPAMSecretResolver resolver)
    {
        _logger = logger;
        _arubaClient = arubaClient;
        _resolver = resolver;
    }
    
    public JobResult ProcessJob(InventoryJobConfiguration jobConfiguration, SubmitInventoryUpdate submitInventoryUpdate)
    {
        throw new NotImplementedException();
    }
}
