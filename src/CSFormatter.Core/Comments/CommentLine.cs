#nullable disable
namespace CSFormatter.Core.Comments;

internal interface ICommentLine
{
    string Content { get; }

    bool IsLast { get; }
}

internal class CommentLine : ICommentLine
{
    public CommentLine(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            Content = content;
        }
    }

    public string Content { get; protected set; }

    public bool IsLast { get; internal set; }
}
