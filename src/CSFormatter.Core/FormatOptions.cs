namespace CSFormatter.Core;

/// <summary>
/// Options for a formatting run. Defaults match CodeMaid's shipped defaults, except for the
/// end-of-file newline (VS Code convention is to keep one).
/// </summary>
public sealed class FormatOptions
{
    public int IndentSize { get; set; } = 4;

    public bool UseTabs { get; set; }

    /// <summary>The newline to write: "\n", "\r\n", or "auto" to keep the document's dominant style.</summary>
    public string NewLine { get; set; } = "auto";

    public bool FormatCode { get; set; } = true;

    public bool SortUsings { get; set; } = true;

    /// <summary>Put every item of an object, collection or array initializer on one line.</summary>
    public bool SingleLineInitializers { get; set; } = true;

    /// <summary>Put the parameter list of a primary or normal constructor on one line.</summary>
    public bool SingleLineConstructorParameters { get; set; } = true;

    public bool FormatComments { get; set; } = true;

    public bool PadMembers { get; set; } = true;

    public bool PadBeforeCaseStatements { get; set; } = true;

    public bool PadBeforeSingleLineComments { get; set; }

    public bool RemoveTrailingWhitespace { get; set; } = true;

    public bool CollapseBlankLines { get; set; } = true;

    public bool TrimBlankLinesAtTopAndBottom { get; set; } = true;

    public bool RemoveBlankLineAfterBrace { get; set; } = true;

    public bool RemoveBlankLineBeforeBrace { get; set; } = true;

    public bool RemoveBlankLineAfterAttributes { get; set; } = true;

    public bool RemoveBlankLineBetweenChainedStatements { get; set; } = true;

    public bool InsertFinalNewline { get; set; } = true;

    public int CommentWrapColumn { get; set; } = 100;

    public bool CommentSkipWrapOnLastWord { get; set; } = true;

    public bool CommentXmlAlignParamTags { get; set; }

    public bool CommentXmlTagsToLowerCase { get; set; }

    public bool CommentXmlSplitSummaryTag { get; set; } = true;

    public int CommentXmlValueIndent { get; set; }

    public bool CommentXmlKeepTagsTogether { get; set; }

    public bool CommentXmlSpaceTags { get; set; }

    public bool CommentXmlSpaceSingleTags { get; set; }

    public bool CommentXmlSplitAllTags { get; set; }

    /// <summary>Comments whose first word starts with one of these are left untouched.</summary>
    public string[] CommentIgnoreTokens { get; set; } = ["TODO: ", "HACK: ", "UNDONE: "];
}
