using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.Tests.Common.TestUtilities;

public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;

    public XunitLogger(string _, ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception exception, 
        Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine($"[{logLevel}] {formatter(state, exception)}");

        if (exception != null)
        {
            _output.WriteLine(exception.ToString());
        }
    }
}