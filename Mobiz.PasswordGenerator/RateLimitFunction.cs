using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PasswordGenerator;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using System.Net;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure;
using Azure.Storage.Queues.Models;

namespace Mobiz.PasswordGenerator
{
    public static class RateLimitFunction
    {

        private static int RequestLimit = int.Parse(Environment.GetEnvironmentVariable("REQUEST_LIMIT_PER_MINUTE") ?? "60");
        private const string TableName = "RateLimitTable";
        private const string QueueName = "RateLimitQueue";
        private const string StorageConnectionString = "YourStorageConnectionString"; // Replace with your storage connection string

        [FunctionName("RateLimit")]
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

            // Initialize Table Storage
            var tableClient = new TableClient(StorageConnectionString, TableName);
            await tableClient.CreateIfNotExistsAsync();

            var entity = new RateLimitEntity(userId);
            try
            {
                var existingEntity = await tableClient.GetEntityAsync<RateLimitEntity>(userId, userId);
                entity = existingEntity.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                // Entity does not exist, we will create a new one
            }

            if (entity.RequestCount >= RequestLimit)
            {
                // Initialize Queue Storage
                var queueClient = new QueueClient(StorageConnectionString, QueueName);
                await queueClient.CreateIfNotExistsAsync();

                var messageContent = $"{userId}-{DateTime.UtcNow}";
                await queueClient.SendMessageAsync(messageContent);

                return new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
            }

            // Update request count
            entity.RequestCount++;
            await tableClient.UpsertEntityAsync(entity);

            return new OkObjectResult("Request accepted.");
        }

        public class RateLimitEntity : Azure.Data.Tables.ITableEntity
        {
            public RateLimitEntity() { }

            public RateLimitEntity(string userId)
            {
                PartitionKey = userId;
                RowKey = userId;
                RequestCount = 0;
                Timestamp = DateTimeOffset.UtcNow;
            }

            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }

            public int RequestCount { get; set; }
        }
    }
}
