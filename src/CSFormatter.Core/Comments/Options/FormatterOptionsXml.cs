using CSFormatter.Core.Comments;

namespace CSFormatter.Core.Comments.Options;

/// <summary>
/// Which XML tags get their own line, and how. Ported from CodeMaid's
/// FormatterOptionsXml.FromSettings, with the same tag table.
/// </summary>
public sealed class FormatterOptionsXml
{
    public FormatterOptionsXml()
    {
        Tags = new Dictionary<string, FormatterOptionsXmlTag>(StringComparer.OrdinalIgnoreCase);
        Default = new XmlTagOptions();
    }

    /// <summary>Whether <c>param</c> tags should all be made the same length.</summary>
    public bool AlignParamTags { get; set; }

    public XmlTagOptions Default { get; set; }

    public Dictionary<string, FormatterOptionsXmlTag> Tags { get; set; }

    public IXmlTagOptions GetTagOptions(string tagName)
    {
        return !Tags.TryGetValue(tagName, out var tag) ? Default : new XmlTagOptions(tag, Default);
    }

    public static FormatterOptionsXml FromOptions(FormatOptions options)
    {
        static FormatterOptionsXmlTag SplitBoth() => new() { Split = XmlTagNewLine.BeforeAndAfter };

        return new FormatterOptionsXml
        {
            AlignParamTags = options.CommentXmlAlignParamTags,
            Default = new XmlTagOptions
            {
                Case = options.CommentXmlTagsToLowerCase ? XmlTagCase.LowerCase : XmlTagCase.Keep,
                Indent = options.CommentXmlValueIndent,
                KeepTogether = options.CommentXmlKeepTagsTogether,
                Literal = false,
                SpaceContent = options.CommentXmlSpaceTags,
                SpaceSelfClosing = options.CommentXmlSpaceSingleTags,
                Split = options.CommentXmlSplitAllTags ? XmlTagNewLine.Always : XmlTagNewLine.Content,
            },
            Tags = new Dictionary<string, FormatterOptionsXmlTag>
            {
                ["summary"] = new() { Split = options.CommentXmlSplitSummaryTag ? XmlTagNewLine.Always : XmlTagNewLine.Default },
                ["copyright"] = new() { Split = XmlTagNewLine.Always, Indent = CodeCommentHelper.CopyrightExtraIndent },
                ["code"] = new() { Split = XmlTagNewLine.BeforeAndAfter, Literal = true },
                ["p"] = SplitBoth(),
                ["para"] = SplitBoth(),
                ["list"] = SplitBoth(),
                ["listheader"] = SplitBoth(),
                ["item"] = SplitBoth(),
                ["term"] = SplitBoth(),
                ["description"] = SplitBoth(),
            },
        };
    }
}
