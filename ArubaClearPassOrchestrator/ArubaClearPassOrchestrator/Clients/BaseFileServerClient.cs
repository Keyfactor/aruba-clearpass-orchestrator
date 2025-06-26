using System.Security.Cryptography.X509Certificates;

namespace ArubaClearPassOrchestrator.Clients;

public abstract class BaseFileServerClient
{
    protected string ConvertToPem(X509Certificate2 cert)
    {
        return "-----BEGIN CERTIFICATE-----\n" +
               Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks) +
               "\n-----END CERTIFICATE-----\n";
    }
}
