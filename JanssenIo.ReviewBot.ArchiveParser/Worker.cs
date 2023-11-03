using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    internal class Worker : IHostedService
    {
        private readonly IHostApplicationLifetime appLifeTime;
        private readonly ILogger logger;
        private readonly Download.IFetchArchives downloader;
        private readonly Parse.IParseArchives parser;
        private readonly Store.ISaveReviews inserter;

        public Worker(
            IHostApplicationLifetime appLifeTime,
            ILogger<Worker> logger,
            Download.IFetchArchives downloader,
            Parse.IParseArchives parser,
            Store.ISaveReviews inserter
            )
        {
            this.appLifeTime = appLifeTime;
            this.logger = logger;
            this.downloader = downloader;
            this.parser = parser;
            this.inserter = inserter;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            appLifeTime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        using Stream archive = await downloader.Download();

                        var reviews = parser.Parse(archive).ToArray();

                        await inserter.SaveMany(reviews);

                        logger.LogInformation(new EventId(1), "ArchiveParser completed successfully.");
                        UpdateLastRun();
                    }
                    catch (Exception e)
                    {
                        logger.LogCritical(new EventId(0), e, "Unexpected failure");
                    }
                    finally
                    {
                        appLifeTime.StopApplication();
                    }
                });
            });


            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Stopped {nameof(Worker)}.");
            return Task.CompletedTask;
        }

        private static void UpdateLastRun()
        {
            var appSettingsText = File.ReadAllText("appsettings.json");
            var lastRun = nameof(Download.Configuration.LastRunDate);
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            // Don't capture the quotes before the date, otherwise the replacement pattern doesn't work
            // It will then read: '$12021-'; 12021 is not a valid capture group.
            var regex = new Regex($@"({lastRun}"": )""(.*?)("")");
            var newSettings = regex.Replace(appSettingsText, @$"$1""{today}$3");

            File.WriteAllText("appsettings.json", newSettings);
        }
    }
}
