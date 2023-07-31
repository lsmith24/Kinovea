using Kinovea.Root.AwsS3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace Kinovea.Root.AwsS3.Services
{
    public class StorageService : IStorageService
    {
        public async Task<S3ResponseDto> UploadFileAsync(S3Object s3obj, AwsCredentials awsCreds)
        {
            // Add AWS Credentials
            var credentials = new BasicAWSCredentials(awsCreds.AwsKey, awsCreds.AwsSecretKey);

            // Specify region of bucket
            var config = new AmazonS3Config()
            {
                RegionEndpoint = Amazon.RegionEndpoint.USEast2
            };

            // create PutObjectRequest from s3Obj data
            var request = new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = s3obj.BucketName,
                Key = s3obj.Name,
                FilePath = s3obj.FilePath
            };

            var response = new S3ResponseDto();

            try
            {
                // Create S3 Client
                var client = new AmazonS3Client(credentials, config);

                // Upload utility to s3
                //var transferUtility = new TransferUtility(client);

                // Upload file to s3
                await client.PutObjectAsync(request);

                // assuming it works
                response.StatusCode = 200;
                response.Message = $"{s3obj.Name} uploaded successfully";
            }
            catch (AmazonS3Exception ex)
            {
                response.StatusCode = (int)ex.StatusCode;
                response.Message = ex.Message;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Message = ex.Message;
            }

            return response;
        }
    }
}
