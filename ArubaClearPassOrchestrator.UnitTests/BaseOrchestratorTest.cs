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

using ArubaClearPassOrchestrator.Clients.Interfaces;
using ArubaClearPassOrchestrator.Models.Aruba.ClusterServer;
using ArubaClearPassOrchestrator.Tests.Common.TestUtilities;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.UnitTests;

public abstract class BaseOrchestratorTest
{
    protected readonly ILogger Logger;
    protected readonly Mock<IArubaClient> ArubaClientMock = new();
    protected readonly Mock<IPAMSecretResolver> PAMResolverMock = new ();

    protected BaseOrchestratorTest(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(new XunitLoggerProvider(output));
        });
        
        Logger = loggerFactory.CreateLogger<BaseOrchestratorTest>();
        
        PAMResolverMock.Setup(p => p.Resolve("ServerUsername")).Returns("foo");
        PAMResolverMock.Setup(p => p.Resolve("ServerPassword")).Returns("bar");
    }
    
    protected void MockClusterServerReturns(string serverName, string serverUuid)
    {
        ArubaClientMock.Setup(p => p.GetClusterServers())
            .ReturnsAsync(new List<ClusterServerItem>()
            {
                new()
                {
                    Name = serverName,
                    ServerUuid = serverUuid
                }
            });
    }
}
