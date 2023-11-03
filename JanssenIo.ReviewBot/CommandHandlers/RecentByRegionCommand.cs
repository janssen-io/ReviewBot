using JanssenIo.ReviewBot.ArchiveParser;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public class RecentByRegionHandler : IReplyToCommands
    {
        private readonly IQueryReviews reviews;

        public RecentByRegionHandler(IQueryReviews reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            var regex = new Regex("u/review_bot region ([\"'])((?:\\\\1|.)*?)\\1", RegexOptions.IgnoreCase);
            var matches = regex.Matches(body);
            if (matches.Count == 0)
                yield break;

            foreach(Match match in matches)
            {
                var region = match.Groups[2].Value;
                var mostRecentReviews = this.reviews
                    .Where(review => review.Author != null && review.Region != null
                       && review.Author.ToLower() == author.ToLower()
                       && review.Region.ToLower().Contains(region.ToLower()), author);

                var text = new StringBuilder($"{author}'s latest '{region}' reviews:");
                text.AppendLine();
                text.AppendLine();
                text.AppendLine(MarkdownListFormatter.Format(mostRecentReviews));

                yield return text.ToString();
            }
        }
    }
}
