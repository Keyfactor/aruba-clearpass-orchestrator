using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.UnitTests;

public class ReenrollmentTests : BaseOrchestratorTest
{
    private readonly Reenrollment _sut;

    public ReenrollmentTests(ITestOutputHelper output) : base(output)
    {
        _sut = new Reenrollment(Logger);
    }

    [Fact]
    public void ExtensionName_MatchesExpectedValue()
    {
        Assert.Equal("Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Reenrollment", _sut.ExtensionName);
    }
}
