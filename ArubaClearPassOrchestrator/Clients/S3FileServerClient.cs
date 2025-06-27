using System.Security.Cryptography.X509Certificates;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ArubaClearPassOrchestrator.Clients.Interfaces;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ArubaClearPassOrchestrator.Clients;

public class S3FileServerClient : BaseFileServerClient, IFileServerClient
{
    private readonly AWSCredentials? _credentials;
    private readonly string _bucketName;
    private readonly ILogger _logger;
    private const int PresignedUrlExpiryMinutes = 30;
    
    public S3FileServerClient(ILogger logger, string bucketName, string? accessKey, string? secretAccessKey)
    {
        _logger = logger;
        
        _logger.LogDebug($"Creating an S3 file server client for bucket URL {bucketName}");

        if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretAccessKey))
        {
            _logger.LogDebug("Basic AWS credentials with access key and secret access key being used");
            _credentials = new BasicAWSCredentials(accessKey, secretAccessKey);
        }
        else
        {
            _logger.LogDebug("Using default AWS credentials");
            _credentials = null; // Let the below code handle resolving the default credentials
        }

        _bucketName = bucketName;
    }
    
    /// <summary>
    /// Uploads the certificate contents to S3 and returns a pre-signed URL to the certificate.
    /// </summary>
    /// <param name="key">The path to store the certificate</param>
    /// <param name="certificate">The certificate to store into S3</param>
    /// <returns>A pre-signed URL to access the certificate.</returns>
    public async Task<string> UploadCertificate(string key, X509Certificate2 certificate)
    {
        string? certificateUrl = null;
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
            
            _logger.LogDebug($"Uploading the certificate to S3. Bucket name: {_bucketName}, Key: {key}");

            await client.PutObjectAsync(new PutObjectRequest()
            {
                BucketName = _bucketName,
                Key = key,
                ContentType = "application/x-pem-file",
                InputStream = new MemoryStream(data)
            });
            
            _logger.LogInformation($"Successfully uploaded the certificate to S3. Bucket name: {_bucketName}, Key: {key}");
            
            var expiryDate = DateTime.UtcNow.AddMinutes(PresignedUrlExpiryMinutes);
            
            _logger.LogDebug($"Creating a pre-signed URL for certificate. Expiration (UTC): {expiryDate:o}");

            certificateUrl = await client.GetPreSignedURLAsync(new GetPreSignedUrlRequest()
            {
                BucketName = _bucketName,
                Key = key,
                Expires = expiryDate,
                Verb = HttpVerb.GET,
            });
            
            _logger.LogInformation($"Pre-signed URL for certificate created. Expiration (UTC): {expiryDate:o}");
            
            _logger.MethodExit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred uploading certificate contents to S3. Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
            throw;
        }
        
        return certificateUrl;
    }

    /// <summary>
    /// Although S3 buckets are globally unique, the S3 client needs to point the writer
    /// to the appropriate region. To get the region, we can leverage the S3Client SDK
    /// to get the region from the bucket name.
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
    private IAmazonS3 GetS3Client(RegionEndpoint? region = null)
    {
        var regionString = region == null ? "(not provided)" : region.DisplayName;
        _logger.LogDebug($"Getting S3 client for region: {regionString}");
        
        // To lookup an S3 bucket location, if no region is specified provide us-east-1 as a default for the lookup
        var config = new AmazonS3Config
        {
            RegionEndpoint = region ?? RegionEndpoint.USEast1 // Any default, just for the call to succeed
        };
        
        if (_credentials != null)
        {
            _logger.LogDebug("Using basic AWS credentials for S3 client");
            
            return new AmazonS3Client(_credentials, config);
        }

        _logger.LogDebug("Using default AWS credentials for S3 client");
        
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
        
        _logger.LogDebug($"Determining region based on response");
        
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
