using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.Core;
using JanssenIo.ReviewBot.Functions;
using JanssenIo.ReviewBot.Replies;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StoreConfiguration = JanssenIo.ReviewBot.Functions.StoreConfiguration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configure ILogger
        //services.BindConfiguration<AppInsightsConfig>("ApplicationInsights");
        //services.AddLogging(l =>
        //{
        //    l.AddApplicationInsights(
        //    c =>
        //    {
        //        var aiConfig = services.BuildServiceProvider().GetService<IOptions<AppInsightsConfig>>()!.Value;
        //        c.ConnectionString = aiConfig.ConnectionString;
        //    },
        //    _ => { });
        //});

        // Bot run config from CosmosDB
        services.AddTransient<IStoreConfiguration, CosmosConfigurationStore>(services =>
        {
            var cosmos = services.GetService<CosmosClient>()!;
            var container = cosmos.GetDatabase("bot-db").GetContainer("reviewbot-config");
            return new CosmosConfigurationStore(container);
        });

        // Parser
        services.AddArchiveParser(services =>
        {

            services.BindConfiguration<StoreConfiguration>("Store");
            services.AddTransient<CosmosClient>(services =>
            {
                IOptions<StoreConfiguration> config = services.GetService<IOptions<StoreConfiguration>>()!;
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

        // Replies
        services.AddReviewBot(services =>
        {
            services.AddScoped<IQueryReviews, Latest10Query>(services =>
            {
                var logger = services.GetService<ILogger<CosmosReviewStore>>()!;
                var cosmos = services.GetService<CosmosClient>()!;
                var container = cosmos.GetDatabase("bot-db").GetContainer("whiskyreviews");
                return new Latest10Query(new CosmosReviewStore(logger, container));
            });
        });
    })
    .Build();

await host.RunAsync();

