using ArubaClearPassOrchestrator.Models.Keyfactor;
using Keyfactor.Orchestrators.Extensions;
using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.UnitTests.Builders;

public class ReenrollmentJobConfigurationBuilder
{
    private string _clientMachine = "example.com";
    private string _storePath = "clearpass.localhost;HTTPS(RSA)";
    private ArubaCertificateStoreProperties _properties;
    private Dictionary<string, object> _jobProperties;
    private Dictionary<string, string[]> _sans;
    private long _jobHistoryId = 123;

    public ReenrollmentJobConfigurationBuilder()
    {
        _properties = new ArubaCertificateStoreProperties
        {
            FileServerType = "Amazon S3",
            FileServerHost = "bogus.com",
            FileServerUsername = "hocus",
            FileServerPassword = "pocus",
            DigestAlgorithm = "SHA-256",
        };
        
        _jobProperties = new Dictionary<string, object>
        {
            { "subjectText", "CN=com.example" },
            { "keyType", "RSA" },
            { "keySize", 2048 },
        };
    }

    public ReenrollmentJobConfigurationBuilder WithStorePath(string storePath)
    {
        _storePath = storePath;
        return this;
    }

    public ReenrollmentJobConfigurationBuilder WithSubjectText(string subjectText)
    {
        return WithJobProperty("subjectText", subjectText);
    }

    public ReenrollmentJobConfigurationBuilder WithJobProperties(Dictionary<string, object> properties)
    {
        _jobProperties = properties;
        return this;
    }

    public ReenrollmentJobConfigurationBuilder WithoutJobProperty(string key)
    {
        _jobProperties.Remove(key);
        return this;
    }

    public ReenrollmentJobConfigurationBuilder WithJobProperty(string key, object value)
    {
        _jobProperties[key] = value;
        return this;
    }

    public ReenrollmentJobConfigurationBuilder WithSans(Dictionary<string, string[]> sans)
    {
        _sans = sans;
        return this;
    }

    public ReenrollmentJobConfigurationBuilder WithFileServerType(string type)
    {
        _properties.FileServerType = type;
        return this;
    }

    public ReenrollmentJobConfiguration Build()
    {
        return new ReenrollmentJobConfiguration
        {
            CertificateStoreDetails = new CertificateStore
            {
                ClientMachine = _clientMachine,
                Properties = JsonConvert.SerializeObject(_properties),
                StorePath = _storePath,
            },
            ServerPassword = "ServerPassword",
            ServerUsername = "ServerUsername",
            JobProperties = _jobProperties,
            SANs = _sans,
            JobHistoryId = _jobHistoryId
        };
    }
}
