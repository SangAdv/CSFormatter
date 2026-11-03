namespace CSFormatter.Core.Comments.Options;

public enum XmlTagCase
{
    Default = 0,
    Keep,
    LowerCase,
    UpperCase,
}

[Flags]
public enum XmlTagNewLine
{
    Default = 0,

    /// <summary>Put tags on their own line when content is too long to fit on a single line.</summary>
    Content = 1 << 0,

    BeforeOpen = 1 << 1,
    AfterOpen = 1 << 2,
    BeforeClose = 1 << 3,
    AfterClose = 1 << 4,

    /// <summary>Break before the open tag and after the close tag, regardless of content length.</summary>
    BeforeAndAfter = BeforeOpen | AfterClose,

    /// <summary>Put tags on their own lines.</summary>
    Always = BeforeOpen | AfterOpen | BeforeClose | AfterClose,
}
