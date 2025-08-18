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
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace ArubaClearPassOrchestrator.Clients;

public class FileServerClientFactory : IFileServerClientFactory
{
    public IFileServerClient CreateFileServerClient(ILogger logger, string type, string fileServerHost, string fileServerUsername,
        string fileServerPassword)
    {
        logger.MethodEntry();
        IFileServerClient fileServerClient = null;

        try
        {
            switch (type)
            {
                case "Amazon S3":
                    fileServerClient =
                        new S3FileServerClient(logger, fileServerHost, fileServerUsername, fileServerPassword);
                    break;
                default:
                    logger.LogWarning(
                        $"No server type mapping found for '{type}'. Returning a null file server client.");
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"An error occurred getting the file server client: {ex.Message}");
        }
        finally
        {
            logger.MethodExit();
        }

        return fileServerClient;
    }
}
