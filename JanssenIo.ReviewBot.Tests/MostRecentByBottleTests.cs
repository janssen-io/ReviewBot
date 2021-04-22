using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.CommandHandlers;
using LiteDB;
using System;
using System.Linq;
using Xunit;

namespace JanssenIo.ReviewBot.Tests
{
    public class MostRecentByBottleTests : LatestListingTests<RecentByBottleHandler>
    {
        protected override string Command => "/u/review_bot bottle 'Ardbeg'";

        [Fact]
        public void FiltersByBottle()
        {
            // Arrange
            var myReview = new Review 
            {
                Author = "Author 1",
                Bottle = "Ardbeg",
            };

            var otherReview = new Review 
            {
                Author = "Author 1",
                Bottle = "Four Roses",
            };

            this.reviews.Insert(myReview);
            this.reviews.Insert(otherReview);

            // Act
            var replies = this.sut.ReplyTo(myReview.Author, Command);

            // Assert
            Assert.Contains(myReview.Bottle, replies.Single());
            Assert.DoesNotContain(otherReview.Bottle, replies.Single());
        }

        protected override RecentByBottleHandler CreateSut()
        {
            return new RecentByBottleHandler(this.reviews);
        }
    }
}
