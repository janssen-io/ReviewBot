using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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

        const string everyFiveMinutes = "0 */5 * * * *";

        [FunctionName("Reply")]
        public void Run(
            [TimerTrigger(everyFiveMinutes)] TimerInfo timer,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            inbox.ReadMessages();
        }

    }
}
