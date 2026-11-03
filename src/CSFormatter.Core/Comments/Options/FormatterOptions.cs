namespace CSFormatter.Core.Comments.Options;

/// <summary>Document wide options for the comment formatter.</summary>
public sealed class FormatterOptions
{
    /// <summary>Comments whose first word starts with one of these tokens are left alone.</summary>
    public string[] IgnoreTokens { get; set; } = [];

    /// <summary>Do not wrap to a newline for only a single word.</summary>
    public bool SkipWrapOnLastWord { get; set; }

    public int TabSize { get; set; } = 4;

    public int WrapColumn { get; set; } = 100;

    public FormatterOptionsXml Xml { get; set; } = new();

    public static FormatterOptions FromOptions(FormatOptions options)
    {
        return new FormatterOptions
        {
            IgnoreTokens = options.CommentIgnoreTokens ?? [],
            SkipWrapOnLastWord = options.CommentSkipWrapOnLastWord,
            TabSize = options.IndentSize,
            WrapColumn = options.CommentWrapColumn,
            Xml = FormatterOptionsXml.FromOptions(options),
        };
    }
}
