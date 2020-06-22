using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace PlayTogetherApi.Services
{
    public class S3Service
    {
        readonly BasicAWSCredentials AwsCreds;
        readonly AmazonS3Config S3Config;
        readonly string BucketName;
        public AmazonS3Client S3Client => _s3Client = _s3Client ?? new AmazonS3Client(AwsCreds, S3Config);
        private AmazonS3Client _s3Client = null;

        public S3Service(IConfiguration conf)
        {
            string AccessKeyId = conf.GetSection("AWS_PUBLIC_ACCESS").Value;
            string SecretAccessKey = conf.GetSection("AWS_SECRET_ACCESS").Value;
            BucketName = conf.GetSection("AWS_BUCKET").Value ?? "playtogether";

            AwsCreds = new BasicAWSCredentials(AccessKeyId, SecretAccessKey);

            S3Config = new AmazonS3Config
            {
                ServiceURL = "https://folder.s3.amazonaws.com/",
                RegionEndpoint = RegionEndpoint.EUWest1
            };
        }

        public async Task<bool> FileExistsAsync(string filename)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = filename
            };
            try
            {
                using (var getResponse = await S3Client.GetObjectAsync(getRequest))
                {
                    return true;
                }
            }
            catch (AmazonServiceException e)
            {
                // could consider checking e.ErrorCode == "NoSuchKey";
                if (e.ErrorCode == "NoSuchKey")
                {
                    return false;
                }
                throw;
            }
        }

        public async Task UploadFileAsync(Stream stream, string targetFileName)
        {
            var putRequest1 = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = targetFileName,
                InputStream = stream,
                CannedACL = S3CannedACL.PublicRead
            };
            var putResponse = await S3Client.PutObjectAsync(putRequest1);
            // todo: check putResponse
        }

        public async Task DeleteFileAsync(string filename)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = filename
            };
            await S3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}
