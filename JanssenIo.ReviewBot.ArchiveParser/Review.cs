using System;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    public class Review
    {
        public Guid id { get; set; }
        public string? Bottle { get; set; }
        public string? Author { get; set; }
        public string? Link { get; set; }
        public string? Score { get; set; }
        public string? Region { get; set; }
        public DateTime? PublishedOn { get; set; }

        public Review()
        {
            id = Guid.NewGuid();
        }
    }
}
