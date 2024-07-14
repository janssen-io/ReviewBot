using static JanssenIo.ReviewBot.Replies.ReviewBot;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace JanssenIo.ReviewBot.Azure
{
    public class Reply
    {
        private readonly InboxReplier inbox;

        public Reply(InboxReplier inbox)
        {
            this.inbox = inbox;
        }

        const string everyFiveMinutes = "0 */5 * * * *";

        [Function("Reply")]
        public void Run([TimerTrigger(everyFiveMinutes)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            inbox.ReadMessages();
        }

    }
}
