using JanssenIo.ReviewBot.ArchiveParser;
using LiteDB;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public class RecentByBottleHandler : IReplyToCommands
    {
        private readonly ILiteCollection<Review> reviews;

        public RecentByBottleHandler(ILiteCollection<Review> reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            var regex = new Regex("u/review_bot bottle ([\"'])((?:\\\\1|.)*?)\\1", RegexOptions.IgnoreCase);
            var matches = regex.Matches(body);
            if (matches.Count == 0)
                yield break;

            foreach(Match match in matches)
            {
                var bottle = match.Groups[2].Value;
                var mostRecentReviews = this.reviews
                    .Query()
                    .Where(review => review.Author != null && review.Bottle != null
                       && review.Author.ToLower() == author.ToLower()
                       && review.Bottle.ToLower().Contains(bottle.ToLower()))
                    .OrderByDescending(r => r.PublishedOn)
                    .Limit(10)
                    .ToArray();

                var text = new StringBuilder($"{author}'s latest '{bottle}' reviews:");
                text.AppendLine();
                text.AppendLine();
                text.AppendLine(MarkdownListFormatter.Format(mostRecentReviews));

                yield return text.ToString();
            }
        }
    }
}
