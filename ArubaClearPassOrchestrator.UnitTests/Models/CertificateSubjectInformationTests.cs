using ArubaClearPassOrchestrator.Models.Keyfactor;

namespace ArubaClearPassOrchestrator.UnitTests.Models;

public class CertificateSubjectInformationTests
{
    [Fact]
    public void ParseFromSubjectText_WhenCompleteSubjectTextIsProvided_ParsesCommonName()
    {
        var subjectText = "CN=commonname,E=test@example.com,O=organization,OU=ou,L=city,ST=state,C=CR";
        var result = CertificateSubjectInformation.ParseFromSubjectText(subjectText);
        
        Assert.Equal("commonname", result.CommonName);
    }
    
    [Fact]
    public void ParseFromSubjectText_WhenCompleteSubjectTextIsProvided_ParsesEmail()
    {
        var subjectText = "CN=commonname,E=test@example.com,O=organization,OU=ou,L=city,ST=state,C=CR";
        var result = CertificateSubjectInformation.ParseFromSubjectText(subjectText);
        
        Assert.Equal("test@example.com", result.Email);
    }
    
    [Fact]
    public void ParseFromSubjectText_WhenCompleteSubjectTextIsProvided_ParsesOrganization()
    {
        var subjectText = "CN=commonname,E=test@example.com,O=organization,OU=ou,L=city,ST=state,C=CR";
        var result = CertificateSubjectInformation.ParseFromSubjectText(subjectText);
        
        Assert.Equal("organization", result.Organization);
    }
    
    [Fact]
    public void ParseFromSubjectText_WhenCompleteSubjectTextIsProvided_ParsesOrganizationalUnit()
    {
        var subjectText = "CN=commonname,E=test@example.com,O=organization,OU=ou,L=city,ST=state,C=CR";
        var result = CertificateSubjectInformation.ParseFromSubjectText(subjectText);
        
        Assert.Equal("ou", result.OrganizationalUnit);
    }
    
    [Fact]
    public void ParseFromSubjectText_WhenCompleteSubjectTextIsProvided_ParsesCityLocality()
    {
        var subjectText = "CN=commonname,E=test@example.com,O=organization,OU=ou,L=city,ST=state,C=CR";
        var result = CertificateSubjectInformation.ParseFromSubjectText(subjectText);
        
        Assert.Equal("city", result.CityLocality);
    }
    
    [Fact]
    public void ParseFromSubjectText_WhenCompleteSubjectTextIsProvided_ParsesCountryRegion()
    {
        var subjectText = "CN=commonname,E=test@example.com,O=organization,OU=ou,L=city,ST=state,C=CR";
        var result = CertificateSubjectInformation.ParseFromSubjectText(subjectText);
        
        Assert.Equal("CR", result.CountryRegion);
    }
    
    [Fact]
    public void ParseFromSubjectText_WhenFieldIsEmpty_SetsFieldValueToNull()
    {
        var subjectText = "CN=commonname,O=organization,OU=ou,L=city,ST=state,C=CR";
        var result = CertificateSubjectInformation.ParseFromSubjectText(subjectText);
        
        Assert.Null(result.Email);
    }
}
