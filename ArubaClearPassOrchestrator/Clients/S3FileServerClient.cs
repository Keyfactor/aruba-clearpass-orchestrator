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

using System.Security.Cryptography.X509Certificates;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Clients;

public class S3FileServerClient : BaseFileServerClient, IFileServerClient
{
    private readonly AWSCredentials _credentials;
    private readonly string _serviceUrl; // Service Endpoint for S3-compatible API services (Cloudian, MinIO, etc.)
    private readonly string _bucketName;
    private readonly ILogger _logger;
    
    private const int PresignedUrlExpiryMinutes = 30;
    
    public S3FileServerClient(ILogger logger, string bucketName, string accessKey, string secretAccessKey)
    {
        _logger = logger;
        _logger.MethodEntry();
        
        _logger.LogDebug($"Creating an S3 file server client for bucket URL {bucketName}");

        if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretAccessKey))
        {
            _logger.LogDebug("Using basic AWS credentials with access key and secret access key");
            _credentials = new BasicAWSCredentials(accessKey, secretAccessKey);
        }
        else
        {
            _logger.LogDebug("Using default AWS credentials");
            _credentials = null; // Let the below code handle resolving the default credentials
        }

        if (!string.IsNullOrWhiteSpace(bucketName) && bucketName.Contains(';'))
        {
            _logger.LogDebug("Splitting service URL from bucket name");
            var split = bucketName.Split(";");

            _serviceUrl = split[0];
            _bucketName = split[1];
        }
        else
        {
            _logger.LogDebug($"Using supplied bucket name value");
            _bucketName = bucketName;
        }

        _logger.LogDebug($"Service URL: {_serviceUrl ?? "(not provided)"}, Bucket Name: {_bucketName}");
        
        _logger.MethodExit();
    }
    
    /// <summary>
    /// Uploads the certificate contents to S3 and returns a pre-signed URL to the certificate.
    /// </summary>
    /// <param name="fileName">The path to store the certificate</param>
    /// <param name="certificate">The certificate to store into S3</param>
    /// <returns>A pre-signed URL to access the certificate.</returns>
    public async Task<string> UploadCertificate(string fileName, X509Certificate2 certificate)
    {
        string certificateUrl = null;
        try
        {
            _logger.MethodEntry();
            var region = await GetRegionEndpointOfBucket(_bucketName);

            // Need to recreate the S3 client to point at the S3 bucket region
            var client = GetS3Client(region);

            _logger.LogDebug("Converting certificate contents to string");

            // _logger.LogDebug($"Certificate: {JsonConvert.SerializeObject(certificate)}");
            // _logger.LogDebug($"Raw Data: {certificate.RawData}, GetRawCertData: {certificate.GetRawCertData()}, GetRawCertDataString: {certificate.GetRawCertDataString()}");
            string pem = ConvertToPem(certificate);
            byte[] data = Encoding.UTF8.GetBytes(pem);
            
            _logger.LogTrace($"PEM contents: {pem}");

            var key = $"{fileName}.pem";

            _logger.LogDebug($"Uploading the certificate to S3. Bucket name: {_bucketName}, Key: {key}");

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentType = "application/x-pem-file",
                InputStream = new MemoryStream(data),
            };

            await client.PutObjectAsync(request);

            _logger.LogInformation(
                $"Successfully uploaded the certificate to S3. Bucket name: {_bucketName}, Key: {key}");

            var expiryDate = DateTime.UtcNow.AddMinutes(PresignedUrlExpiryMinutes);

            _logger.LogDebug($"Creating a pre-signed URL for certificate. Expiration (UTC): {expiryDate:o}");

            certificateUrl = await client.GetPreSignedURLAsync(new GetPreSignedUrlRequest()
            {
                BucketName = _bucketName,
                Key = key,
                Expires = expiryDate,
                Verb = HttpVerb.GET,
            });
            
            _logger.LogDebug($"Certificate pre-signed URL: {certificateUrl}");
            _logger.LogInformation($"Pre-signed URL for certificate created. Expiration (UTC): {expiryDate:o}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"An error occurred uploading certificate contents to S3. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            _logger.MethodExit();
        }
        
        return certificateUrl;
    }

    /// <summary>
    /// Although S3 buckets are globally unique, the S3 client needs to point the writer
    /// to the appropriate region. To get the region, we can leverage the S3Client SDK
    /// to get the region from the bucket name.
    ///
    /// If using an S3-compatible service, the region endpoint should still be inferred
    /// by the bucket name, but it may be simply mocked (i.e. always "us-east-1" or "")
    /// </summary>
    /// <param name="bucketName">The S3 bucket name</param>
    /// <returns>The region endpoint associated with the S3 bucket.</returns>
    private async Task<RegionEndpoint> GetRegionEndpointOfBucket(string bucketName)
    {
        _logger.MethodEntry();
        
        _logger.LogDebug($"Creating an S3 client to look up region for bucket {bucketName}");
        var client = GetS3Client(null);
        _logger.LogDebug($"Determining region endpoint for S3 bucket {bucketName}");
        
        var response = await client.GetBucketLocationAsync(bucketName);
        var location = DetermineRegionEndpointOfBucket(response);
        
        _logger.LogDebug($"Got region of S3 bucket {bucketName}. Region: {location.DisplayName}");
        
        _logger.MethodExit();
        return location;
    }

    /// <summary>
    /// Creates an S3 client. If a region is provided, it will scope the S3 client to that region.
    /// It will either use basic AWS credentials or use default AWS credentials for authorization.
    /// </summary>
    /// <param name="region">An optional regional specification</param>
    /// <returns>An IAmazonS3 interface to talk to S3</returns>
    private IAmazonS3 GetS3Client(RegionEndpoint region = null)
    {
        _logger.MethodEntry();
        
        var regionString = region == null ? "(not provided)" : region.DisplayName;
        _logger.LogDebug($"Getting S3 client for region: {regionString}");
        
        var config = new AmazonS3Config
        {
            ServiceURL = _serviceUrl,
            ForcePathStyle = true,      // Make bucket name part of the URL path instead of hostname, required for S3-compatible services like Cloudian.
        };
        
        // S3-Compatible APIs should not have a RegionEndpoint supplied to the client, otherwise
        // the SDK will try to talk to S3 directly even if ServiceURL is explicitly set.
        //
        // To look up an S3 bucket location, if no region is specified provide us-east-1 as a default for the lookup
        if (config.ServiceURL == null)
        {
            config.RegionEndpoint = region ?? RegionEndpoint.USEast1;
            _logger.LogDebug($"S3 client configured to talk to Amazon S3 at region endpoint {config.RegionEndpoint.DisplayName}");
        }
        else
        {
            _logger.LogDebug($"S3 client configured to talk to S3-compatible API at service URL {config.ServiceURL}");
        }
        
        if (_credentials != null)
        {
            _logger.LogDebug("Using basic AWS credentials for S3 client");
            _logger.MethodExit();
            
            return new AmazonS3Client(_credentials, config);
        }

        _logger.LogDebug("Using default AWS credentials for S3 client");
        
        _logger.MethodExit();
        return new AmazonS3Client(config);
    }

    /// <summary>
    /// Determining the bucket region based on a GetBucketLocationResponse. AWS has some legacy behavior to account for.
    /// Please refer to documentation: https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLocation.html#API_GetBucketLocation_ResponseSyntax
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    private RegionEndpoint DetermineRegionEndpointOfBucket(GetBucketLocationResponse response)
    {
        _logger.MethodEntry();
        var location = response.Location;
        
        _logger.LogDebug($"Determining S3 bucket region based on response");
        
        // Per AWS spec: null means us-east-1
        if (location == null || string.IsNullOrEmpty(location.Value))
        {
            _logger.LogDebug($"Defaulting region to us-east-1");
            return RegionEndpoint.USEast1;
        }
            

        // "EU" is legacy name for eu-west-1
        if (location.Value == "EU")
        {
            _logger.LogDebug($"Defaulting region eu-west-1");
            return RegionEndpoint.EUWest1;
        }
        
        _logger.LogDebug($"Looking up region by system name: {location.Value}");
        
        var region = RegionEndpoint.GetBySystemName(location.Value);
        
        _logger.LogDebug($"Successfully determined region: {region.DisplayName}");
        
        _logger.MethodExit();
        return region;
    }
}
