using LiteDB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    public static class Store
    {
        private static readonly EventId DuplicateReviewId = new EventId(3001, "Duplicate Review");
        private static readonly EventId UnexpectedErrorId = new EventId(3002, "Unexpected Error");
        private static readonly EventId DatabaseUpdatedId = new EventId(3000, "Database Updated");

        internal class Configuration
        {
            public string? ConnectionString { get; set; }
        }

        public interface ISaveReviews
        {
            public Task Save(Review review);
            public Task SaveMany(IEnumerable<Review> reviews);
        }

        public class DuplicateReviewException : Exception
        {
            public DuplicateReviewException() : base()
            {
            }

            public DuplicateReviewException(string? message) : base(message)
            {
            }

            public DuplicateReviewException(string? message, Exception? innerException) : base(message, innerException)
            {
            }
        }

        public class LoggingInserter : ISaveReviews
        {
            private readonly ILogger<ISaveReviews> logger;
            private readonly ISaveReviews store;

            public LoggingInserter(ILogger<ISaveReviews> logger, ISaveReviews store)
            {
                this.logger = logger;
                this.store = store;
            }

            public Task Save(Review review)
                => this.store.Save(review);

            public async Task SaveMany(IEnumerable<Review> reviews)
            {
                int numNew = 0, numDup = 0, numError = 0;

                foreach (var review in reviews)
                {
                    try
                    {
                        review.PublishedOn ??= review.SubmittedOn;
                        await Save(review);
                        numNew++;
                    }
                    catch (DuplicateReviewException e) 
                    // when (e.ErrorCode == LiteException.INDEX_DUPLICATE_KEY)
                    {
                        logger.LogWarning(DuplicateReviewId, "Duplicate: {Author}, {Bottle}, {Link}", review.Author, review.Bottle, review.Link);
                        numDup++;
                    }
                    catch (Exception e)
                    {
                        logger.LogCritical(UnexpectedErrorId, e, "Unexpected error while inserting review.");
                        numError++;
                    }
                }
                logger.LogInformation(DatabaseUpdatedId, "Database Updated successfully: {New} new, {Duplicates} duplicates, {Errors} errors", numNew, numDup, numError);
            }
        }

        internal class LiteDbInserter : ISaveReviews
        {
            private readonly ILiteCollection<Review> db;

            public LiteDbInserter(Configuration config)
            {
                var conn = new LiteDatabase(config.ConnectionString);
                this.db = OpenCollection(conn);
            }

            public Task Save(Review review)
                => Task.FromResult(db.Insert(review));

            public Task SaveMany(IEnumerable<Review> reviews)
                => Task.WhenAll(reviews.Select(Save).ToArray());

            private static ILiteCollection<Review> OpenCollection(LiteDatabase db)
            {
                var reviews = db.GetCollection<Review>("reviews");

                reviews.EnsureIndex("$.Author + $.Bottle + $.Link", true);

                return reviews;
            }
        }

    }
}
