#nullable disable
using System.Text.RegularExpressions;
using CSFormatter.Core.Comments.Options;

namespace CSFormatter.Core.Comments;

/// <summary>Ported verbatim from CodeMaid's Model/Comments/CodeCommentMatch.cs.</summary>
internal sealed class CodeCommentMatch
{
    public CodeCommentMatch(Match match, FormatterOptions formatterOptions)
    {
        if (!match.Success)
        {
            throw new ArgumentException("Cannot parse a line that does not match the comment regex.", nameof(match));
        }

        if (formatterOptions.IgnoreTokens.Any(p => match.Value.StartsWith(p)))
        {
            Words = [match.Groups["line"].Value];
            IsLiteral = true;
            IsEmpty = false;
            IsList = false;
        }
        else
        {
            Indent = match.Groups["indent"].Success ? match.Groups["indent"].Value.Length : 0;
            ListPrefix = match.Groups["listprefix"].Success ? match.Groups["listprefix"].Value : null;
            Words = match.Groups["words"].Success ? match.Groups["words"].Captures.OfType<Capture>().Select(c => c.Value).ToList() : null;

            IsLiteral = false;
            IsEmpty = string.IsNullOrWhiteSpace(match.Value) || Words == null || Words.Count < 1;
            IsList = !string.IsNullOrWhiteSpace(ListPrefix);
        }

        // A list prefix with no content (e.g. a lone hyphen) is treated as regular content.
        if (IsEmpty && IsList)
        {
            Words = [ListPrefix];
            ListPrefix = null;
            IsEmpty = false;
            IsList = false;
        }
    }

    public int Indent { get; }

    public bool IsEmpty { get; private set; }

    public bool IsList { get; }

    public bool IsLiteral { get; }

    public int Length =>
        Indent - 1 +
        Words.Sum(w => w.Length + 1) +
        (IsList ? ListPrefix.Length + 1 : 0);

    public string ListPrefix { get; }

    public List<string> Words { get; }

    /// <summary>
    /// Appends the words of <paramref name="other"/> to this match when the two can share a line.
    /// </summary>
    public bool TryAppend(CodeCommentMatch other)
    {
        if (other == null)
            return false;

        if (IsEmpty || other.IsEmpty)
            return false;

        if (other.IsList)
            return false;

        if (IsList && other.Indent < 1)
            return false;

        if (IsLiteral || other.IsLiteral)
            return false;

        Words.AddRange(other.Words);
        IsEmpty = Words.Count < 1;

        return true;
    }
}
