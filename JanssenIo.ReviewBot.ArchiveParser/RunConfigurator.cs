using JanssenIo.ReviewBot.Core;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    public class RunConfigurator
    {
        private readonly IStoreConfiguration configuration;
        private static readonly string lastRunKey = nameof(Download.Configuration.LastRunDate);
        private static readonly string lastRunFormat = "yyyy-MM-dd";

        public RunConfigurator(IStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<DateTime> GetLastRun()
        {
            string lastRun = await this.configuration.Read(lastRunKey);

            if (string.IsNullOrEmpty(lastRun)
                || !DateTime.TryParseExact(
                        lastRun, lastRunFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None,
                        out DateTime d))
            {
                return DateTime.MinValue;
            }

            return d;
        }

        public async Task UpdateLastRun()
        {
            var today = DateTime.Today.ToString(lastRunFormat);

            await this.configuration.Write(lastRunKey, today);
        }
    }
}
