using JanssenIo.ReviewBot.ArchiveParser;
using Microsoft.Azure.Cosmos;
using System;
using System.Linq;

namespace JanssenIo.ReviewBot.Replies
{
    public interface IQueryReviews
    {
        Review[] Where(Func<Review, bool> filter, string author);
    }

    public class Latest10Query : IQueryReviews
    {
        private readonly IQueryReviews innerQuery;

        public Latest10Query(IQueryReviews innerQuery)
        {
            this.innerQuery = innerQuery;
        }

        public Review[] Where(Func<Review, bool> filter, string author)
            => innerQuery.Where(filter, author)
                .OrderByDescending(r => r.PublishedOn)
                .Take(10)
                .ToArray();
    }

    public class CosmosQueryAdapter : IQueryReviews
    {
        private readonly Container reviews;

        public CosmosQueryAdapter(Container container)
        {
            reviews = container;
        }

        public Review[] Where(Func<Review, bool> filter, string author)
            => reviews
                .GetItemLinqQueryable<Review>(allowSynchronousQueryExecution: true, requestOptions: new QueryRequestOptions() { PartitionKey = new PartitionKey(author) })
                .Where(filter)
                .OrderByDescending(r => r.PublishedOn)
                .Take(10)
                .ToArray();

    }
}
