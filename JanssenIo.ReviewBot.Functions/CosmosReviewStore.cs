using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.Replies;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.Functions
{
    internal class CosmosReviewStore : Store.ISaveReviews, IQueryReviews
    {
        private readonly Container container;
        private readonly ILogger<CosmosReviewStore> logger;

        public CosmosReviewStore(ILogger<CosmosReviewStore> logger, Container container)
        {
            this.container = container;
            this.logger = logger;
        }

        public async Task Save(Review review)
        {
            try
            {
                await SaveAsync(review);
            }
            catch (CosmosException e)
                when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                logger.LogInformation("Received HTTP 429, backing off for 5 seconds");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await Save(review);
            }
            catch (CosmosException e)
                when (e.StatusCode == HttpStatusCode.Conflict)
            {
                throw new Store.DuplicateReviewException("Review already exists in CosmosDB", e);
            }
        }

        public async Task SaveMany(IEnumerable<Review> reviews)
        {
            await Task.WhenAll(reviews.Select(Save));
            logger.LogInformation(new EventId(1), "ArchiveParser completed successfully.");
        }

        public Review[] Where(Expression<Func<Review, bool>> filter, string author)
            => container
                .GetItemLinqQueryable<Review>(allowSynchronousQueryExecution: true, requestOptions: new QueryRequestOptions() { PartitionKey = new PartitionKey(author) })
                .Where(filter)
                .OrderByDescending(r => r.PublishedOn)
                .Take(10)
                .ToArray();


        private Task<ItemResponse<Review>> SaveAsync(Review review)
            => container.CreateItemAsync(review);
    }
}
