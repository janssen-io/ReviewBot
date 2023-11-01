using JanssenIo.ReviewBot.ArchiveParser;
using JanssenIo.ReviewBot.CommandHandlers;
using LiteDB;
using System.Linq;
using Xunit;

namespace JanssenIo.ReviewBot.Tests
{
    public class MostRecentBySubredditTests : LatestListingTests<RecentBySubredditHandler>
    {
        protected override string Command => "/u/review_bot /r/scotch";

        [Fact]
        public void FiltersBySubreddit()
        {
            // Arrange
            var myReview = new Review 
            {
                Author = "Author 1",
                Bottle = "Talisker",
                Link = "https://reddit.com/r/scotch" 
            };

            var otherReview = new Review 
            {
                Author = "Author 1",
                Bottle = "Four Roses",
                Link = "https://reddit.com/r/bourbon" 
            };

            this.database.Add(myReview);
            this.database.Add(otherReview);

            // Act
            var replies = this.sut.ReplyTo(myReview.Author, Command);

            // Assert
            Assert.Contains(myReview.Bottle, replies.Single());
            Assert.DoesNotContain(otherReview.Bottle, replies.Single());
        }

        protected override RecentBySubredditHandler CreateSut()
        {
            return new RecentBySubredditHandler(this.reviews);
        }
    }
}
