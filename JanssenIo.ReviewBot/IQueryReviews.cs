using JanssenIo.ReviewBot.ArchiveParser;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JanssenIo.ReviewBot
{
    public interface IQueryReviews
    {
        Review[] Where(Func<Review, bool> filter);
    }

    public class Latest10Query : IQueryReviews
    {
        private readonly IQueryReviews innerQuery;

        public Latest10Query(IQueryReviews innerQuery)
        {
            this.innerQuery = innerQuery;
        }

        public Review[] Where(Func<Review, bool> filter)
            => innerQuery.Where(filter)
                .OrderByDescending(r => r.PublishedOn)
                .Take(10)
                .ToArray();
    }

    public class CosmosQueryAdapter : IQueryReviews
    {
        private readonly Container reviews;

        public CosmosQueryAdapter(Container container)
        {
            this.reviews = container;
        }

        public Review[] Where(Func<Review, bool> filter)
            => this.reviews
                .GetItemLinqQueryable<Review>()
                .Where(filter)
                .ToArray();
            
    }
}
