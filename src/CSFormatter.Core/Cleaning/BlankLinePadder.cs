using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSFormatter.Core.Cleaning;

/// <summary>
/// Inserts a blank line before member declarations.
/// </summary>
/// <remarks>
/// Replaces CodeMaid's InsertBlankLinePaddingLogic, which classified members through the EnvDTE
/// CodeModel. This walks the Roslyn syntax tree instead. Only "before" padding is applied: a
/// member's "after" padding is the next member's "before" padding, and the remaining case (end of
/// a block) is covered by the blank-line-before-brace removal. The skip rule is CodeMaid's: never
/// pad when the previous line is a scope boundary.
/// </remarks>
internal static class BlankLinePadder
{
    private static readonly Regex PaddablePreviousLine = new(@"^\s*[^\s\{]", RegexOptions.Multiline);

    public static string[] Pad(string[] lines, SyntaxNode root, FormatOptions options)
    {
        if (!options.PadMembers)
        {
            return lines;
        }

        var insertBefore = new SortedSet<int>();

        foreach (var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            if (!ShouldPadBefore(member))
            {
                continue;
            }

            var startLine = StartLineOf(member);
            if (startLine <= 0 || startLine >= lines.Length)
            {
                continue;
            }

            var previous = lines[startLine - 1];
            if (string.IsNullOrWhiteSpace(previous) || !PaddablePreviousLine.IsMatch(previous))
            {
                continue;
            }

            insertBefore.Add(startLine);
        }

        if (insertBefore.Count == 0)
        {
            return lines;
        }

        var result = new List<string>(lines.Length + insertBefore.Count);
        for (var i = 0; i < lines.Length; i++)
        {
            if (insertBefore.Contains(i))
            {
                result.Add(string.Empty);
            }

            result.Add(lines[i]);
        }

        return result.ToArray();
    }

    private static bool ShouldPadBefore(MemberDeclarationSyntax member) => member switch
    {
        ClassDeclarationSyntax or StructDeclarationSyntax or InterfaceDeclarationSyntax
            or EnumDeclarationSyntax or RecordDeclarationSyntax or DelegateDeclarationSyntax => true,

        MethodDeclarationSyntax or ConstructorDeclarationSyntax or DestructorDeclarationSyntax
            or OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax => true,

        EventDeclarationSyntax or EventFieldDeclarationSyntax => true,

        BaseNamespaceDeclarationSyntax => true,

        PropertyDeclarationSyntax or IndexerDeclarationSyntax => IsMultiLine(member),

        FieldDeclarationSyntax => IsMultiLine(member),

        _ => false,
    };

    private static bool IsMultiLine(MemberDeclarationSyntax member)
    {
        var span = member.GetLocation().GetLineSpan();
        return span.StartLinePosition.Line != span.EndLinePosition.Line;
    }

    /// <summary>
    /// First line of the member, including a documentation comment that precedes it.
    /// </summary>
    private static int StartLineOf(MemberDeclarationSyntax member)
    {
        var firstToken = member.GetFirstToken();
        var line = firstToken.GetLocation().GetLineSpan().StartLinePosition.Line;

        foreach (var trivia in firstToken.LeadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                line = Math.Min(line, trivia.GetLocation().GetLineSpan().StartLinePosition.Line);
            }
        }

        return line;
    }
}
