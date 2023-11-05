using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using JanssenIo.ReviewBot.ArchiveParser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Cosmos;
using JanssenIo.ReviewBot.Core;
using JanssenIo.ReviewBot.Replies;

[assembly: FunctionsStartup(typeof(JanssenIo.ReviewBot.Azure.Startup))]
namespace JanssenIo.ReviewBot.Azure;

public class Startup : FunctionsStartup
{
    public class AppInsightsConfig { public string ConnectionString { get; set; } }
    public class StoreConfiguration { public string ConnectionString { get; set; } }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddArchiveParser(services =>
        {
            services.BindConfiguration<StoreConfiguration>("Store");
            services.AddTransient<CosmosClient>(services =>
            {
                IOptions<StoreConfiguration> config =  services.GetService<IOptions<StoreConfiguration>>()!;
                return new CosmosClient(config.Value.ConnectionString);
            });

            services.AddTransient<Store.ISaveReviews, Store.LoggingInserter>(services =>
            {
                var logger = services.GetService<ILogger<CosmosReviewStore>>()!;
                var cosmos = services.GetService<CosmosClient>()!;
                var container = cosmos.GetDatabase("bot-db").GetContainer("whiskyreviews");
                Store.ISaveReviews innerStore = new CosmosReviewStore(logger, container);
                return new Store.LoggingInserter(logger, innerStore);
            });
        });

        builder.Services.AddReviewBot(services =>
        {
            services.AddScoped<IQueryReviews, Latest10Query>(services =>
            {
                var logger = services.GetService<ILogger<CosmosReviewStore>>()!;
                var cosmos = services.GetService<CosmosClient>()!;
                var container = cosmos.GetDatabase("bot-db").GetContainer("whiskyreviews");
                return new Latest10Query(new CosmosReviewStore(logger, container));
            });
        });

        builder.Services.BindConfiguration<AppInsightsConfig>("ApplicationInsights");
        builder.Services.AddTransient<IStoreConfiguration, CosmosConfigurationStore>(services =>
        {
            // TODO: move all cosmos related stuff to Azure project
            var cosmos = services.GetService<CosmosClient>()!;
            var container = cosmos.GetDatabase("bot-db").GetContainer("reviewbot-config");
            return new CosmosConfigurationStore(container);
        });

        builder.Services.AddLogging(l =>
        {
            l.AddApplicationInsights(
                c =>
                {
                    var aiConfig = builder.Services.BuildServiceProvider().GetService<IOptions<AppInsightsConfig>>();
                    c.ConnectionString = aiConfig.Value.ConnectionString;
                },
                _ => { });
        });
    }
}
