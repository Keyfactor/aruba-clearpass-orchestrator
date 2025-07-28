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

using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Models.Aruba.CertSignRequest;

public class CreateCertificateSignRequestRequest
{
    [JsonProperty("subject_CN")]
    public string SubjectCN { get; set; }
    
    [JsonProperty("subject_O")]
    public string SubjectO { get; set; }
    
    [JsonProperty("subject_OU")]
    public string SubjectOU { get; set; }
    
    [JsonProperty("subject_L")]
    public string SubjectL { get; set; }
    
    [JsonProperty("subject_ST")]
    public string SubjectST { get; set; }
    
    [JsonProperty("subject_C")]
    public string SubjectC { get; set; }
    
    [JsonProperty("private_key_password")]
    public string PrivateKeyPassword { get; set; }
    
    [JsonProperty("private_key_type")]
    public string PrivateKeyType { get; set; }
    
    [JsonProperty("digest_algorithm")]
    public string DigestAlgorithm { get; set; }
}
