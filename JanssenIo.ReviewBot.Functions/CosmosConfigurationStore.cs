using JanssenIo.ReviewBot.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.Functions
{
    internal class CosmosConfigurationStore : IStoreConfiguration
    {
        private readonly Container configuration;

        public CosmosConfigurationStore(Container container)
        {
            this.configuration = container;
        }

        public async Task<string> Read(string key)
            => (await TryRead(key, out string value))
                ? value
                : throw new ItemNotFound(key);

        public Task<bool> TryRead(string key, out string value)
        {
            var items = this.configuration
                .GetItemLinqQueryable<Item>(allowSynchronousQueryExecution: true)
                .Where(item => item.Id == key)
                .ToArray();

            if (items.Length != 1)
            {
                value = string.Empty;
                return Task.FromResult(false);
            }

            value = items[0].Value;
            return Task.FromResult(true);
        }

        public Task Write(string key, string value)
        {
            return this.configuration.UpsertItemAsync(new Item
            {
                Id = key,
                Value = value
            });
        }

        private sealed class Item
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }
    }
}
