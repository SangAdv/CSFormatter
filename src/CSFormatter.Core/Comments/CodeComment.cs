#nullable disable
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using CSFormatter.Core.Comments.Options;

namespace CSFormatter.Core.Comments;

/// <summary>
/// One block of <c>///</c> comment lines, expanded from a starting line.
/// </summary>
/// <remarks>
/// Rewritten from CodeMaid's Model/Comments/CodeComment.cs. The original walked an EnvDTE
/// <c>EditPoint</c> up and down the document; this walks a string array by index. The expansion
/// rules, the XML detection and the replacement semantics are unchanged.
/// </remarks>
internal sealed class CodeComment
{
    private readonly string[] _lines;
    private readonly FormatterOptions _formatterOptions;
    private readonly Regex _commentLineRegex;

    public CodeComment(string[] lines, int line, FormatterOptions formatterOptions)
    {
        _lines = lines;
        _formatterOptions = formatterOptions;
        _commentLineRegex = CodeCommentHelper.GetCommentRegex(includePrefix: true);

        var start = Expand(line, -1);
        var end = Expand(line, 1);

        if (start >= 0 && end >= start)
        {
            StartLine = start;
            EndLine = end;
            IsValid = true;
        }
    }

    /// <summary>Zero based index of the first line of the comment.</summary>
    public int StartLine { get; } = -1;

    /// <summary>Zero based index of the last line of the comment.</summary>
    public int EndLine { get; } = -1;

    public bool IsValid { get; }

    /// <summary>
    /// Returns the formatted comment lines, or <c>null</c> when the comment is already formatted.
    /// </summary>
    public string[] Format()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Cannot format comment, the comment is not valid.");
        }

        var originalText = string.Join("\n", _lines[StartLine..(EndLine + 1)]);
        var matches = _commentLineRegex.Matches(originalText).OfType<Match>().ToArray();
        if (matches.Length == 0)
        {
            return null;
        }

        var commentOptions = new CommentOptions
        {
            Prefix = matches[0].Groups["prefix"].Value ?? string.Empty,
            Regex = CodeCommentHelper.GetCommentRegex(includePrefix: false),
        };

        // Concatenate the comment lines without prefixes and see if the result parses as XML.
        ICommentLine line = null;
        var commentText = string.Join("\n", matches.Select(m => m.Groups["line"].Value));
        if (commentText.Contains('<'))
        {
            try
            {
                var xml = XElement.Parse($"<doc>{commentText}</doc>");
                line = new CommentLineXml(xml, _formatterOptions);
            }
            catch (XmlException)
            {
                // Not XML, so the comment is handled as plain text.
            }
        }

        line ??= new CommentLine(commentText);

        var formatter = new CommentFormatter(line, _formatterOptions, commentOptions);

        return formatter.Equals(originalText) ? null : formatter.ToString().Split('\n');
    }

    /// <summary>
    /// Walks from <paramref name="line"/> in the given direction while the lines form one comment.
    /// Returns the boundary line, or -1 when the block is not a formattable comment.
    /// </summary>
    private int Expand(int line, int direction)
    {
        var result = -1;
        string prefix = null;

        for (var current = line; current >= 0 && current < _lines.Length; current += direction)
        {
            var match = _commentLineRegex.Match(_lines[current]);
            if (!match.Success)
            {
                break;
            }

            // A different prefix ends the block. This takes priority over the spacer check so
            // comments adjacent to commented out code are still handled.
            var currentPrefix = match.Groups["prefix"].Value.TrimStart();
            if (prefix != null && !string.Equals(prefix, currentPrefix))
            {
                break;
            }

            prefix = currentPrefix;

            // The initial spacer is required, otherwise this is assumed to be commented out code.
            if (!match.Groups["initialspacer"].Success)
            {
                return -1;
            }

            result = current;
        }

        return result;
    }
}
