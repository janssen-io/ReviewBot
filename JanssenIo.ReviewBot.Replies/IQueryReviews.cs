using JanssenIo.ReviewBot.ArchiveParser;
using LiteDB;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace JanssenIo.ReviewBot.Replies
{
    public interface IQueryReviews
    {
        Review[] Where(Expression<Func<Review, bool>> filter, string author);
    }

    public class Latest10Query : IQueryReviews
    {
        private readonly IQueryReviews innerQuery;

        public Latest10Query(IQueryReviews innerQuery)
        {
            this.innerQuery = innerQuery;
        }

        public Review[] Where(Expression<Func<Review, bool>> filter, string author)
            => innerQuery.Where(filter, author)
                .OrderByDescending(r => r.PublishedOn)
                .Take(10)
                .ToArray();
    }

    public class LiteDbQueryAdapter : IQueryReviews
    {
        private readonly ILiteCollection<Review> reviews;

        public LiteDbQueryAdapter(ILiteCollection<Review> reviews)
        {
            this.reviews = reviews;
        }

        public Review[] Where(Expression<Func<Review, bool>> filter, string author)
        {
            return reviews.Query().Where(filter).ToArray();
        }
    }
}
