using JanssenIo.ReviewBot.ArchiveParser;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public class LatestReviewsHandler : IReplyToCommands
    {
        private readonly IQueryReviews reviews;

        public LatestReviewsHandler(IQueryReviews reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            if (!body.Contains("u/review_bot latest", System.StringComparison.OrdinalIgnoreCase))
                return Enumerable.Empty<string>();

            var mostRecentReviews = this.reviews
                .Where(review => review.Author != null
                   && review.Author.ToLower() == author.ToLower());

            var text = new StringBuilder($"{author}'s latest reviews:");
            text.AppendLine();
            text.AppendLine();
            text.AppendLine(MarkdownListFormatter.Format(mostRecentReviews));

            return new[] { text.ToString() };
        }
    }
}
