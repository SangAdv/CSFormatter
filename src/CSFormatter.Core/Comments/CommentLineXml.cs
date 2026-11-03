#nullable disable
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using CSFormatter.Core.Comments.Options;

namespace CSFormatter.Core.Comments;

/// <summary>Ported verbatim from CodeMaid's Model/Comments/CommentLineXml.cs.</summary>
internal sealed class CommentLineXml : CommentLine
{
    private static readonly Regex InterpunctionRegex = new(@"^[^\w]", RegexOptions.Compiled);

    private readonly FormatterOptions _formatterOptions;
    private readonly StringBuilder _innerText;

    public CommentLineXml(XElement xml, FormatterOptions formatterOptions)
        : base(null)
    {
        _formatterOptions = formatterOptions;
        _innerText = new StringBuilder();

        TagName = xml.Name.LocalName;
        TagOptions = _formatterOptions.Xml.GetTagOptions(TagName);

        OpenTag = CreateXmlOpenTag(xml, TagOptions);
        CloseTag = CreateXmlCloseTag(xml, TagOptions);
        IsSelfClosing = CloseTag == null;

        Lines = [];
        ParseChildNodes(xml);
        CloseInnerText(true);
        IsLast = xml.NextNode == null;
    }

    public string CloseTag { get; }

    public bool IsSelfClosing { get; }

    public ICollection<ICommentLine> Lines { get; }

    public string OpenTag { get; internal set; }

    public string TagName { get; }

    public IXmlTagOptions TagOptions { get; }

    private static string CreateXmlCloseTag(XElement element, IXmlTagOptions options)
    {
        if (element.IsEmpty)
        {
            return null;
        }

        return $"</{TagCase(element.Name.LocalName, options.Case)}>";
    }

    private static string CreateXmlOpenTag(XElement element, IXmlTagOptions options)
    {
        var builder = new StringBuilder();
        var name = element.Name.LocalName;

        builder.Append('<');
        builder.Append(TagCase(name, options.Case));

        if (element.HasAttributes)
        {
            foreach (var attr in element.Attributes())
            {
                builder.Append(CodeCommentHelper.Spacer);
                builder.Append(attr);
            }
        }

        if (element.IsEmpty)
        {
            if (options.SpaceSelfClosing)
            {
                builder.Append(CodeCommentHelper.Spacer);
            }

            builder.Append('/');
        }

        builder.Append('>');

        var result = builder.ToString();

        return options.KeepTogether ? CodeCommentHelper.SpaceToFake(result) : result;
    }

    private static bool StartsWithInterpunction(string value) => InterpunctionRegex.IsMatch(value);

    private static string TagCase(string tag, XmlTagCase tagCase) => tagCase switch
    {
        XmlTagCase.LowerCase => tag.ToLowerInvariant(),
        XmlTagCase.UpperCase => tag.ToUpperInvariant(),
        _ => tag,
    };

    /// <summary>Flushes buffered text as a comment line.</summary>
    private void CloseInnerText(bool isLast)
    {
        if (_innerText.Length > 0)
        {
            Lines.Add(new CommentLine(_innerText.ToString()) { IsLast = isLast });
            _innerText.Clear();
        }
    }

    private bool NeedsXmlHandling(XElement e)
    {
        // All root level elements are always on their own line.
        if (e.Parent == null || e.Parent.Parent == null)
            return true;

        // Some special tags should also always be their own line.
        return _formatterOptions.Xml.Tags.ContainsKey(e.Name.LocalName);
    }

    private void ParseChildNodes(XElement xml)
    {
        if (TagOptions.Literal)
        {
            // Read content literally and preserve all formatting.
            using var reader = xml.CreateReader();
            reader.MoveToContent();
            Content = reader.ReadInnerXml();
            return;
        }

        var node = xml.FirstNode;
        while (node != null)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                var element = (XElement)node;

                if (NeedsXmlHandling(element))
                {
                    CloseInnerText(false);
                    Lines.Add(new CommentLineXml(element, _formatterOptions));
                }
                else
                {
                    // Not forced onto its own line: append as string to the current content.
                    _innerText.Append(CreateXmlOpenTag(element, TagOptions));

                    if (!element.IsEmpty)
                    {
                        if (TagOptions.SpaceContent)
                        {
                            _innerText.Append(CodeCommentHelper.Spacer);
                        }

                        ParseChildNodes(element);

                        _innerText.Append(CreateXmlCloseTag(element, TagOptions));
                    }
                }
            }
            else
            {
                // Always trim trailing.
                var value = node.ToString().TrimEnd(CodeCommentHelper.Spacer);

                // If the parent is an element, trim the starting spaces.
                if (node.PreviousNode == null && node.Parent.NodeType == XmlNodeType.Element && !TagOptions.SpaceContent)
                {
                    value = value.TrimStart(CodeCommentHelper.Spacer);
                }

                // After an XML element, put a space before the text unless it starts with punctuation.
                if (node.PreviousNode != null && node.PreviousNode.NodeType == XmlNodeType.Element)
                {
                    if (!StartsWithInterpunction(value))
                    {
                        _innerText.Append(CodeCommentHelper.Spacer);
                    }
                }

                _innerText.Append(value);

                // Add spacing after (almost) each word.
                if (node.NextNode != null || node.Parent.NodeType != XmlNodeType.Element || TagOptions.SpaceContent)
                {
                    _innerText.Append(CodeCommentHelper.Spacer);
                }
            }

            node = node.NextNode;
        }
    }
}
