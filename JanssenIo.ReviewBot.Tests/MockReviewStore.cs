using JanssenIo.ReviewBot.ArchiveParser;
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

        public Review[] Where(Func<Review, bool> filter)
        {
            return reviews.Where(filter).ToArray();
        }
    }
}
