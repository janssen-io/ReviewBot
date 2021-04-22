using JanssenIo.ReviewBot.ArchiveParser;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public class RecentBySubredditHandler : IReplyToCommands
    {
        private readonly ILiteCollection<Review> reviews;

        public RecentBySubredditHandler(ILiteCollection<Review> reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            var regex = new Regex("/u/review_bot (/r/[a-z0-9][a-z0-9_]{2,20})", RegexOptions.IgnoreCase);
            var matches = regex.Matches(body);
            if (matches.Count == 0)
                yield break;

            var subreddits = matches
                .Select(m => m.Groups[1].Value)
                .Distinct();

            foreach(var subreddit in subreddits)
            {
                var mostRecentReviews = this.reviews
                    .Query()
                    .Where(review => review.Author != null && review.Link != null
                       && review.Author.ToLower() == author.ToLower()
                       && review.Link.ToLower().Contains(subreddit.ToLower()))
                    .OrderByDescending(r => r.PublishedOn)
                    .Limit(10)
                    .ToArray();

                var text = new StringBuilder($"{author}'s latest reviews in {subreddit}:");
                text.AppendLine();
                text.AppendLine(MarkdownListFormatter.Format(mostRecentReviews));

                yield return text.ToString();
            }
        }
    }
}
