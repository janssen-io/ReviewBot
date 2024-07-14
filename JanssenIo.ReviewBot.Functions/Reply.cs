using static JanssenIo.ReviewBot.Replies.ReviewBot;

using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace JanssenIo.ReviewBot.Functions;

public class Reply
{
    private readonly InboxReplier inbox;
    private readonly ILogger<Reply> logger;

    public Reply(InboxReplier inbox, ILogger<Reply> logger)
    {
        this.inbox = inbox;
        this.logger = logger;
    }

    const string everyFiveMinutes = "0 */5 * * * *";

    [Function("Reply")]
    public void Run([TimerTrigger(everyFiveMinutes)] TimerInfo timer)
    {
        this.logger.LogInformation("C# HTTP trigger function processed a request.");

        inbox.ReadMessages();
    }
}
