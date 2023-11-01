﻿using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.CommandHandlers;
using LiteDB;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Linq;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot
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
                            c => c.ConnectionString = key,
                            _ => { });

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
            services.AddReviewBot(context);
            services.AddLogging();
        }

    }
}
