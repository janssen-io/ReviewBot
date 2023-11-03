using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.Replies;
using JanssenIo.ReviewBot.Replies.CommandHandlers;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JanssenIo.ReviewBot.Tests
{
    public abstract class LatestListingTests<T>
        where T : IReplyToCommands
    {
        protected abstract T CreateSut();
        protected abstract string Command { get; }

        protected readonly IQueryReviews reviews;
        protected readonly T sut;

        protected readonly List<Review> database;

        protected LatestListingTests()
        {
            this.database = new List<Review>();
            this.reviews = new Latest10Query(new MockReviewStore(this.database));
            this.sut = CreateSut();
        }

        [Fact]
        public void ReturnsNothingWithoutCommand()
        {
            // Act
            var replies = this.sut.ReplyTo("Author 1", "no command");

            // Assert
            Assert.Empty(replies);
        }

        [Fact]
        public void FiltersByAuthor()
        {
            // Arrange
            var myReview = new Review
            {
                Author = "Author 1",
                Bottle = "Ardbeg 10",
                Link = "https://reddit.com/r/scotch",
                Region = "Islay",
            };

            var otherReview = new Review
            {
                Author = "Author 2",
                Bottle = "Ardbeg 20",
                Link = "https://reddit.com/r/scotch",
                Region = "Islay",
            };

            this.database.Add(myReview);
            this.database.Add(otherReview);

            // Act
            var replies = this.sut.ReplyTo(myReview.Author, Command);

            // Assert
            string reply = Assert.Single(replies);
            Assert.Contains(myReview.Bottle, reply);
            Assert.DoesNotContain(otherReview.Bottle, reply);
        }

        [Fact]
        public void SortsByDate()
        {
            // Arrange
            var newReviews = new[]
            {
                new Review
                {
                    Author = "Author 1",
                    Bottle = "Ardbeg 10",
                    Link = "https://reddit.com/r/scotch",
                    Region = "Islay",
                    PublishedOn = new DateTime(2010, 1, 1),
                },
                new Review
                {
                    Author = "Author 1",
                    Bottle = "Ardbeg 20",
                    Link = "https://reddit.com/r/scotch",
                    Region = "Islay",
                    PublishedOn = new DateTime(2015, 1, 1),
                }
            };

            this.database.AddRange(newReviews);

            // Act
            var replies = this.sut.ReplyTo(newReviews[0].Author, Command);

            // Assert
            var reply = replies.Single();
            Assert.True(reply.IndexOf(newReviews[0].Bottle) > reply.IndexOf(newReviews[1].Bottle));
        }

        [Fact]
        public void ShowsMaximum10()
        {
            // Arrange
            var newReviews = Enumerable
                .Range(0, 20)
                .Select(i => new Review
                {
                    Author = "Author 1",
                    Bottle = $"Ardbeg {i}",
                    // lowest bottle numbers are most recent
                    PublishedOn = new DateTime(2020, 1, 31 - i),
                    Link = "https://reddit.com/r/scotch",
                    Region = "Islay",
                }).ToArray();

            this.database.AddRange(newReviews);

            // Act
            var replies = this.sut.ReplyTo(newReviews[0].Author, Command);

            // Assert
            var reply = replies.Single();

            var top10 = newReviews.Take(10);
            var oldestReviews = newReviews.Skip(10);

            Assert.All(
                top10,
                review => Assert.Contains(review.Bottle, reply));

            Assert.All(
                oldestReviews,
                review => Assert.DoesNotContain(review.Bottle, reply));
        }

        [Fact]
        public void AlwaysShowsHeader()
        {
            // Act
            var replies = this.sut.ReplyTo("Author 1", Command);

            // Assert
            var reply = replies.Single();
            Assert.Contains("Author 1's latest", reply);
        }
    }
}
