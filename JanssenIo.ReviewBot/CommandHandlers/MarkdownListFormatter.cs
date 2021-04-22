using JanssenIo.ReviewBot.ArchiveParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    internal static class MarkdownListFormatter
    {
        public static string Format(ICollection<Review> reviews)
        {
            if (reviews.Count == 0)
                return "_Nothing here..._";

            return string.Join(
                Environment.NewLine,
                reviews.Select(r => $"*{r.Score} - [{r.Bottle}]({r.Link})"));
        }
    }
}
