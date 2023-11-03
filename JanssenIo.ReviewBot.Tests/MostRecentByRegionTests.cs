using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.CommandHandlers;
using LiteDB;
using System.Linq;
using Xunit;

namespace JanssenIo.ReviewBot.Tests
{
    public class MostRecentByRegionTests : LatestListingTests<RecentByRegionHandler>
    {
        protected override string Command => "/u/review_bot region 'Islay'";

        [Fact]
        public void FiltersByRegion()
        {
            // Arrange
            var myReview = new Review 
            {
                Author = "Author 1",
                Bottle = "Ardbeg 10",
                Region = "Islay",
            };

            var otherReview = new Review 
            {
                Author = "Author 1",
                Bottle = "Ardbeg 20",
                Region = "Highlands",
            };

            this.database.Add(myReview);
            this.database.Add(otherReview);

            // Act
            var replies = this.sut.ReplyTo(myReview.Author, Command);

            // Assert
            Assert.Contains(myReview.Bottle, replies.Single());
            Assert.DoesNotContain(otherReview.Bottle, replies.Single());
        }

        protected override RecentByRegionHandler CreateSut()
        {
            return new RecentByRegionHandler(this.reviews);
        }
    }
}
