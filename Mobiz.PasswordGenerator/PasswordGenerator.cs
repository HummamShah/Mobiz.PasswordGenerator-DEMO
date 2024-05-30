using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PasswordGenerator;
using System.Linq;
using System.Net;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure;

namespace Mobiz.PasswordGenerator
{
    public static class PasswordGenerator
    {
        [FunctionName("PasswordGenerator")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string numberOfPasswordsParam = req.Query["numberOfPasswords"];
            string passwordLengthParam = req.Query["passwordLength"];

            int numberOfPasswords;
            int passwordLength;

            if (!int.TryParse(numberOfPasswordsParam, out numberOfPasswords) || !int.TryParse(passwordLengthParam, out passwordLength))
            {
                return new BadRequestObjectResult("Invalid input. Please provide valid 'numberOfPasswords' and 'passwordLength'.");
            }
            if(passwordLength <= 6)
            {
                return new BadRequestObjectResult("Invalid input. Please provide valid 'passwordLength'. Minimum Password length is 7.");
            }
            var passwordGenerator = new Password(includeLowercase: true, includeUppercase: true, includeNumeric: true, includeSpecial: true, passwordLength: passwordLength);
            var passwords = Enumerable.Range(0, numberOfPasswords).Select(_ => passwordGenerator.Next()).ToArray();

            return new OkObjectResult(passwords);
        }
    }
   

}
