using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon;
using Amazon.Runtime;
using System;
using System.Configuration;
using Microsoft.Extensions.Options;

namespace _301271988_chauhanpachchigar__Lab3
{
    public class Helper
    {
        private readonly AWSOptions _awsOptions;

        public Helper(IOptions<AWSOptions> awsOptions)
        {
            _awsOptions = awsOptions.Value;
        }

        public AmazonDynamoDBClient InitializeDynamoDBClient()
        {
            if (string.IsNullOrEmpty(_awsOptions.AccessKey) || string.IsNullOrEmpty(_awsOptions.SecretKey))
            {
                throw new Exception("AWS Access Key or Secret Key is missing in the configuration.");
            }

            var awsCredentials = new BasicAWSCredentials(_awsOptions.AccessKey, _awsOptions.SecretKey);
            return new AmazonDynamoDBClient(awsCredentials, RegionEndpoint.GetBySystemName(_awsOptions.Region));
        }

        public AmazonS3Client InitializeS3Client()
        {
            if (string.IsNullOrEmpty(_awsOptions.AccessKey) || string.IsNullOrEmpty(_awsOptions.SecretKey))
            {
                throw new Exception("AWS Access Key or Secret Key is missing in the configuration.");
            }

            var awsCredentials = new BasicAWSCredentials(_awsOptions.AccessKey, _awsOptions.SecretKey);
            return new AmazonS3Client(awsCredentials, RegionEndpoint.GetBySystemName(_awsOptions.Region));
        }
    }
}