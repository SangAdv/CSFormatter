using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSFormatter.Core.Cleaning;

/// <summary>
/// Sorts top level <c>using</c> directives the way Visual Studio's "Remove and Sort Usings" does:
/// System first, then everything else alphabetically, with aliases and static usings last.
/// </summary>
internal static class UsingSorter
{
    public static CompilationUnitSyntax Sort(CompilationUnitSyntax root)
    {
        var usings = root.Usings;
        if (usings.Count < 2)
        {
            return root;
        }

        // Reordering across a #if boundary would move a using out of its condition.
        if (usings.Any(u => u.ContainsDirectives || u.GetLeadingTrivia().Any(t => t.IsDirective)))
        {
            return root;
        }

        var ordered = usings
            .OrderBy(SystemGroup)
            .ThenBy(KindGroup)
            .ThenBy(u => u.Name?.ToString() ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        // Trivia follows the position, not the directive, so indentation and line endings stay put.
        var rebuilt = ordered
            .Select((directive, i) => directive
                .WithLeadingTrivia(usings[i].GetLeadingTrivia())
                .WithTrailingTrivia(usings[i].GetTrailingTrivia()))
            .ToList();

        return root.WithUsings(SyntaxFactory.List(rebuilt));
    }

    private static int SystemGroup(UsingDirectiveSyntax directive)
    {
        var name = directive.Name?.ToString() ?? string.Empty;
        return name == "System" || name.StartsWith("System.", StringComparison.Ordinal) ? 0 : 1;
    }

    private static int KindGroup(UsingDirectiveSyntax directive)
    {
        if (directive.Alias != null)
        {
            return 1;
        }

        return directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) ? 2 : 0;
    }
}
