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
using Xunit.Abstractions;

namespace ArubaClearPassOrchestrator.IntegrationTests.Clients.FileServerClients;

[Trait("Category", "Integration")]
public abstract class BaseFileServerClientTest<T> : BaseIntegrationTest where T : IFileServerClient
{
    protected readonly T Client;

    public BaseFileServerClientTest(ITestOutputHelper output, string type, string hostname, string username,
        string password) : base(output)
    {
        var fileServerClientFactory = new FileServerClientFactory();
        var client = fileServerClientFactory.CreateFileServerClient(Logger, type, hostname, username, password);

        if (client == null)
        {
            throw new NotImplementedException(
                $"File server type {type} was not implemented in the file server factory");
        }

        Assert.IsAssignableFrom<T>(client);

        Client = (T)client;
    }

    
}
