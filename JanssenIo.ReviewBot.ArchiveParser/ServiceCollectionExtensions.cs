using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JanssenIo.ReviewBot.ArchiveParser;

public static class ServiceCollectionExtensions
{
    public static void AddArchiveParser(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.BindConfiguration<Download.Configuration>(nameof(Download));
        services.BindConfiguration<Store.Configuration>(nameof(Store));

        services.AddTransient<Download.IFetchArchives, Download.GoogleSheetsDownloader>();
        services.AddTransient<Parse.IParseArchives, Parse.GoogleSheetsParser>();

        services.AddTransient<CosmosClient>(services =>
        {
            IOptions<Store.Configuration> config = services.GetService<IOptions<Store.Configuration>>()!;
            return new CosmosClient(config.Value.ConnectionString);
        });

        services.AddTransient<Store.ISaveReviews, Store.LoggingInserter>(services =>
        {
            var logger = services.GetService<ILogger<Store.CosmosDbInserter>>()!;
            var cosmos = services.GetService<CosmosClient>()!;
            Store.ISaveReviews innerStore = new Store.CosmosDbInserter(logger, cosmos, "bot-db");
            return new Store.LoggingInserter(logger, innerStore);
        });

        services.AddLogging();
    }

    public static void BindConfiguration<T>(this IServiceCollection services, string section)
        where T : class, new()
    {
        services.AddOptions<T>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                var s = configuration.GetSection(section);
                s.Bind(settings);
            });
    }
}
