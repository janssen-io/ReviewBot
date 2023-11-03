using JanssenIo.ReviewBot.CommandHandlers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Reddit;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using JanssenIo.ReviewBot.ArchiveParser;
using Microsoft.Extensions.Options;

namespace JanssenIo.ReviewBot
{
    public static class ServiceCollectionExtensions
    {
        public static void AddReviewBot(this IServiceCollection services)
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

            // TODO: move to Azure-project
            services.AddScoped<IQueryReviews, Latest10Query>(services =>
            {
                var options = services.GetService<IOptions<StoreConfiguration>>()!;
                var db = new CosmosClient(options.Value.ConnectionString).GetDatabase("bot-db");
                var container = db.GetContainer("whiskyreviews");
                return new Latest10Query(new CosmosQueryAdapter(container));
            });
        }
    }
}
