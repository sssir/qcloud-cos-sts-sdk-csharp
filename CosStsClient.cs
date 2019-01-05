using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TencentCloud
{
    public class CosStsClient
    {
        private const string StsDomain = "sts.api.qcloud.com";
        private const string StsUrl = "https://{host}/v2/index.php";

        public static async Task<string> GetCredentialAsync(JObject options)
        {
            string secretId = options["secretId"].ToString();
            string secretKey = options["secretKey"].ToString();
            string proxy = string.IsNullOrEmpty(options["proxy"]?.ToString()) ? null : options["proxy"].ToString();
            string host = options["host"]?.ToString();
            int durationSeconds = int.Parse((options["durationSeconds"] ?? options["durationInSeconds"] ?? 1800).ToString());
            JObject policy = options.ContainsKey("policy") ? (JObject)options["policy"] : GetPolicy(options);
            string policyStr = policy.ToString(Formatting.None);
            string action = "GetFederationToken";
            Random rdm = new Random();
            int nonce = rdm.Next(10000, 20000);
            long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string method = "POST";
            JObject parameters =
                new JObject(
                    new JProperty("Region", ""),
                    new JProperty("SecretId", secretId),
                    new JProperty("Timestamp", timestamp),
                    new JProperty("Nonce", nonce),
                    new JProperty("Action", action),
                    new JProperty("durationSeconds", durationSeconds),
                    new JProperty("name", "cos"),
                    new JProperty("policy", policyStr)
                );
            parameters["Signature"] = GetSignature(parameters, secretKey, method);
            HttpClient client = new HttpClient(
                new HttpClientHandler() { Proxy = new WebProxy(proxy, false), UseProxy = proxy != null });
            HttpResponseMessage result = await client.PostAsync(
                StsUrl.Replace("{host}", host ?? "sts.api.qcloud.com"),
                new StringContent(Json2String(parameters, true), Encoding.UTF8, "application/x-www-form-urlencoded"));
            return await result.Content.ReadAsStringAsync();
        }

        private static string GetSignature(JObject opt, string key, string method)
        {
            string formatString = method + StsDomain + "/v2/index.php?" + Json2String(opt);
            HMACSHA1 myhmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(key));
            var sign = myhmacsha1.ComputeHash(new MemoryStream(Encoding.UTF8.GetBytes(formatString)));
            return Convert.ToBase64String(sign);
        }

        private static string Json2String(JObject obj, bool isEncode = false)
        {
            List<string> arr = new List<string>();
            string[] properties = obj.Properties().Select(p => p.Name).ToArray();
            Array.Sort(properties, StringComparer.Ordinal);
            properties.ToList().ForEach(item =>
            {
                string val = obj[item]?.ToString() ?? string.Empty;
                arr.Add(item + "=" + (isEncode ? Uri.EscapeDataString(val) : val));
            });
            return string.Join('&', arr);
        }

        private static JObject GetPolicy(JObject config)
        {
            string bucket = config["bucket"].ToString();
            string region = config["region"].ToString();
            string allowPrefix = config["allowPrefix"].ToString();
            string[] allowActions = (string[])config["allowActions"].ToObject(typeof(string[]));
            string shortBucketName = bucket.Split('-').First();
            string appId = bucket.Split('-').Last();
            JObject policy = new JObject(
                new JProperty("version", "2.0"),
                new JProperty("statement", new JArray(
                    new JObject(
                        new JProperty("effect", "allow"),
                        new JProperty("principal", new JObject(new JProperty("qcs", "*"))),
                        new JProperty("action", new JArray(allowActions)),
                        new JProperty("resource", $"qcs::cos:{region}:uid/{appId}:prefix//{appId}/{shortBucketName}/{allowPrefix}")
                        )
                    ))
                );
            return policy;
        }
    }
}
