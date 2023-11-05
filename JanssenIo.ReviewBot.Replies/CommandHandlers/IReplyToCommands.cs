using System.Collections.Generic;

namespace JanssenIo.ReviewBot.Replies.CommandHandlers
{
    public interface IReplyToCommands
    {
        IEnumerable<string> ReplyTo(string author, string body);
    }
}
