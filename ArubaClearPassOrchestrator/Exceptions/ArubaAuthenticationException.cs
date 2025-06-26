namespace ArubaClearPassOrchestrator.Exceptions;

public class ArubaAuthenticationException : Exception
{
    public ArubaAuthenticationException(string message) : base($"An error occurred authenticating to the Aruba instance. Message: {message}")
    {
    }
}
