using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinovea.Root.AwsS3.Models
{
    public class S3Object
    {
        public string Name { get; set; } = null;
        public string FilePath { get; set; } = null;

        public string BucketName { get; set; } = null;
    }
}
