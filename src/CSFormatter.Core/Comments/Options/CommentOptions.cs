#nullable disable
using System.Text.RegularExpressions;

namespace CSFormatter.Core.Comments.Options;

/// <summary>Comment specific options for the formatter.</summary>
internal sealed class CommentOptions
{
    public string Prefix { get; internal set; } = string.Empty;

    public Regex Regex { get; internal set; }
}
