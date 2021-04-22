using JanssenIo.ReviewBot.CommandHandlers;
using LiteDB;
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

namespace JanssenIo.ReviewBot
{
    internal static class ReviewBot
    {
        public static readonly EventId UnexpectedErrorId = new EventId(5000, "Unexpected Bot Error");
        public static readonly EventId NoCommandId = new EventId(5001, "Message Without Command");
        public static readonly EventId RepliedId = new EventId(5002, "Message Without Command");

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
            private readonly ILogger<InboxReplier> inboxLogger;
            private readonly IEnumerable<IReplyToCommands> repliers;
            private readonly Configuration config;

            public Service(
                IHostApplicationLifetime appLifeTime,
                ILogger<Service> logger,
                ILogger<InboxReplier> inboxLogger,
                IEnumerable<IReplyToCommands> repliers,
                Configuration config
                )
            {
                this.appLifeTime = appLifeTime;
                this.logger = logger;
                this.inboxLogger = inboxLogger;
                this.repliers = repliers;
                this.config = config;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                appLifeTime.ApplicationStarted.Register(() =>
                {
                    Task.Run(() =>
                    {
                        RedditClient? reddit = null;
                        try
                        {
                            reddit = new RedditClient(
                                appId: config.AppId,
                                refreshToken: config.RefreshToken,
                                appSecret: config.AppSecret);

                            new InboxReplier(reddit, inboxLogger, repliers)
                                .ReadMessages();
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

        internal class InboxReplier
        {
            private readonly RedditClient reddit;
            private readonly ILogger<InboxReplier> logger;
            private readonly IEnumerable<IReplyToCommands> repliers;

            public InboxReplier (
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
                foreach(var message in reddit.Account.Messages.Unread)
                {
                    try
                    {
                        HandleMessage(message);
                        if (message.Author == "FlockOnFire")
                            reddit.Account.Messages.ReadMessage(message.Name);
                    }
                    catch(Exception e)
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
                comment.AppendLine("^(More info? Ask) [^(FlockOnFire)](https://reddit.com/u/flockonfire) ^(or click) [^(here.)](https://github.com/FlockBots/review-bot)");

                var replyText = comment.ToString();
                logger.LogInformation(RepliedId, "{Author} mentioned me with a command: {Body}" + Environment.NewLine + "{ReplyText}", message.Author, message.Body, replyText);

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
