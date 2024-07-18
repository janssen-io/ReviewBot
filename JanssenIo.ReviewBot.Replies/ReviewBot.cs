using JanssenIo.ReviewBot.Replies.CommandHandlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.Replies
{
    public static class ReviewBot
    {
        public static readonly EventId UnexpectedErrorId = new EventId(5000, "Unexpected Bot Error");
        public static readonly EventId NoCommandId = new EventId(5001, "Message Without Command");
        public static readonly EventId RepliedId = new EventId(5002, "Message With Command");
        public static readonly EventId RepliedWithId = new EventId(5003, "Message Body and Reply");

        public class Configuration
        {
            public string? AppId { get; set; }
            public string? RefreshToken { get; set; }
            public string? AppSecret { get; set; }

        }

        internal class Service : IHostedService
        {
            private readonly IHostApplicationLifetime appLifeTime;
            private readonly ILogger<Service> logger;
            private readonly InboxReplier inbox;

            public Service(
                IHostApplicationLifetime appLifeTime,
                ILogger<Service> logger,
                InboxReplier inbox
                )
            {
                this.appLifeTime = appLifeTime;
                this.logger = logger;
                this.inbox = inbox;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                appLifeTime.ApplicationStarted.Register(() =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            inbox.ReadMessages();
                        }
                        catch (Exception e)
                        {
                            logger.LogCritical(UnexpectedErrorId, e, e.Message);
                        }
                        finally
                        {
                            appLifeTime.StopApplication();
                        }
                    });
                });


                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                Console.WriteLine($"Stopped {nameof(ReviewBot)}.");
                return Task.CompletedTask;
            }
        }

        public class InboxReplier
        {
            private readonly RedditClient reddit;
            private readonly ILogger<InboxReplier> logger;
            private readonly IEnumerable<IReplyToCommands> repliers;

            public InboxReplier(
                RedditClient reddit,
                ILogger<InboxReplier> logger,
                IEnumerable<IReplyToCommands> repliers
                )
            {
                this.reddit = reddit;
                this.logger = logger;
                this.repliers = repliers;
            }

            public void ReadMessages()
            {
                foreach (var message in reddit.Account.Messages.Unread)
                {
                    try
                    {
                        HandleMessage(message);
                        reddit.Account.Messages.ReadMessage(message.Name);
                    }
                    catch (Exception e)
                    {
                        logger.LogCritical(UnexpectedErrorId, e, e.Message);
                    }
                }
            }

            private void HandleMessage(Message message)
            {
                var replies = repliers.SelectMany(r => r.ReplyTo(message.Author, message.Body));
                var comment = replies.Aggregate(
                    new StringBuilder(),
                    (newComment, reply) => newComment.AppendLine(reply));

                if (comment.Length == 0)
                {
                    logger.LogDebug(NoCommandId, "{Author} mentioned me without a command: {Body}", message.Author, message.Body);
                    return;
                }

                comment.AppendLine("___");
                comment.AppendLine("^(More info? Ask) [^(FlockOnFire)](https://reddit.com/u/flockonfire) ^(or click) [^(here.)](https://github.com/janssen-io/ReviewBot/)");

                var replyText = comment.ToString();
                logger.LogInformation(RepliedId, "{Author} mentioned me with a command", message.Author);
                logger.LogTrace(RepliedWithId, "{Body}" + Environment.NewLine + "{ReplyText}", message.Body, replyText);

                if (message.WasComment)
                {
                    var mention = reddit.Comment(message.Name);
                    mention.Reply(replyText);
                }
                else
                {
                    reddit.Account.Messages.Compose(
                        message.Author, message.Subject, replyText);
                }
            }
        }
    }
}
