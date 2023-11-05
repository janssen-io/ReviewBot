using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace JanssenIo.ReviewBot.Replies.CommandHandlers
{
    public class RecentByBottleHandler : IReplyToCommands
    {
        private readonly IQueryReviews reviews;

        public RecentByBottleHandler(IQueryReviews reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            var regex = new Regex("u/review_bot bottle ([\"'])((?:\\\\1|.)*?)\\1", RegexOptions.IgnoreCase);
            var matches = regex.Matches(body);
            if (matches.Count == 0)
                yield break;

            foreach (Match match in matches)
            {
                var bottle = match.Groups[2].Value;
                var mostRecentReviews = reviews
                    .Where(review => review.Author != null && review.Bottle != null
                       && review.Author.ToLower() == author.ToLower()
                       && review.Bottle.ToLower().Contains(bottle.ToLower()), author);

                var text = new StringBuilder($"{author}'s latest '{bottle}' reviews:");
                text.AppendLine();
                text.AppendLine();
                text.AppendLine(MarkdownListFormatter.Format(mostRecentReviews));

                yield return text.ToString();
            }
        }
    }
}
