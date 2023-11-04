using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

namespace JanssenIo.ReviewBot.ArchiveParser;

public static class ServiceCollectionExtensions
{
    public static void AddArchiveParser(this IServiceCollection services, Action<IServiceCollection> registerReviewStore)
    {
        services.AddHttpClient();
        services.BindConfiguration<Download.Configuration>(nameof(Download));
        services.BindConfiguration<Store.Configuration>(nameof(Store));

        services.AddTransient<Download.IFetchArchives, Download.GoogleSheetsDownloader>();
        services.AddTransient<Parse.IParseArchives, Parse.GoogleSheetsParser>();

        registerReviewStore(services);

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
