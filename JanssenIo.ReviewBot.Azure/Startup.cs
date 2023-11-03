using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using JanssenIo.ReviewBot.ArchiveParser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Cosmos;
using JanssenIo.ReviewBot.Core;

[assembly: FunctionsStartup(typeof(JanssenIo.ReviewBot.Azure.Startup))]
namespace JanssenIo.ReviewBot.Azure;

public class Startup : FunctionsStartup
{
    public class AppInsightsConfig { public string ConnectionString { get; set; } }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddReviewBot();
        builder.Services.AddArchiveParser();
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
