using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.Replies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JanssenIo.ReviewBot.Tests
{
    internal class MockReviewStore : IQueryReviews
    {
        private readonly IEnumerable<Review> reviews;

        public MockReviewStore(IEnumerable<Review> reviews)
        {
            this.reviews = reviews;
        }

        public Review[] Where(Func<Review, bool> filter, string author)
        {
            return reviews.Where(filter).ToArray();
        }
    }
}
