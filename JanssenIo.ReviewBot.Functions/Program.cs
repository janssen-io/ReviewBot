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
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

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
    .ConfigureLogging(logging =>
    {
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });
    })
    .Build();

await host.RunAsync();

