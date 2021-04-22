using LiteDB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace JanssenIo.ReviewBot.ArchiveParser
{
    internal static class Store
    {
        private static readonly EventId DuplicateReviewId = new EventId(3001, "Duplicate Review");
        private static readonly EventId UnexpectedErrorId = new EventId(3002, "Unexpected Error");
        private static readonly EventId DatabaseUpdatedId = new EventId(3000, "Database Updated");

        internal class Configuration
        {
            public string? ConnectionString { get; set; }
        }

        internal interface ISaveReviews
        {
            public void Save(Review review);
            public void SaveMany(IEnumerable<Review> reviews);
        }

        internal class Inserter : ISaveReviews
        {
            private readonly ILiteCollection<Review> db;
            private readonly ILogger<Inserter> logger;

            public Inserter(ILogger<Inserter> logger, Configuration config)
            {
                var conn = new LiteDatabase(config.ConnectionString);
                this.db = OpenCollection(conn);
                this.logger = logger;
            }

            public void Save(Review review)
                => db.Insert(review);

            public void SaveMany(IEnumerable<Review> reviews)
            {
                int numNew = 0, numDup = 0, numError = 0;

                foreach (var review in reviews)
                {
                    try
                    {
                        Save(review);
                        numNew++;
                    }
                    catch (LiteException e) when (e.ErrorCode == LiteException.INDEX_DUPLICATE_KEY)
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

            private static ILiteCollection<Review> OpenCollection(LiteDatabase db)
            {
                var reviews = db.GetCollection<Review>("reviews");

                reviews.EnsureIndex("$.Author + $.Bottle + $.Link", true);

                return reviews;
            }
        }
    }
}
