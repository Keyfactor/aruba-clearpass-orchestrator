using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;

namespace ArubaClearPassOrchestrator;

public class Reenrollment : IReenrollmentJobExtension
{
    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Reenrollment";
    private readonly ILogger _logger = LogHandler.GetClassLogger<Reenrollment>();

    public Reenrollment()
    {
    }

    public Reenrollment(ILogger logger)
    {
        _logger = logger;
    }
    
    public JobResult ProcessJob(ReenrollmentJobConfiguration jobConfiguration, SubmitReenrollmentCSR submitReenrollmentUpdate)
    {
        throw new NotImplementedException();
    }
}
