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
    public string? Organization { get; set; }
    
    /// <summary>
    /// The OU field of the Certificate Subject Information
    /// </summary>
    public string? OrganizationalUnit { get; set; }
    
    /// <summary>
    /// The L field of the Certificate Subject Information
    /// </summary>
    public string? CityLocality { get; set; }
    
    /// <summary>
    /// The ST field of the Certificate Subject Information
    /// </summary>
    public string? StateProvince { get; set; }
    
    /// <summary>
    /// The C field of the Certificate Subject Information
    /// </summary>
    public string? CountryRegion { get; set; }
    
    /// <summary>
    /// The E field of the Certificate Subject Information
    /// </summary>
    public string? Email { get; set; }
    
    public string SubjectText { get; set; }
    
    /// <summary>
    /// Dictionary containing all key-value pairs found in the subject text
    /// </summary>
    public Dictionary<string, string> AllFields { get; set; }

    public static CertificateSubjectInformation ParseFromSubjectText(string subjectText)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Parse by finding key positions rather than splitting by comma
        var parts = ParseSubjectPartsByKeys(subjectText);
        
        foreach (var part in parts)
        {
            var equalIndex = part.IndexOf('=');
            if (equalIndex > 0 && equalIndex < part.Length - 1)
            {
                var key = part.Substring(0, equalIndex).Trim();
                var value = part.Substring(equalIndex + 1).Trim();
                dictionary[key] = value;
            }
        }

        // Try to get values from the dictionary
        dictionary.TryGetValue("CN", out var commonName);
        dictionary.TryGetValue("L", out var cityLocality);
        dictionary.TryGetValue("C", out var countryRegion);
        dictionary.TryGetValue("E", out var email);
        dictionary.TryGetValue("O", out var organization);
        dictionary.TryGetValue("OU", out var organizationalUnit);
        dictionary.TryGetValue("ST", out var stateProvince);

        var result = new CertificateSubjectInformation()
        {
            SubjectText = subjectText,
            CommonName = commonName,
            CityLocality = cityLocality,
            CountryRegion = countryRegion,
            Email = email,
            Organization = organization,
            OrganizationalUnit = organizationalUnit,
            StateProvince = stateProvince,
            AllFields = new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase)
        };
        
        return result;
    }
    
    private static List<string> ParseSubjectPartsByKeys(string subjectText)
    {
        var parts = new List<string>();
        var keyPositions = new List<int>();
        
        // Find all positions where any key pattern appears (letters followed by '=')
        for (int i = 0; i < subjectText.Length - 1; i++)
        {
            // Look for pattern: [letter(s)][=]
            if (char.IsLetter(subjectText[i]))
            {
                var keyStart = i;
                
                // Find the end of the key (continue while we have letters or digits)
                while (i < subjectText.Length && (char.IsLetter(subjectText[i]) || char.IsDigit(subjectText[i])))
                {
                    i++;
                }
                
                // Check if this is followed by '=' and is at a valid position
                if (i < subjectText.Length && subjectText[i] == '=' && IsValidKeyPosition(subjectText, keyStart))
                {
                    keyPositions.Add(keyStart);
                }
            }
        }
        
        // Sort positions and extract parts
        keyPositions.Sort();
        
        for (int i = 0; i < keyPositions.Count; i++)
        {
            var startPos = keyPositions[i];
            var endPos = i < keyPositions.Count - 1 ? keyPositions[i + 1] : subjectText.Length;
            
            var part = subjectText.Substring(startPos, endPos - startPos).Trim();
            
            // Remove trailing comma if present
            if (part.EndsWith(","))
            {
                part = part.Substring(0, part.Length - 1).Trim();
            }
            
            if (!string.IsNullOrEmpty(part))
            {
                parts.Add(part);
            }
        }
        
        return parts;
    }
    
    private static bool IsValidKeyPosition(string subjectText, int keyIndex)
    {
        // Check if the character before the key is a valid delimiter (comma, space, or start of string)
        if (keyIndex == 0)
            return true;
            
        var prevChar = subjectText[keyIndex - 1];
        return prevChar == ',' || char.IsWhiteSpace(prevChar);
    }
}
