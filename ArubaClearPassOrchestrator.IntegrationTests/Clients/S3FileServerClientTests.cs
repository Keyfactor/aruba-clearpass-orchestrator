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
using ArubaClearPassOrchestrator.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients;

public class S3FileServerClientTests : BaseFileServerClientTest<S3FileServerClient>
{
    public S3FileServerClientTests(ITestOutputHelper output) : base(
        output: output,
        type: "S3",
        hostname: Environment.GetEnvironmentVariable("S3_BUCKET_NAME"),
        username: Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
        password: Environment.GetEnvironmentVariable("S3_SECRET_ACCESS_KEY"))
    {
        SkipTestUnlessEnvEnabled("S3_RUN_TESTS");
    }
    
    [Fact]
    public async Task UploadCertificate_WhenACertificateIsUploaded_ReturnsAPreSignedUrl()
    {
        var certificate = CertificateGenerator.GenerateCertificate("com.example", "foobarbaz", true);
        var certificateUrl = await Client.UploadCertificate("test_example.pfx", certificate);
        Assert.NotNull(certificateUrl);
        Logger.LogInformation($"Certificate URL: {certificateUrl}");
    }
    
    
}
