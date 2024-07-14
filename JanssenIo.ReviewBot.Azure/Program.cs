﻿using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.Azure;
using JanssenIo.ReviewBot.Core;
using JanssenIo.ReviewBot.Replies;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StoreConfiguration = JanssenIo.ReviewBot.Azure.StoreConfiguration;

var host = new HostBuilder()
    .ConfigureServices(services =>
    {
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

        services.BindConfiguration<AppInsightsConfig>("ApplicationInsights");
        services.AddTransient<IStoreConfiguration, CosmosConfigurationStore>(services =>
        {
            var cosmos = services.GetService<CosmosClient>()!;
            var container = cosmos.GetDatabase("bot-db").GetContainer("reviewbot-config");
            return new CosmosConfigurationStore(container);
        });

        services.AddLogging(l =>
        {
            l.AddApplicationInsights(
                c =>
                {
                    var aiConfig = services.BuildServiceProvider().GetService<IOptions<AppInsightsConfig>>();
                    c.ConnectionString = aiConfig.Value.ConnectionString;
                },
                _ => { });
        });
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

await host.RunAsync();

