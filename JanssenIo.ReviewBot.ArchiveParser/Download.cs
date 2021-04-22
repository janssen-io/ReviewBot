using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    internal static class Download 
    {
        public class Configuration
        {
            public DateTime? LastRunDate { get; set; }
            public Uri? Location { get; set; }
            public string? FilePath { get; set; }
        }

        internal interface IFetchArchives
        {
            Task<string> Download();
        }

        internal class GoogleSheetsDownloader : IFetchArchives
        {
            private readonly HttpClient httpClient;
            private readonly Configuration config;

            public GoogleSheetsDownloader(HttpClient httpClient, Configuration config)
            {
                this.httpClient = httpClient;
                this.config = config;
            }

            public async Task<string> Download()
            {
                string saveTo = GetSaveLocation();
                Uri archiveUri = GetArchiveUri();

                var response = await httpClient.GetAsync(archiveUri);
                response.EnsureSuccessStatusCode();
                var csv = await response.Content.ReadAsStringAsync();

                using var writer = new StreamWriter(saveTo, append: false);
                await writer.WriteAsync(csv);

                return saveTo;
            }

            private Uri GetArchiveUri()
            {
                DateTime lastRunDate = config.LastRunDate ?? DateTime.MinValue;

                var parameters = new StringBuilder();
                parameters.Append("?tqx=out:csv");
                parameters.Append($"&tq=SELECT * WHERE A >= date '{lastRunDate:yyyy-MM-dd}'");

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
