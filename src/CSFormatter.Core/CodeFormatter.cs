using System.Text.RegularExpressions;
using CSFormatter.Core.Cleaning;
using CSFormatter.Core.Comments;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace CSFormatter.Core;

/// <summary>
/// Formats one C# document.
/// </summary>
/// <remarks>
/// CodeMaid delegated indentation to Visual Studio's SmartFormat and usings to a VS command; both
/// are done here with Roslyn. The whitespace and comment rules are CodeMaid's, ported as-is.
/// </remarks>
public static class CodeFormatter
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    /// <summary>Formats <paramref name="text"/> and returns the result.</summary>
    public static string Format(string text, FormatOptions? options = null)
    {
        options ??= new FormatOptions();

        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var newLine = ResolveNewLine(text, options.NewLine);
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        // A file with syntax errors is left structurally alone; the text rules still run.
        var tree = CSharpSyntaxTree.ParseText(text, ParseOptions);
        var parsedOk = !tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);

        if (parsedOk)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            if (options.SortUsings)
            {
                root = UsingSorter.Sort(root);
            }

            if (options.FormatCode)
            {
                root = FormatCode(root, options);
            }

            text = root.ToFullString();
        }

        text = WhitespaceCleaner.Apply(text, options);

        if (parsedOk && (options.SingleLineInitializers || options.SingleLineConstructorParameters))
        {
            text = ListFlattener.Flatten(text, ParseOptions, options);
        }

        // Re-parse: the whitespace pass moved lines, so the earlier tree's spans are stale.
        var lines = text.Split('\n');
        if (options.PadMembers)
        {
            var current = CSharpSyntaxTree.ParseText(string.Join("\n", lines), ParseOptions).GetRoot();
            lines = BlankLinePadder.Pad(lines, current, options);
        }

        if (options.FormatComments)
        {
            lines = CommentCleaner.Format(lines, options);
        }

        return Join(lines, options, newLine);
    }

    private static CompilationUnitSyntax FormatCode(CompilationUnitSyntax root, FormatOptions options)
    {
        using var workspace = new AdhocWorkspace();

        var formatted = Formatter.Format(
            root,
            workspace,
            workspace.Options
                .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, options.IndentSize)
                .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, options.UseTabs)
                .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\n"));

        return (CompilationUnitSyntax)formatted;
    }

    private static string Join(string[] lines, FormatOptions options, string newLine)
    {
        var body = new List<string>(lines);

        // Drop the empty entry that represents the document's final newline.
        if (body.Count > 0 && body[^1].Length == 0)
        {
            body.RemoveAt(body.Count - 1);
        }

        if (options.TrimBlankLinesAtTopAndBottom)
        {
            body = TrimBlankLines(body);
        }

        var result = string.Join("\n", body);
        if (options.InsertFinalNewline && result.Length > 0)
        {
            result += "\n";
        }

        return newLine == "\n" ? result : result.Replace("\n", newLine);
    }

    private static List<string> TrimBlankLines(List<string> lines)
    {
        var start = 0;
        while (start < lines.Count && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }

        var end = lines.Count - 1;
        while (end >= start && string.IsNullOrWhiteSpace(lines[end]))
        {
            end--;
        }

        return end < start ? [] : lines.GetRange(start, end - start + 1);
    }

    private static string ResolveNewLine(string text, string requested)
    {
        if (requested is "\n" or "\r\n")
        {
            return requested;
        }

        var crlf = Regex.Matches(text, "\r\n").Count;
        var lf = text.Count(c => c == '\n') - crlf;
        return crlf > lf ? "\r\n" : "\n";
    }
}
