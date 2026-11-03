using CSFormatter.Core.Comments.Options;

namespace CSFormatter.Core.Comments;

/// <summary>
/// Reflows every <c>///</c> comment block in a document. Mirrors CodeMaid's
/// CommentFormatLogic.FormatComments: comments starting inside the range are formatted even when
/// they overlap the end.
/// </summary>
internal static class CommentCleaner
{
    public static string[] Format(string[] lines, FormatOptions options)
    {
        var formatterOptions = FormatterOptions.FromOptions(options);
        var result = new List<string>(lines.Length);

        for (var i = 0; i < lines.Length;)
        {
            var comment = new CodeComment(lines, i, formatterOptions);

            if (!comment.IsValid)
            {
                result.Add(lines[i]);
                i++;
                continue;
            }

            var formatted = comment.Format();
            if (formatted == null)
            {
                result.AddRange(lines[comment.StartLine..(comment.EndLine + 1)]);
            }
            else
            {
                result.AddRange(formatted);
            }

            i = comment.EndLine + 1;
        }

        return result.ToArray();
    }
}
