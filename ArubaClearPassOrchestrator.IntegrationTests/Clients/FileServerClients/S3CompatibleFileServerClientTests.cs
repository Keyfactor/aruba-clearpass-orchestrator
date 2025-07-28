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

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients.FileServerClients;

/// <summary>
/// Tests the integration with S3-compatible services such as Cloudian, MinIO, etc.
/// For a local example with MinIO, you can configure with Docker Compose.
/// Run from the project root: docker-compose -f local/docker-compose.minio.yml up --build
/// </summary>
public class S3CompatibleFileServerClientTests: BaseFileServerClientTest<S3FileServerClient>
{
    public S3CompatibleFileServerClientTests(ITestOutputHelper output) : base(
        output: output,
        type: "Amazon S3",
        hostname: GetEnvironmentVariable("S3_COMPATIBLE_BUCKET_NAME"),
        username: GetEnvironmentVariable("S3_COMPATIBLE_ACCESS_KEY"),
        password: GetEnvironmentVariable("S3_COMPATIBLE_SECRET_ACCESS_KEY"))
    {
        SkipTestUnlessEnvEnabled("S3_COMPATIBLE_RUN_TESTS");
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
