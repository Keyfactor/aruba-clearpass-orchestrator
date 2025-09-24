## Overview

The Aruba ClearPass Orchestrator Extension is an integration that can inventory and re-enroll certificates on a server in [Aruba ClearPass](https://www.hpe.com/us/en/aruba-clearpass-policy-manager.html).  The certificate store types that can be managed in the current version are:

* Aruba

## Prerequisites
The following setup is required for this integration:
- An Aruba API client with appropriate operator profile permissions (see [Aruba API Client Setup](#aruba-api-client-setup))
- A file server that can serve uploaded certificates via HTTPS (see [File Server Configuration](#file-server-configuration) for a list of supported file server types)

### Aruba API Client Setup
> Please refer to the [Aruba Networks documentation](https://developer.arubanetworks.com/cppm/docs/api-authorization-oauth2) for the official API client setup guide.

- If you do not already have an API client available, here are the steps to create an API client. Within Aruba ClearPass Guest, go to Administration > API Services > API Clients and select `Create API client`.
- For Operating Mode, choose `ClearPass REST API`
- For Operator Profile, choose the profile you wish to use with the API client. Make sure the Operator Profile meets the [minimum required permissions](#operator-profile-permissions-requirements)
- For the Grant Type, choose `Client credentials`
- Make sure the client is Enabled

Copy the client secret in a secure location. The client ID and client secret will be used in your certificate store configuration.

#### Operator Profile Permissions Requirements

The following permissions are required on your API client's operator profile:
- API Services > Allow API Access > Allow Access
- Platform > Import Configuration > Read Only
- Policy Manager > Certificates > Read, Write

## Store Path Configuration

Aruba manages a single certificate per server + service. Both values are required to update a server certificate. The format of the store path should be `<server-name>;<service-name>`. For example, if you are updating the `clearpass.localhost` server certificate for service `HTTPS (RSA)`, the store path format will be `clearpass.localhost;HTTPS(RSA)`. 

As of writing, acceptable values for `service-name` are as follows:

- RADIUS
- HTTPS(RSA)
- HTTPS(ECC)
- RadSec

To build in flexibility for more Aruba-supported values, the orchestrator **will not perform** validation on the provided `service-name`, but you may run into issues if the service name does not exactly match the values above.

## File Server Configuration

The Aruba ClearPass API requires an HTTP-accessible URL for certificates when performing a re-enrollment. The URL **must** be accessible by the Aruba ClearPass server. Currently, the `FileServerType` types accepted are:

- [Amazon S3](#amazon-s3)

Please see each related section for information on how to configure your certificate store type with the associated file server type.

### Amazon S3

The `Amazon S3` File Server Type supports operations directly to AWS S3 and to S3-compatible services (i.e. Cloudian Hyperstore, MinIO, etc.). If configured to talk to AWS S3 services, we recommend you review the [Amazon S3 Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html) documentation. Otherwise, please consult your AWS-compatible service's documentation for best practices and access policies.

For this file store type, certificate contents will be uploaded to a bucket, and a temporary pre-signed URL will be generated for Aruba ClearPass to access the object via HTTPS.  You will need to configure your provider's security roles that have access to the S3 bucket, can determine the region the S3 bucket is located in, can upload the certificate contents to the bucket, and will need to be able to generate a pre-signed URL for the uploaded certificate file (see [an example IAM policy](#example-aws-iam-policy) if you are targeting AWS).

These are the File Server configurations on the Certificate Store setup:
- File Server Type
    - This value will need to be **Amazon S3**.
- File Server Host
    - If targeting AWS S3, this will be the **S3 bucket name**. S3 bucket names are globally unique identifiers.
    - If targeting an S3-compatible service (i.e. Cloudian Hyperstore, MinIO, etc.), the host will be in the format `<service-url>;<bucket-name>`. For example, `https://s3-us-west1.cloudian.example.com:443;your-bucket-name`.
- File Server Username
    - Optional. If you wish to use IAM user credentials, this will be the **Access Key** for the IAM user credentials.
    - If not provided, the orchestrator can attempt to resolve credentials. See the AWS [Credential and profile resolution](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html) for more information.
- File Server Password
    - Optional. If you wish to use IAM user credentials, this will be the **Secret Access Key** for the IAM user credentials.
    - If not provided, the orchestrator can attempt to resolve credentials. See the AWS [Credential and profile resolution](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html) for more information.


#### Example AWS IAM Policy
Here is an example IAM policy with the minimum permissions necessary:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:GetBucketLocation"
      ],
      "Resource": [
        "arn:aws:s3:::your-bucket-name",
        "arn:aws:s3:::your-bucket-name/*"
      ]
    }
  ]
}
```

## Contributing

This project welcomes any contributions. Please see the [CONTRIBUTING](./CONTRIBUTING.md) document for a development guide.