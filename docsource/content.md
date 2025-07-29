## Overview

The Aruba ClearPass Orchestrator Extension is an integration that can inventory and re-enroll certificates on a server in [Aruba ClearPass](https://www.hpe.com/us/en/aruba-clearpass-policy-manager.html).  The certificate store types that can be managed in the current version are:

* Aruba

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
    - If not provided, the orchestrator can resolve credentials. See the AWS [Credential and profile resolution](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html) for more information.
- File Server Password
    - Optional. If you wish to use IAM user credentials, this will be the **Secret Access Key** for the IAM user credentials.
    - If not provided, the orchestrator can resolve credentials. See the AWS [Credential and profile resolution](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html) for more information.


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

## Unit + Integration Tests

This project features unit and integration tests that can be run from any IDE or command line.

### Setting Up Integration Tests

Inside the `ArubaClearPassOrchestrator.IntegrationTests` directory, there is a [.env.test.example](./ArubaClearPassOrchestrator.IntegrationTests/.env.test.example) file with the environment variables you can fill out. Each integration test has a flag that you can toggle to skip running that test. Copy the `.env.test.example` to `.env.test` within the same directory and fill out the environment variable values.

### Running the Tests

Here are some command line scripts to run the test suites:

```bash
# restore project dependencies (optional)
dotnet restore

# run integration and unit tests
dotnet test

# run just the unit tests
dotnet test --filter "Category!=Integration"

# run just the integration tests
dotnet test --filter "Category=Integration"
```

