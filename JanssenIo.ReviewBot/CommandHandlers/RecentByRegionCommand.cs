using JanssenIo.ReviewBot.ArchiveParser;
using LiteDB;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public class RecentByRegionHandler : IReplyToCommands
    {
        private readonly ILiteCollection<Review> reviews;

        public RecentByRegionHandler(ILiteCollection<Review> reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            var regex = new Regex("/u/review_bot region ([\"'])((?:\\\\1|.)*?)\\1");
            var matches = regex.Matches(body);
            if (matches.Count == 0)
                yield break;

            foreach(Match match in matches)
            {
                var region = match.Groups[2].Value;
                var mostRecentReviews = this.reviews
                    .Query()
                    .Where(review => review.Author != null && review.Region != null
                       && review.Author.ToLower() == author.ToLower()
                       && review.Region.ToLower().Contains(region.ToLower()))
                    .OrderByDescending(r => r.PublishedOn)
                    .Limit(10)
                    .ToArray();

                var text = new StringBuilder($"{author}'s latest '{region}' reviews:");
                text.AppendLine();
                text.AppendLine(MarkdownListFormatter.Format(mostRecentReviews));

                yield return text.ToString();
            }
        }
    }
}
