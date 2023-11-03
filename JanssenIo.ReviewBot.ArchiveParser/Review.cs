using Newtonsoft.Json;
using System;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    public class Review
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public string? Bottle { get; set; }
        public string? Author { get; set; }
        public string? Link { get; set; }
        public string? Score { get; set; }
        public string? Region { get; set; }
        public DateTime? PublishedOn { get; set; }
        public DateTime? SubmittedOn { get; set; }

        public Review()
        {
            Id = Guid.NewGuid();
        }
    }
}
