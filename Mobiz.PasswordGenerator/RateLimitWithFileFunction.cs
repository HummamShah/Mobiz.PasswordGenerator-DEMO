using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mobiz.PasswordGenerator
{
    public static class RateLimitWithFileFunction
    {
        private static readonly int RequestLimit = int.Parse(Environment.GetEnvironmentVariable("REQUEST_LIMIT_PER_MINUTE") ?? "60");
        private static readonly string DataDirectory = Environment.GetEnvironmentVariable("RateLimitDataDirectory") ?? "Data"; // Path to the directory where rate limit data will be stored

        [FunctionName("RateLimitWithFile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request for rate limiting.");

            string userId = req.Query["userId"];
            if (string.IsNullOrEmpty(userId))
            {
                return new BadRequestObjectResult("Please provide a valid 'userId' in the query string.");
            }

            // Check rate limit
            if (!CheckRateLimit(userId))
            {
                // Rate limit exceeded
                return new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
            }

            // Increment request count
            IncrementRequestCount(userId);

            return new OkObjectResult("Request accepted.");
        }

        private static bool CheckRateLimit(string userId)
        {
            try
            {
                string filePath = GetFilePath(userId);
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                if (!File.Exists(filePath))
                {
                    
                    // File does not exist, create it
                    //File.WriteAllText(filePath, JsonConvert.SerializeObject(new RateLimitData { RequestCount = 1, LastUpdated = DateTime.UtcNow }));
                    return true;
                }

                // Read rate limit data from file
                string json = File.ReadAllText(filePath);
                RateLimitData rateLimitData = JsonConvert.DeserializeObject<RateLimitData>(json);

                // Check if rate limit exceeded
                if (rateLimitData.RequestCount >= RequestLimit && rateLimitData.LastUpdated.Date == DateTime.UtcNow.Date)
                {
                    if (rateLimitData.LastUpdated.AddMinutes(1) <=  DateTime.UtcNow)
                    {
                        File.Delete(filePath);
                        return true;
                    }
                    return false; // Rate limit exceeded
                }
            }
            catch(Exception e)
            {

            }
            

            return true; // Rate limit not exceeded
        }

        private static void IncrementRequestCount(string userId)
        {
            string filePath = GetFilePath(userId);
            if (!File.Exists(filePath))
            {
                // File does not exist, create it
                File.WriteAllText(filePath, JsonConvert.SerializeObject(new RateLimitData { RequestCount = 1, LastUpdated = DateTime.UtcNow }));
            }
            else
            {
                // Read rate limit data from file
                string json = File.ReadAllText(filePath);
                RateLimitData rateLimitData = JsonConvert.DeserializeObject<RateLimitData>(json);

                // Increment request count and update last updated time
                rateLimitData.RequestCount++;
                rateLimitData.LastUpdated = DateTime.UtcNow;

                // Write updated data back to file
                File.WriteAllText(filePath, JsonConvert.SerializeObject(rateLimitData));
            }
        }

        private static string GetFilePath(string userId)
        {
            string fileName = $"{userId}.json";
            return Path.Combine(DataDirectory, fileName);
        }

        private class RateLimitData
        {
            public int RequestCount { get; set; }
            public DateTime LastUpdated { get; set; }
        }
    }
}





