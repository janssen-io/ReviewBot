using System.Text;

namespace JanssenIo.ReviewBot.CommandHandlers
{
    public abstract class Reply 
    {
        public abstract StringBuilder AddTo(StringBuilder reply);
    }

    public class Comment : Reply
    {
        public Comment(string text) => Text = text;

        public string Text { get; }

        public override StringBuilder AddTo(StringBuilder reply)
            => reply.AppendLine(Text);
    }
}
