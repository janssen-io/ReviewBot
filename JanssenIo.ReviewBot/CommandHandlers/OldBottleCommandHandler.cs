using JanssenIo.ReviewBot.ArchiveParser;
using LiteDB;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public class OldBottleCommandHandler : IReplyToCommands
    {
        private readonly ILiteCollection<Review> reviews;

        public OldBottleCommandHandler(ILiteCollection<Review> reviews)
        {
            this.reviews = reviews;
        }

        public IEnumerable<string> ReplyTo(string author, string body)
        {
            var regex = new Regex("u/review_bot ([\"'])((?:\\\\1|.)*?)\\1", RegexOptions.IgnoreCase);
            var matches = regex.Matches(body);
            if (matches.Count == 0)
                yield break;

            foreach(Match match in matches)
            {
                var keyword = match.Groups[2].Value;
                yield return $"I didn't understand your command. Did you mean `/u/review_bot bottle '{keyword}'` or `/u/review_bot region '{keyword}'`?";
            }
        }
    }
}
