using Microsoft.Extensions.DependencyInjection;
using Reddit;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using JanssenIo.ReviewBot.ArchiveParser;
using Microsoft.Extensions.Options;
using JanssenIo.ReviewBot.Replies.CommandHandlers;
using System;

namespace JanssenIo.ReviewBot.Replies
{
    public static class ServiceCollectionExtensions
    {
        public static void AddReviewBot(this IServiceCollection services, Action<IServiceCollection> registerIQueryReviews)
        {
            services.AddHttpClient();

            services.AddTransient<IReplyToCommands, LatestReviewsHandler>();
            services.AddTransient<IReplyToCommands, RecentByBottleHandler>();
            services.AddTransient<IReplyToCommands, RecentByRegionHandler>();
            services.AddTransient<IReplyToCommands, RecentBySubredditHandler>();

            services.BindConfiguration<ReviewBot.Configuration>("ReviewBot");
            services.BindConfiguration<StoreConfiguration>("Store");

            services.AddScoped<RedditClient>(services =>
            {
                var config = services.GetService<IOptions<ReviewBot.Configuration>>()!.Value;

                return new RedditClient(
                    appId: config.AppId,
                    refreshToken: config.RefreshToken,
                    appSecret: config.AppSecret);

            });

            services.AddScoped<ReviewBot.InboxReplier>(services =>
            {
                RedditClient reddit = services.GetService<RedditClient>()!;
                ILogger<ReviewBot.InboxReplier> inboxLogger = services.GetService<ILogger<ReviewBot.InboxReplier>>()!;
                IEnumerable<IReplyToCommands> repliers = services.GetServices<IReplyToCommands>();
                return new ReviewBot.InboxReplier(reddit, inboxLogger, repliers);
            });

            registerIQueryReviews(services);
        }
    }
}
