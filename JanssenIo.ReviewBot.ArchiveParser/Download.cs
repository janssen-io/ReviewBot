using JanssenIo.ReviewBot.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    public static class Download 
    {
        public const string LastRunKey = "LastRunDate";
        public const string LastRunFormat = "yyyy-MM-dd";

        private static readonly EventId DownloadStarted = new EventId(6000, "Download Started");

        public class Configuration
        {
            public DateTime? LastRunDate { get; set; }
            public Uri? Location { get; set; }
            public string? FilePath { get; set; }
        }

        public interface IFetchArchives
        {
            Task<Stream> Download();
        }

        public class GoogleSheetsDownloader : IFetchArchives
        {
            private readonly HttpClient httpClient;
            private readonly Configuration config;
            private readonly IStoreConfiguration runtimeConfig;
            private readonly ILogger<IFetchArchives> logger;

            public GoogleSheetsDownloader(HttpClient httpClient, IOptions<Configuration> config, IStoreConfiguration runtimeConfig, ILogger<IFetchArchives> logger)
            {
                this.httpClient = httpClient;
                this.config = config.Value;
                this.runtimeConfig = runtimeConfig;
                this.logger = logger;
            }

            public async Task<Stream> Download()
            {
                Uri archiveUri = await GetArchiveUri();
                this.logger.LogInformation(DownloadStarted, "Downloading reviews from {ArchiveUri}", archiveUri);

                var response = await httpClient.GetAsync(archiveUri);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }

            private async Task<Uri> GetArchiveUri()
            {
                DateTime lastRunDate = DateTime.MinValue;

                // Parse DateTime to validate it
                if (
                    await runtimeConfig.TryRead(LastRunKey, out string lastRunDateString)
                    && DateTime.TryParseExact(
                        lastRunDateString, LastRunFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime parsedLastRun))
                {
                    lastRunDate = parsedLastRun;
                }

                DateTime today = DateTime.Today;

                var parameters = new StringBuilder();
                parameters.Append("?tqx=out:csv");
                parameters.Append(
                    $"&tq=SELECT * WHERE A >= date '{lastRunDate.ToString(LastRunFormat)}'"
                    + $" AND A < date '{today.ToString(LastRunFormat)}'");

                return new UriBuilder(config.Location!)
                {
                    Query = parameters.ToString()
                }.Uri;
            }

            private string GetSaveLocation()
            {
                var tmpDir = Path.GetTempPath();
                var tmpFile = Path.GetRandomFileName();
                var tmpPath = Path.Combine(tmpDir, tmpFile);

                var location = config.FilePath ?? tmpPath;
                using var fs = new FileStream(location, FileMode.OpenOrCreate);

                return location;
            }
        }
    }
}
