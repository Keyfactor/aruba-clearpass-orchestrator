using System.Security.Cryptography.X509Certificates;
using ArubaClearPassOrchestrator.Tests.Common.Generators;

namespace ArubaClearPassOrchestrator.UnitTests;

/// <summary>
/// A context that will be shared across all tests in a test class. This will help minimize expensive operations happening for every test.
/// </summary>
public class SharedTestContext
{
    public X509Certificate2 TestCertificate;

    public SharedTestContext()
    {
        TestCertificate = CertificateGenerator.GenerateCertificate("com.example", "foobar", true);
    }
}
