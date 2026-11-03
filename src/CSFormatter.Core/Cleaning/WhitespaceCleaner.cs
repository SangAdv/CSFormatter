using System.Text.RegularExpressions;

namespace CSFormatter.Core.Cleaning;

/// <summary>
/// Ported from CodeMaid's RemoveWhitespaceLogic / InsertWhitespaceLogic. The rules and their
/// defaults are unchanged; the EnvDTE document is replaced by a string that is already normalized
/// to "\n", so the patterns drop their "\r?" prefixes.
/// </summary>
internal static class WhitespaceCleaner
{
    public static string Apply(string text, FormatOptions options)
    {
        // Removal pass, in CodeMaid's order.
        if (options.RemoveTrailingWhitespace)
            text = Sub(text, @"[ \t]+\n", "\n");

        if (options.RemoveBlankLineAfterAttributes)
            text = Sub(text, @"(^[ \t]*\[[^\]]+\][ \t]*(//[^\r\n]*)*)(\n){2}(?![ \t]*//)", "$1\n");

        if (options.RemoveBlankLineAfterBrace)
            text = Sub(text, @"\{([ \t]*(//[^\r\n]*)*)(\n){2,}", "{$1\n");

        if (options.RemoveBlankLineBeforeBrace)
            text = Sub(text, @"(\n){2,}([ \t]*)\}", "\n$2}");

        if (options.RemoveBlankLineBetweenChainedStatements)
            text = Sub(text, @"(\n){2,}([ \t]*)(else|catch|finally)( |\t|\n)", "\n$2$3$4");

        if (options.CollapseBlankLines)
            text = Sub(text, @"(\n){3,}", "\n\n");

        // Insertion pass.
        if (options.PadBeforeCaseStatements)
            text = Sub(text, @"(^[ \t]*)(break;|return([ \t][^;]*)?;)\n([ \t]*)(case|default)", "$1$2\n\n$4$5");

        if (options.PadBeforeSingleLineComments)
            text = Sub(text, @"(^[ \t]*(?!//)[^ \t\r\n\{].*\n)([ \t]*//)(?!//)", "$1\n$2");

        return text;
    }

    private static string Sub(string text, string pattern, string replacement) =>
        Regex.Replace(text, pattern, replacement, RegexOptions.Multiline);
}
