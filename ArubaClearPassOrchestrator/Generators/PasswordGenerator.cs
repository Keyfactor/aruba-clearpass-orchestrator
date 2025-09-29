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

using System.Text;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Security;

namespace Keyfactor.Extensions.Orchestrator.ArubaClearPassOrchestrator.Generators;

public static class PasswordGenerator
{
    /// <summary>
    /// Generates a secure password of the specified length using a cryptographically secure random number generator.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="length">The length of the password to generate.</param>
    /// <returns>A securely generated password of specified length.</returns>
    public static string Generate(ILogger logger, int length)
    {
        logger.MethodEntry();
        logger.LogDebug($"Generating a secure password with {length} characters");
        const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_";
        var random = new SecureRandom();
        var password = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(allowedChars.Length);
            password.Append(allowedChars[index]);
        }

        logger.MethodExit();
        return password.ToString();
    }
}
