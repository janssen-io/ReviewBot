namespace JanssenIo.ReviewBot.Core
{
    public interface IStoreConfiguration
    {
        Task<string> Read(string key);
        Task<bool> TryRead(string key, out string value);
        Task Write(string key, string value);
    }

    public class ItemNotFound : Exception
    {
        public ItemNotFound() : base()
        {
        }

        public ItemNotFound(string key) : base($"Item with key '{key}' was not found.")
        {
        }

        public ItemNotFound(string key, Exception? innerException) : base($"Item with key '{key}' was not found.", innerException)
        {
        }
    }
}