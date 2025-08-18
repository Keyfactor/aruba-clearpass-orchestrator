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

namespace ArubaClearPassOrchestrator.Models.Keyfactor;

public class CertificateSubjectInformation
{
    /// <summary>
    /// The CN field of the Certificate Subject Information
    /// </summary>
    public string CommonName { get; set; }
    
    /// <summary>
    /// The O field of the Certificate Subject Information
    /// </summary>
    public string Organization { get; set; }
    
    /// <summary>
    /// The OU field of the Certificate Subject Information
    /// </summary>
    public string OrganizationalUnit { get; set; }
    
    /// <summary>
    /// The L field of the Certificate Subject Information
    /// </summary>
    public string CityLocality { get; set; }
    
    /// <summary>
    /// The ST field of the Certificate Subject Information
    /// </summary>
    public string StateProvince { get; set; }
    
    /// <summary>
    /// The C field of the Certificate Subject Information
    /// </summary>
    public string CountryRegion { get; set; }
    
    /// <summary>
    /// The E field of the Certificate Subject Information
    /// </summary>
    public string Email { get; set; }

    public static CertificateSubjectInformation ParseFromSubjectText(string subjectText)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parts = subjectText.Split(',');

        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2)
            {
                var key = kv[0].Trim();
                var value = kv[1].Trim();
                dictionary[key] = value;
            }
        }

        // Try to get values from the dictionary. We do not need to worry if the key is not part of the dictionary.
        dictionary.TryGetValue("CN", out var commonName);
        dictionary.TryGetValue("L", out var cityLocality);
        dictionary.TryGetValue("C", out var countryRegion);
        dictionary.TryGetValue("E", out var email);
        dictionary.TryGetValue("O", out var organization);
        dictionary.TryGetValue("OU", out var organizationalUnit);
        dictionary.TryGetValue("ST", out var stateProvince);

        var result = new CertificateSubjectInformation()
        {
            CommonName = commonName,
            CityLocality = cityLocality,
            CountryRegion = countryRegion,
            Email = email,
            Organization = organization,
            OrganizationalUnit = organizationalUnit,
            StateProvince = stateProvince
        };
        
        return result;
    }
}
