using Xunit.Sdk;

namespace ArubaClearPassOrchestrator.Tests.Common.Exceptions;

public class SkipTestException : XunitException
{
    public SkipTestException(string reason) : base($"Test skipped: {reason}") { }
}
