using JanssenIo.ReviewBot.CommandHandlers;
using Xunit;

namespace JanssenIo.ReviewBot.Tests
{
    public class LatestReviewsTests : LatestListingTests<LatestReviewsHandler>
    {
        protected override string Command => "/u/review_bot latest";

        [Fact]
        public void AlwaysReturnsASingleReply()
        {
            // Act
            var replies = this.sut.ReplyTo("Author 1", "/u/review_bot latest ... /u/review_bot latest");

            // Assert
            Assert.Single(replies);
        }

        protected override LatestReviewsHandler CreateSut()
        {
            return new LatestReviewsHandler(this.reviews);
        }
    }
}
