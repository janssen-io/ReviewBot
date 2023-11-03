using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JanssenIo.ReviewBot.Azure
{
    public class Reply
    {
        private readonly ReviewBot.InboxReplier inbox;

        public Reply(ReviewBot.InboxReplier inbox)
        {
            this.inbox = inbox;
        }

        [FunctionName("Reply")]
        public Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            inbox.ReadMessages();

            return Task.FromResult<IActionResult>(new NoContentResult());
        }

    }
}
