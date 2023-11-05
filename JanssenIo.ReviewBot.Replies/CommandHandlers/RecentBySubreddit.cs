using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JanssenIo.ReviewBot.Replies.CommandHandlers
{
    public class RecentBySubredditHandler : IReplyToCommands
    {
        private readonly IQueryReviews reviews;

        public RecentBySubredditHandler(IQueryReviews reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            var regex = new Regex("u/review_bot (/r/[a-z0-9][a-z0-9_]{2,20})", RegexOptions.IgnoreCase);
            var matches = regex.Matches(body);
            if (matches.Count == 0)
                yield break;

            var subreddits = matches
                .Select(m => m.Groups[1].Value)
                .Distinct();

            foreach (var subreddit in subreddits)
            {
                var mostRecentReviews = reviews
                    .Where(review => review.Author != null && review.Link != null
                       && review.Author.ToLower() == author.ToLower()
                       && review.Link.ToLower().Contains(subreddit.ToLower()), author);

                var text = new StringBuilder($"{author}'s latest reviews in {subreddit}:");
                text.AppendLine();
                text.AppendLine();
                text.AppendLine(MarkdownListFormatter.Format(mostRecentReviews));

                yield return text.ToString();
            }
        }
    }
}
