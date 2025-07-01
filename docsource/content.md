## Overview

The Aruba ClearPass Orchestrator Extension is an integration that can inventory and re-enroll certificates on a server in [Aruba ClearPass](https://www.hpe.com/us/en/aruba-clearpass-policy-manager.html).  The certificate store types that can be managed in the current version are:

* Aruba

## File Store Configuration

The Aruba ClearPass API requires an HTTP-accessible URL for certificates when performing a re-enrollment. The URL must be accessible by Aruba's servers. There are a number of File Store types supported by this integration.

### Amazon S3

If you wish to use Amazon S3 as your file store, we recommend you review the [Amazon S3 Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html) documentation. For this file store, you will need to create an S3 bucket. **It is recommended your bucket has public access disabled**. You will need an IAM role that has access to the S3 bucket, can determine the region the S3 bucket is located in, can upload the certificate contents to S3, and will need to be able to generate a pre-signed URL for the uploaded certificate file.

These are the File Server configurations on the Certificate Store setup:
- File Server Type
    - This value will need to be **Amazon S3**.
- File Server Host
    - This will be the **S3 bucket name**. S3 bucket names are globally unique identifiers.
- File Server Username
    - Optional. If you wish to use IAM user credentials, this will be the **Access Key** for the IAM user credentials.
    - If not provided, the orchestrator can resolve credentials. See the AWS [Credential and profile resolution](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html) for more information.
- File Server Password
    - Optional. If you wish to use IAM user credentials, this will be the **Secret Access Key** for the IAM user credentials.
    - If not provided, the orchestrator can resolve credentials. See the AWS [Credential and profile resolution](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html) for more information.


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
