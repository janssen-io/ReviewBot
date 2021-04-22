using JanssenIo.ReviewBot.ArchiveParser;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public class LatestReviewsHandler : IReplyToCommands
    {
        private readonly ILiteCollection<Review> reviews;

        public LatestReviewsHandler(ILiteCollection<Review> reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            if (!body.Contains("/u/review_bot latest"))
                return Enumerable.Empty<string>();

            var mostRecentReviews = this.reviews
                .Query()
                .Where(review => review.Author != null
                   && review.Author.ToLower() == author.ToLower())
                .OrderByDescending(r => r.PublishedOn)
                .Limit(10)
                .ToArray();

            var text = new StringBuilder($"{author}'s latest reviews:");
            text.AppendLine();
            text.AppendLine();
            text.AppendLine(MarkdownListFormatter.Format(mostRecentReviews));

            return new[] { text.ToString() };
        }
    }
}
