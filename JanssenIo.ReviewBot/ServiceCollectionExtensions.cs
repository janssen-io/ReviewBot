using JanssenIo.ReviewBot.CommandHandlers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Reddit;
using System.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace JanssenIo.ReviewBot
{
    public static class ServiceCollectionExtensions
    {
        public static void AddReviewBot(this IServiceCollection services, HostBuilderContext context)
        {
            services.AddHostedService<ReviewBot.Service>();
            services.AddHttpClient();

            services.AddTransient<IReplyToCommands, LatestReviewsHandler>();
            services.AddTransient<IReplyToCommands, RecentByBottleHandler>();
            services.AddTransient<IReplyToCommands, RecentByRegionHandler>();
            services.AddTransient<IReplyToCommands, RecentBySubredditHandler>();

            services.BindConfiguration<ReviewBot.Configuration>(context, "ReviewBot");
            services.BindConfiguration<StoreConfiguration>(context, "Store");

            services.AddScoped<Container>(
                container =>
                {
                    var options = container.GetService<StoreConfiguration>();
                    var db = new CosmosClient(options.ConnectionString).GetDatabase("bot-db");
                    return db.GetContainer("whiskyreviews");
                });

            services.AddScoped<RedditClient>(services =>
            {
                ReviewBot.Configuration config = services.GetService<ReviewBot.Configuration>();

                return new RedditClient(
                    appId: config.AppId,
                    refreshToken: config.RefreshToken,
                    appSecret: config.AppSecret);

            });

            services.AddScoped<ReviewBot.InboxReplier>(services =>
            {
                RedditClient reddit = services.GetService<RedditClient>();
                ILogger<ReviewBot.InboxReplier> inboxLogger = services.GetService<ILogger<ReviewBot.InboxReplier>>(); 
                IEnumerable<IReplyToCommands> repliers = services.GetServices<IReplyToCommands>();
                return new ReviewBot.InboxReplier(reddit, inboxLogger, repliers);
            });
        }

        static void BindConfiguration<T>(this IServiceCollection services, HostBuilderContext host, string section)
            where T : class, new()
        {
            var config = new T();
            host.Configuration
                .GetSection(section)
                .Bind(config);

            services.AddSingleton(config);
        }
    }
}
