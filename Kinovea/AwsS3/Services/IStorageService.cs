using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kinovea.Root.AwsS3.Models;

namespace Kinovea.Root.AwsS3
{
    public interface IStorageService
    {
        Task<Models.S3ResponseDto> UploadFileAsync(S3Object s3obj, AwsCredentials awsCreds);
    }
}
