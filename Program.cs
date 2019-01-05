using System;
using Newtonsoft.Json.Linq;
using TencentCloud;

namespace qcloud_cos_sts_sdk
{
    class Program
    {
        static void Main(string[] args)
        {
            JObject opt = JObject.Parse(@"{
                secretId: 'AKIDIdmhPtFD2e2DBv9mtmSu9AxWryrZnJK6',
                secretKey: 'f6HCrGPIwLXWHp9F7k9MM8h293kDSTHq',
                proxy: '',
                durationSeconds: 1800,

                // 放行判断相关参数
                bucket: '0-1252076768',
                region: 'ap-shanghaiuid',
                allowPrefix: '',
                // 简单上传和分片，需要以下的权限，其他权限列表请看 https://cloud.tencent.com/document/product/436/31923
                allowActions: [
                    // 简单上传
                    'name/cos:PutObject',
                    // 分片上传
                    'name/cos:InitiateMultipartUpload',
                    'name/cos:ListMultipartUploads',
                    'name/cos:ListParts',
                    'name/cos:UploadPart',
                    'name/cos:CompleteMultipartUpload'
                ],
            }");

            string result = CosStsClient.GetCredentialAsync(opt).Result;
            Console.WriteLine(result);
        }
    }
}
