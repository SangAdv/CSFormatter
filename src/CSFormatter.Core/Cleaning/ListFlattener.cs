using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSFormatter.Core.Cleaning;

/// <summary>
/// Moves every item of an object, collection, array or <c>with</c> initializer, and of a class,
/// struct or record constructor parameter list, onto one line.
/// </summary>
/// <remarks>
/// This works on the formatted text and only replaces the whitespace <em>between</em> tokens that
/// spans a line break, so literal contents are never touched. Lists holding a comment, a
/// preprocessor directive or a multi-line literal are skipped: joining them would swallow code,
/// break parsing, or change the literal's value. There is deliberately no width limit.
/// </remarks>
internal static class ListFlattener
{
    public static string Flatten(string text, CSharpParseOptions parseOptions, FormatOptions options)
    {
        var root = CSharpSyntaxTree.ParseText(text, parseOptions).GetRoot();

        var flattenable = root.DescendantNodes()
            .Where(node => IsTarget(node, options))
            .Where(IsMultiLine)
            .Where(CanFlatten)
            .OrderBy(node => node.SpanStart)
            .ToList();

        // An outer list covers its nested ones; keeping both would double-edit the span.
        var targets = new List<SyntaxNode>();
        var coveredUntil = -1;
        foreach (var node in flattenable)
        {
            if (node.SpanStart < coveredUntil)
            {
                continue;
            }

            targets.Add(node);
            coveredUntil = node.Span.End;
        }

        if (targets.Count == 0)
        {
            return text;
        }

        var result = new StringBuilder(text);

        // Right to left, so the spans stay valid.
        foreach (var node in targets.OrderByDescending(node => node.SpanStart))
        {
            // The line break before the opening bracket is trailing trivia of the previous token,
            // so walk back over it instead of replacing the node's full span.
            var start = node.SpanStart;
            while (start > 0 && (text[start - 1] == ' ' || text[start - 1] == '\t' || text[start - 1] == '\n'))
            {
                start--;
            }

            // A space separates an initializer from the expression before it; a parameter list does
            // not take one ("class C(" but "new C {").
            var prefix = start == 0 || node is ParameterListSyntax ? string.Empty : " ";
            result.Remove(start, node.Span.End - start)
                  .Insert(start, prefix + Collapse(text, node));
        }

        return result.ToString();
    }

    private static bool IsTarget(SyntaxNode node, FormatOptions options) => node switch
    {
        InitializerExpressionSyntax => options.SingleLineInitializers,
        ParameterListSyntax list => options.SingleLineConstructorParameters && IsConstructorParameterList(list),
        _ => false,
    };

    /// <summary>
    /// Normal constructors and the primary constructor of a class, struct or record. Methods,
    /// delegates and lambdas keep their parameter list as written.
    /// </summary>
    private static bool IsConstructorParameterList(ParameterListSyntax list) => list.Parent is
        ConstructorDeclarationSyntax or
        ClassDeclarationSyntax or
        StructDeclarationSyntax or
        RecordDeclarationSyntax;

    private static string Collapse(string text, SyntaxNode node)
    {
        var tokens = node.DescendantTokens().ToList();
        var collapsed = new StringBuilder();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (i > 0)
            {
                var previous = tokens[i - 1];
                var gap = text.Substring(previous.Span.End, token.SpanStart - previous.Span.End);

                if (!gap.Contains('\n'))
                {
                    collapsed.Append(gap);
                }
                else if (WantsSpaceBetween(previous, token) && (collapsed.Length == 0 || collapsed[^1] != ' '))
                {
                    collapsed.Append(' ');
                }
            }

            // A trailing comma reads badly on a single line; every element is still separated.
            if (token.IsKind(SyntaxKind.CommaToken) &&
                i + 1 < tokens.Count &&
                tokens[i + 1].IsKind(SyntaxKind.CloseBraceToken))
            {
                continue;
            }

            collapsed.Append(token.Text);
        }

        return collapsed.ToString();
    }

    private static bool WantsSpaceBetween(SyntaxToken previous, SyntaxToken token) =>
        token.Kind() is not (
            SyntaxKind.DotToken or
            SyntaxKind.CommaToken or
            SyntaxKind.SemicolonToken or
            SyntaxKind.CloseParenToken or
            SyntaxKind.CloseBracketToken) &&
        previous.Kind() is not (
            SyntaxKind.OpenParenToken or
            SyntaxKind.OpenBracketToken or
            SyntaxKind.DotToken);

    private static bool IsMultiLine(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        return span.StartLinePosition.Line != span.EndLinePosition.Line;
    }

    private static bool ContainsMultiLineToken(SyntaxNode node) =>
        node.DescendantTokens().Any(token => token.Text.Contains('\n'));

    private static bool CanFlatten(SyntaxNode node)
    {
        if (ContainsMultiLineToken(node) || ContainsCommentOrDirective(node.DescendantTrivia(descendIntoTrivia: true)))
        {
            return false;
        }

        // A comment right before the opening token ends up on the joined line too.
        var first = node.GetFirstToken();
        return !ContainsCommentOrDirective(first.GetPreviousToken().TrailingTrivia) &&
               !ContainsCommentOrDirective(first.LeadingTrivia);
    }

    private static bool ContainsCommentOrDirective(IEnumerable<SyntaxTrivia> trivia) =>
        trivia.Any(t =>
            t.IsDirective ||
            t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            t.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
            t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
}
