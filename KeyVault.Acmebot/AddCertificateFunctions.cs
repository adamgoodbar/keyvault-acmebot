using System.IO;
using System.Threading.Tasks;

using KeyVault.Acmebot.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace KeyVault.Acmebot
{
    public class AddCertificateFunctions
    {
        [FunctionName(nameof(AddCertificate_HttpStart))]
        public async Task<IActionResult> AddCertificate_HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "add-certificate")] HttpRequest req,
            [DurableClient] IDurableClient starter,
            ILogger log)
        {
            if (!req.HttpContext.User.Identity.IsAuthenticated)
            {
                return new UnauthorizedObjectResult("Need to activate EasyAuth.");
            }

            var request = JsonConvert.DeserializeObject<AddCertificateRequest>(await req.ReadAsStringAsync());

            if (request?.Domains == null || request.Domains.Length == 0)
            {
                return new BadRequestObjectResult($"{nameof(request.Domains)} is empty.");
            }

            // Function input comes from the request content.
            var instanceId = await starter.StartNewAsync(nameof(SharedFunctions.IssueCertificate), request.Domains);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId, true);
        }

        [FunctionName(nameof(add-certificate))]
        public static async Task<HttpResponseMessage> AddCertificate(HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
        
            string html = File.ReadAllText("index.v2.html");
            
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(html);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }
    }
}