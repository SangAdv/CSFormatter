namespace CSFormatter.Core.Comments.Options;

public interface IXmlTagOptions
{
    XmlTagCase Case { get; }

    int Indent { get; }

    bool KeepTogether { get; }

    bool Literal { get; }

    bool SpaceContent { get; }

    bool SpaceSelfClosing { get; }

    XmlTagNewLine Split { get; }
}

/// <summary>Per tag override; unset values fall back to the document wide default.</summary>
public sealed class FormatterOptionsXmlTag
{
    public XmlTagCase Case { get; set; }

    public int? Indent { get; set; }

    public bool? KeepTogether { get; set; }

    public bool? Literal { get; set; }

    public bool? SpaceContent { get; set; }

    public bool? SpaceSelfClosing { get; set; }

    public XmlTagNewLine Split { get; set; }
}

public sealed class XmlTagOptions : IXmlTagOptions
{
    public XmlTagOptions()
    {
    }

    public XmlTagOptions(FormatterOptionsXmlTag tag, IXmlTagOptions fallback)
    {
        Case = tag.Case != XmlTagCase.Default ? tag.Case : fallback.Case != XmlTagCase.Default ? fallback.Case : XmlTagCase.Keep;
        Indent = tag.Indent ?? fallback.Indent;
        KeepTogether = tag.KeepTogether ?? fallback.KeepTogether;
        Literal = tag.Literal ?? false;
        SpaceContent = tag.SpaceContent ?? fallback.SpaceContent;
        SpaceSelfClosing = tag.SpaceSelfClosing ?? fallback.SpaceSelfClosing;
        Split = tag.Split != XmlTagNewLine.Default ? tag.Split : fallback.Split != XmlTagNewLine.Default ? fallback.Split : XmlTagNewLine.Content;
    }

    public XmlTagCase Case { get; set; }

    public int Indent { get; set; }

    public bool KeepTogether { get; set; }

    public bool Literal { get; set; }

    public bool SpaceContent { get; set; }

    public bool SpaceSelfClosing { get; set; }

    public XmlTagNewLine Split { get; set; }
}
