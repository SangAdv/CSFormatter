using CSFormatter.Core;

namespace CSFormatter.Tests;

public class CodeFormatterTests
{
    private static string Format(string text, Action<FormatOptions> configure = null)
    {
        var options = new FormatOptions { NewLine = "\n" };
        configure?.Invoke(options);
        return CodeFormatter.Format(text, options);
    }

    [Fact]
    public void SortsUsingsSystemFirst()
    {
        const string input = """
            using System.Text;
            using Zeta;
            using System.Linq;
            using Alpha;

            namespace N;

            class C { }
            """;

        var result = Format(input, o => o.FormatComments = false);

        var usings = result.Split('\n').Where(l => l.StartsWith("using ")).ToArray();
        Assert.Equal(["using System.Linq;", "using System.Text;", "using Alpha;", "using Zeta;"], usings);
    }

    [Fact]
    public void PadsBeforeMethodsButKeepsFieldsGrouped()
    {
        const string input = """
            namespace N;

            class C
            {
                private int _a;
                private int _b;
                public void M() { }
            }
            """;

        var result = Format(input, o => o.FormatComments = false);

        Assert.Contains("private int _b;\n\n    public void M()", result);
        Assert.Contains("private int _a;\n    private int _b;", result);
    }

    [Fact]
    public void DoesNotPadAfterOpeningBrace()
    {
        const string input = """
            namespace N;

            class C
            {
                public void M() { }
            }
            """;

        var result = Format(input, o => o.FormatComments = false);

        Assert.Contains("{\n    public void M()", result);
    }

    [Fact]
    public void CollapsesBlankLinesAndTrimsTrailingWhitespace()
    {
        const string input = "class C\n{\n    void M() { }   \n\n\n\n    void N() { }\n}\n\n\n";

        var result = Format(input, o =>
        {
            o.FormatCode = false;
            o.FormatComments = false;
        });

        Assert.DoesNotContain("   \n", result);
        Assert.DoesNotContain("\n\n\n", result);
        Assert.EndsWith("}\n", result);
    }

    [Fact]
    public void WrapsLongSummaryToTheConfiguredColumn()
    {
        const string input = """
            namespace N;

            class C
            {
                /// <summary>This is a very long summary line that should be wrapped because it runs past the configured column by a good margin.</summary>
                public void M() { }
            }
            """;

        var result = Format(input, o =>
        {
            o.CommentWrapColumn = 80;
            o.PadMembers = false;
        });

        var commentLines = result.Split('\n').Where(l => l.Contains("///")).ToArray();
        Assert.True(commentLines.Length > 2, "expected the summary to span multiple lines");
        Assert.All(commentLines, l => Assert.True(l.Length <= 80, $"line too long: {l}"));
        Assert.Contains("</summary>", result);
    }

    [Fact]
    public void LeavesCommentedOutCodeAlone()
    {
        const string input = """
            namespace N;

            class C
            {
                //var x = 1;
                public void M() { }
            }
            """;

        var result = Format(input, o => o.PadMembers = false);

        Assert.Contains("//var x = 1;", result);
    }

    [Fact]
    public void RespectsTodoTokens()
    {
        const string input = """
            namespace N;

            class C
            {
                /// TODO:    do   not   touch     this
                public void M() { }
            }
            """;

        var result = Format(input, o => o.PadMembers = false);

        Assert.Contains("/// TODO:    do   not   touch     this", result);
    }

    [Fact]
    public void PutsInitializerItemsOnOneLine()
    {
        const string input = """
            namespace N;

            class C
            {
                public void M()
                {
                    var x = new Foo
                    {
                        A = 1,
                        B = new Bar
                        {
                            C = 2,
                        },
                    };
                }
            }
            """;

        var result = Format(input, o =>
        {
            o.FormatComments = false;
            o.PadMembers = false;
        });

        Assert.Contains("var x = new Foo { A = 1, B = new Bar { C = 2 } };", result);
    }

    [Fact]
    public void PutsCollectionInitializerItemsOnOneLine()
    {
        const string input = """
            namespace N;

            class C
            {
                private readonly List<int> _l = new List<int>
                {
                    1,
                    2,
                };
            }
            """;

        var result = Format(input, o => o.FormatComments = false);

        Assert.Contains("new List<int> { 1, 2 };", result);
    }

    [Fact]
    public void PutsPrimaryConstructorParametersOnOneLine()
    {
        const string input = """
            namespace N;

            public sealed class C(
                int a,
                string b)
                : Base(a)
            {
            }

            public struct S(
                int a,
                string b)
            {
            }
            """;

        var result = Format(input, o => o.FormatComments = false);

        Assert.Contains("public sealed class C(int a, string b)", result);
        Assert.Contains("public struct S(int a, string b)", result);
    }

    [Fact]
    public void PutsConstructorParametersOnOneLine()
    {
        const string input = """
            namespace N;

            class C
            {
                public C(
                    int a,
                    string b)
                    : base()
                {
                }
            }
            """;

        var result = Format(input, o =>
        {
            o.FormatComments = false;
            o.PadMembers = false;
        });

        Assert.Contains("public C(int a, string b)", result);
    }

    [Fact]
    public void LeavesMethodParametersAndCommentedListsAlone()
    {
        const string input = """
            namespace N;

            class C
            {
                public void M(
                    int a,
                    int b)
                {
                    var x = new Foo(
                        a, // keep
                        b);
                }
            }
            """;

        var result = Format(input, o => o.FormatComments = false);

        Assert.Contains("public void M(\n        int a,\n        int b)", result);
        Assert.Contains("a, // keep\n", result);
    }

    [Fact]
    public void LeavesInitializersWithCommentsAlone()
    {
        const string input = """
            namespace N;

            class C
            {
                public void M()
                {
                    var x = new Foo
                    {
                        A = 1, // keep
                        B = 2,
                    };
                }
            }
            """;

        var result = Format(input, o => o.PadMembers = false);

        Assert.Contains("A = 1, // keep\n            B = 2,", result);
    }

    [Fact]
    public void LeavesMultiLineLiteralInsideInitializerAlone()
    {
        const string input = "namespace N;\n\nclass C\n{\n    private readonly Foo _f = new Foo\n    {\n        Text = \"\"\"\n            a\n\n            b\n            \"\"\",\n    };\n}\n";

        var result = Format(input, o => o.FormatComments = false);

        Assert.Contains("\"\"\"\n            a\n\n            b\n            \"\"\"", result);
    }

    [Fact]
    public void IsIdempotent()
    {
        const string input = """
            using System.Text;
            using System.Linq;

            namespace N
            {
                /// <summary>Short.</summary>
                /// <param name="a">The a.</param>
                /// <param name="longParameterName">The long parameter.</param>
                class C
                {
                    public C(
                        int a,
                        string b)
                    {
                    }

                    private int _a;
                    private readonly Bar _b = new Bar
                    {
                        Value = 1,
                    };
                    public void M(int a, int longParameterName)
                    {
                        if (a > 0) { }
                        else { }
                    }
                }
            }
            """;

        var once = Format(input, o => o.CommentXmlAlignParamTags = true);
        var twice = Format(once, o => o.CommentXmlAlignParamTags = true);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void DoesNotThrowOnBrokenSyntax()
    {
        const string input = "class C { void M( { }\n";

        var result = Format(input);

        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void NormalizesToCrlfWhenRequested()
    {
        var result = Format("class C { }\n", o => o.NewLine = "\r\n");

        Assert.Contains("\r\n", result);
        Assert.DoesNotContain("\n\n", result.Replace("\r\n", "\n"));
    }
}
