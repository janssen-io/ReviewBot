using System.Collections.Generic;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public interface IReplyToCommands
    {
        IEnumerable<string> ReplyTo(string author, string body);
    }
}
