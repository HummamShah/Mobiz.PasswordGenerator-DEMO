using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Mobiz.PasswordGenerator
{
    public static class ProcessDelayedRequests
    {
        private static readonly string StorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? throw new InvalidOperationException("AzureWebJobsStorage is not set.");
        private const string QueueName = "RateLimitQueue";

        [FunctionName("ProcessDelayedRequests")]
        public static async Task Run([QueueTrigger(QueueName, Connection = "AzureWebJobsStorage")] string message, ILogger log)
        {
            log.LogInformation($"Processing delayed request: {message}");

            // Extract userId and timestamp from the message
            var parts = message.Split('-');
            var userId = parts[0];
            var timestamp = DateTime.Parse(parts[1]);

            // Implement your delayed processing logic here
            // For example, you can generate the passwords and send them to the user via email
            var passwords = GeneratePasswords(); // Implement your password generation logic

            log.LogInformation($"Processed request for user {userId} generated at {timestamp}");
        }

        private static string[] GeneratePasswords()
        {
            // Implement your password generation logic here
            return new string[] { "DelayedPassword1!", "DelayedPassword2@" };
        }
    }
}





