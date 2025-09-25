// Copyright 2025 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using ArubaClearPassOrchestrator.Clients;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Keyfactor;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients;

public class ArubaClientTests : BaseIntegrationTest
{
    private readonly IArubaClient _sut;
    
    private readonly string _serverUuid;
    private readonly string _serviceName;
    
    public ArubaClientTests(ITestOutputHelper output) : base(output)
    {
        SkipTestUnlessEnvEnabled("ARUBA_RUN_TESTS");
        
        var arubaHost = GetEnvironmentVariable("ARUBA_HOST");
        var arubaClientId = GetEnvironmentVariable("ARUBA_CLIENT_ID");
        var arubaClientSecret = GetEnvironmentVariable("ARUBA_CLIENT_SECRET");
        
        _serverUuid = GetEnvironmentVariable("ARUBA_SERVER_UUID");
        _serviceName = GetEnvironmentVariable("ARUBA_SERVICE_NAME");

        
        _sut = new ArubaClient(Logger, false, arubaHost, arubaClientId, arubaClientSecret);
    }

    [Fact]
    public async Task GetClusterServers_WhenCalled_ShouldReturnANonEmptyList()
    {
        var result = await _sut.GetClusterServers();
        Assert.NotEmpty(result);
    }
    
    [Fact]
    public async Task GetServerCertificate_WhenCalled_ShouldReturnAServerCertificate()
    {
        var result = await _sut.GetServerCertificate(_serverUuid, _serviceName);
        Assert.NotNull(result);
        Assert.NotEmpty(result.CertFile);
    }
    
    [Fact]
    public async Task CreateCertificateSignRequest_WhenCalled_ShouldReturnACertificateSigningRequest()
    {
        var subjectInformation = new CertificateSubjectInformation()
        {
            CommonName = "com.example.com",
            Organization = "org",
            OrganizationalUnit = "orgunit",
            CityLocality = "foo",
            StateProvince = "WY",
            CountryRegion = "US",
            Email = "test@example.com"
        };
        var result = await _sut.CreateCertificateSignRequest(subjectInformation, null, "F0oB4rB@z!", "2048-bit rsa", "SHA-512");
        Assert.NotNull(result);
        Assert.NotEmpty(result.CertificateSignRequest);
    }
}
