using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Logging.Console;
using System.Linq;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(ConfigureAppConfig)
                .ConfigureLogging((host, logger) => {
                    logger.ClearProviders();

                    var key = host.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");

                    logger
                        .AddApplicationInsights(
                            configureTelemetryConfiguration: a => a.ConnectionString = key,
                            configureApplicationInsightsLoggerOptions: _ => { });

#pragma warning disable S3358 // Ternary operators should not be nested
                    var consoleLevel
                        = args.Contains("-vvv") ? LogLevel.Trace
                        : args.Contains("-vv") ? LogLevel.Debug
                        : args.Contains("-v") ? LogLevel.Information
                        : args.Contains("-w") ? LogLevel.Warning
                        : LogLevel.Error;
#pragma warning restore S3358 // Ternary operators should not be nested

                    logger
                            .AddConsole()
                            .AddFilter<ConsoleLoggerProvider>(
                                logLevel => logLevel >= consoleLevel);
                })
                .ConfigureServices(ConfigureServices)
                .RunConsoleAsync();

            await host;
        }

        static void ConfigureAppConfig(IConfigurationBuilder config)
        {
            config
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("secrets.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }

        static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddHostedService<Worker>();
            services.AddHttpClient();

            services.BindConfiguration<Download.Configuration>(context, nameof(Download));
            services.BindConfiguration<Store.Configuration>(context, nameof(Store));

            services.AddTransient<Download.IFetchArchives, Download.GoogleSheetsDownloader>();
            services.AddTransient<Parse.IParseArchives, Parse.GoogleSheetsParser>();

            services.AddTransient<CosmosClient>(services =>
            {
                Store.Configuration config = services.GetService<Store.Configuration>();
                return new CosmosClient(config.ConnectionString);
            });

            services.AddTransient<Store.ISaveReviews, Store.LoggingInserter>(services =>
            {
                var logger = services.GetService<ILogger<Store.CosmosDbInserter>>();
                var cosmos = services.GetService<CosmosClient>();
                Store.ISaveReviews innerStore = new Store.CosmosDbInserter(logger, cosmos, "bot-db");
                return new Store.LoggingInserter(logger, innerStore);
            });

            services.AddLogging();
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
