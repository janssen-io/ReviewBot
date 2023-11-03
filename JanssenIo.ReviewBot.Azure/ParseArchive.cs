using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using JanssenIo.ReviewBot.ArchiveParser;
using System.Linq;
using JanssenIo.ReviewBot.Core;
using System.Web.Http;

namespace JanssenIo.ReviewBot.Azure
{
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

        [FunctionName(nameof(ParseArchive))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {

            try
            {
                using Stream archive = await downloader.Download();

                var reviews = parser.Parse(archive).ToArray();

                await inserter.SaveMany(reviews);

                logger.LogInformation(new EventId(1), "ArchiveParser completed successfully.");
                await UpdateLastRun(DateTime.UtcNow);
            }
            catch (Exception e)
            {
                logger.LogCritical(new EventId(0), e, "Unexpected failure");
                return new InternalServerErrorResult();
            }

            return new NoContentResult();
        }

        private Task UpdateLastRun(DateTime lastRunDate)
            => runtimeConfig.Write(
                Download.LastRunKey,
                lastRunDate.ToUniversalTime().ToString(Download.LastRunFormat));
    }
}
