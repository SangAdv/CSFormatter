using System.Text.RegularExpressions;

namespace CSFormatter.Core.Comments;

/// <summary>
/// Ported from CodeMaid (CodeMaidShared/Helpers/CodeCommentHelper.cs), C# only, no EnvDTE.
/// </summary>
internal static class CodeCommentHelper
{
    public const int CopyrightExtraIndent = 4;

    public const char KeepTogetherSpacer = '\a';

    public const char Spacer = ' ';

    private const string CSharpCommentPrefix = "///?";

    public static string FakeToSpace(string value) => value.Replace(KeepTogetherSpacer, Spacer);

    public static string SpaceToFake(string value) => value.Replace(Spacer, KeepTogetherSpacer);

    /// <summary>
    /// Matches a complete comment line. When <paramref name="includePrefix"/> is set the match
    /// captures the comment prefix (with its indentation) and requires an initial spacer, which is
    /// what tells a real comment apart from commented out code.
    /// </summary>
    public static Regex GetCommentRegex(bool includePrefix)
    {
        var prefix = includePrefix
            ? $@"(?<prefix>[\t ]*{CSharpCommentPrefix})(?<initialspacer>( |\t|\r|\n|$))?"
            : string.Empty;

        var pattern =
            $@"^{prefix}(?<indent>[\t ]*)(?<line>(?<listprefix>[-=\*\+]+[ \t]*|\w+[\):][ \t]+|\d+\.[ \t]+)?((?<words>[^\t\r\n ]+)*[\t ]*)*)\r*\n?$";

        return new Regex(pattern, RegexOptions.ExplicitCapture | RegexOptions.Multiline);
    }
}
