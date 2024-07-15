using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JanssenIo.ReviewBot.Azure;

public class ParseArchive
{
    private readonly ILogger<ParseArchive> logger;
    private readonly Download.IFetchArchives downloader;
    private readonly Parse.IParseArchives parser;
    private readonly Store.ISaveReviews inserter;
    private readonly IStoreConfiguration runtimeConfig;

    public ParseArchive(
        ILogger<ParseArchive> logger,
        Download.IFetchArchives downloader,
        Parse.IParseArchives parser,
        Store.ISaveReviews inserter,
        IStoreConfiguration runtimeConfig)
    {
        this.logger = logger;
        this.downloader = downloader;
        this.parser = parser;
        this.inserter = inserter;
        this.runtimeConfig = runtimeConfig;
    }

    const string dailyAtSix = "0 0 6 * * *";

    [Function(nameof(ParseArchive))]
    public async Task Run([TimerTrigger(dailyAtSix)] TimerInfo timer)
    {
        logger.LogTrace("Triggered Archive Parser - Downloading and Parsing Archive");
        try
        {
            using Stream archive = await downloader.Download();

            var reviews = parser.Parse(archive).ToArray();

            await inserter.SaveMany(reviews);

            logger.LogTrace(new EventId(1), "ArchiveParser completed successfully.");
            await UpdateLastRun(DateTime.UtcNow);
        }
        catch (Exception e)
        {
            logger.LogCritical(new EventId(0), e, "Unexpected failure");
        }
    }

    private Task UpdateLastRun(DateTime lastRunDate)
        => runtimeConfig.Write(
            Download.LastRunKey,
            lastRunDate.ToUniversalTime().ToString(Download.LastRunFormat));
}
