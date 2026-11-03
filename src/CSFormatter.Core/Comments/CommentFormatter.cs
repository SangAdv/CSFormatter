#nullable disable
using System.Text;
using System.Text.RegularExpressions;
using CSFormatter.Core.Comments.Options;

namespace CSFormatter.Core.Comments;

/// <summary>
/// Wraps and formats a comment. Ported from CodeMaid's Model/Comments/CommentFormatter.cs;
/// the only change is that options come from <see cref="FormatOptions"/> instead of the VS
/// settings store.
/// </summary>
internal sealed class CommentFormatter : IEquatable<string>
{
    private readonly StringBuilder _builder;
    private readonly CommentOptions _commentOptions;
    private readonly FormatterOptions _formatterOptions;
    private readonly int _commentPrefixLength;
    private int _currentPosition;
    private int _indentAmount;
    private bool _isFirstWord;
    private bool _isIndented;
    private bool _isPrefixWritten;

    public CommentFormatter(ICommentLine line, FormatterOptions formatterOptions, CommentOptions commentOptions)
    {
        _formatterOptions = formatterOptions;
        _commentOptions = commentOptions;

        _builder = new StringBuilder();
        _currentPosition = 0;
        _isFirstWord = true;
        _isPrefixWritten = false;
        _isIndented = false;
        _indentAmount = 0;
        _commentPrefixLength = WordLength(commentOptions.Prefix);

        // The root XML line must not output its own tags, only its child lines.
        if (line is CommentLineXml xml)
        {
            if (_formatterOptions.Xml.AlignParamTags)
            {
                AlignParamTags(xml);
            }

            foreach (var l in xml.Lines)
            {
                NewLine();
                Format(l);
            }
        }
        else
        {
            Format(line);
        }
    }

    public bool Equals(string other) => string.Equals(ToString(), other);

    public override string ToString() => _builder.ToString().TrimEnd();

    private static void AlignParamTags(CommentLineXml xml)
    {
        var paramPhrases = xml.Lines.OfType<CommentLineXml>()
            .Where(p => string.Equals(p.TagName, "param", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (paramPhrases.Count > 1)
        {
            // If param tags are broken into separate lines there is nothing to align.
            var paramSplit = paramPhrases[0].TagOptions.Split.HasFlag(XmlTagNewLine.AfterOpen);
            if (!paramSplit)
            {
                var longestParam = paramPhrases.Max(p => p.OpenTag.Length);
                foreach (var phrase in paramPhrases)
                {
                    phrase.OpenTag = phrase.OpenTag.PadRight(longestParam);
                }
            }
        }
    }

    private void Append(char value) => Append(value.ToString());

    /// <summary>
    /// Appends to the current line, writing the prefix, indentation and initial spacer first when
    /// the line is still empty.
    /// </summary>
    private void Append(string value, bool noIndenting = false)
    {
        if (!_isPrefixWritten)
        {
            _builder.Append(_commentOptions.Prefix);
            _currentPosition += _commentPrefixLength;
            _isPrefixWritten = true;
            _isIndented = false;
        }

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!_isIndented && !noIndenting)
        {
            // Empty prefix also means no initial spacing.
            if (_commentPrefixLength > 0)
            {
                _builder.Append(CodeCommentHelper.Spacer);
                _currentPosition += 1;
            }

            if (_indentAmount > 0)
            {
                _builder.Append(string.Empty.PadLeft(_indentAmount));
                _currentPosition += _indentAmount;
            }

            _isIndented = true;
        }

        _builder.Append(value);
        _currentPosition += WordLength(value);
        _isFirstWord = false;
    }

    /// <returns><c>true</c> if the line fitted on a single line.</returns>
    private bool Format(ICommentLine line, int xmlTagLength = 0, bool xmlSpaceParentTagContent = false)
    {
        if (line is CommentLineXml xml)
        {
            return FormatXml(xml);
        }

        if (line.Content == null)
            return true;

        var matches = _commentOptions.Regex.Matches(line.Content)
            .OfType<Match>()
            .Select(x => new CodeCommentMatch(x, _formatterOptions))
            .ToList();

        // Remove empty matches from the start and end of the comment.
        CodeCommentMatch m;
        while (((m = matches.FirstOrDefault()) != null && m.IsEmpty) || ((m = matches.LastOrDefault()) != null && m.IsEmpty))
        {
            matches.Remove(m);
        }

        // Join the comment matches into single lines where possible.
        if (matches.Count > 1)
        {
            int i = 0;
            do
            {
                m = matches[i];
                if (m.TryAppend(matches[i + 1]))
                {
                    matches.RemoveAt(i + 1);
                }
                else
                {
                    i++;
                }
            } while (i < matches.Count - 1);
        }

        // Break if there is more than 1 line match (e.g. a list or child xml tags), or if the
        // content does not fit on a single line.
        var matchCount = matches.Count;
        var forceBreak = matchCount > 1;
        var fittedOnLine = true;

        if (!forceBreak && matchCount == 1 && matches[0].Words.Any())
        {
            var firstLineLength = _commentPrefixLength + xmlTagLength + matches[0].Length + _indentAmount;

            if (_formatterOptions.SkipWrapOnLastWord)
            {
                firstLineLength -= WordLength(matches[0].Words.Last()) + 1;
            }

            forceBreak = firstLineLength > _formatterOptions.WrapColumn;
        }

        if (_currentPosition == 0 || (!_isFirstWord && forceBreak))
        {
            NewLine();
            fittedOnLine = false;
        }
        else if (!_isFirstWord && xmlSpaceParentTagContent)
        {
            Append(CodeCommentHelper.Spacer);
        }

        // The word after an opening tag is the first word, to avoid an extra leading space.
        _isFirstWord = true;

        foreach (var match in matches)
        {
            if (match.IsLiteral || match.IsList)
            {
                if (!_isFirstWord)
                {
                    NewLine();
                    fittedOnLine = false;
                }
            }

            if (match.IsList)
            {
                Append(match.ListPrefix);

                // List items include their spacing, so we are logically still on the first word.
                _isFirstWord = true;
            }

            if (!match.IsEmpty)
            {
                var wordCount = match.Words.Count - 1;

                for (int i = 0; i <= wordCount; i++)
                {
                    var word = match.Words[i];
                    var length = WordLength(word);
                    var wrap = false;

                    // Never wrap on the first word, otherwise a word longer than the column would
                    // wrap forever.
                    if (!_isFirstWord && _currentPosition + length + 1 > _formatterOptions.WrapColumn)
                    {
                        wrap = true;
                    }

                    if (wrap && i == wordCount && _formatterOptions.SkipWrapOnLastWord)
                    {
                        wrap = false;
                    }

                    if (wrap)
                    {
                        NewLine();
                        fittedOnLine = false;

                        // Keep list continuation lines aligned with the list text.
                        if (match.IsList)
                        {
                            Append(string.Empty.PadLeft(WordLength(match.ListPrefix), CodeCommentHelper.Spacer));
                            _isFirstWord = true;
                        }
                    }
                    else if (!_isFirstWord)
                    {
                        Append(CodeCommentHelper.Spacer);
                    }

                    Append(CodeCommentHelper.FakeToSpace(word));
                }
            }
            else
            {
                // A line without words becomes a blank line.
                NewLine();
                NewLine(true);
                fittedOnLine = false;
            }
        }

        return fittedOnLine;
    }

    /// <returns><c>true</c> if the tag stayed on a single line.</returns>
    private bool FormatXml(CommentLineXml xml)
    {
        var isLiteralContent = !string.IsNullOrEmpty(xml.Content);
        var split = xml.TagOptions.Split;

        if (isLiteralContent)
        {
            // Tags holding literal content with multiple lines always go on their own lines.
            if (xml.Content.Contains('\n'))
                split = XmlTagNewLine.Always;
        }
        else if ((split == XmlTagNewLine.Default || split == XmlTagNewLine.Content) && xml.Lines.Count > 1)
        {
            split = XmlTagNewLine.Always;
        }

        if (split.HasFlag(XmlTagNewLine.BeforeOpen) && !_isFirstWord)
        {
            NewLine();
        }

        Append(xml.TagOptions.KeepTogether ? CodeCommentHelper.FakeToSpace(xml.OpenTag) : xml.OpenTag);

        // Self closing tags have no content.
        if (xml.IsSelfClosing)
        {
            if (split.HasFlag(XmlTagNewLine.AfterClose))
            {
                if (!xml.IsLast)
                {
                    NewLine();
                }

                return false;
            }

            return true;
        }

        if (split.HasFlag(XmlTagNewLine.AfterOpen))
        {
            NewLine();
        }

        _indentAmount += xml.TagOptions.Indent;

        if (isLiteralContent)
        {
            // Literal content is output without formatting.
            var literals = xml.Content.Trim('\r', '\n').TrimEnd('\r', '\n', '\t', ' ').Split('\n');
            for (int i = 0; i < literals.Length; i++)
            {
                if (i > 0)
                    NewLine(true);

                Append(literals[i].TrimEnd(), true);
            }
        }
        else
        {
            var xmlTagLength = WordLength(xml.OpenTag) + WordLength(xml.CloseTag) + (xml.TagOptions.SpaceContent ? 2 : 0);

            foreach (var line in xml.Lines)
            {
                if (!Format(line, xmlTagLength, xml.TagOptions.SpaceContent))
                    split |= XmlTagNewLine.BeforeClose | XmlTagNewLine.AfterClose;
            }
        }

        _indentAmount -= xml.TagOptions.Indent;

        // If the opening tag was on its own line, so is the closing tag.
        if (split.HasFlag(XmlTagNewLine.BeforeClose))
        {
            NewLine();
        }
        else if (xml.TagOptions.SpaceContent)
        {
            Append(CodeCommentHelper.Spacer);
        }

        Append(xml.CloseTag);

        if (split.HasFlag(XmlTagNewLine.AfterClose))
        {
            NewLine();
            return false;
        }

        return true;
    }

    /// <summary>Appends a new line, unless the buffer is already on an empty new line.</summary>
    private void NewLine(bool force = false)
    {
        if (_isFirstWord && force)
        {
            Append(string.Empty);
        }

        if (!_isFirstWord || force)
        {
            _builder.Append('\n');
            _currentPosition = 0;

            _isPrefixWritten = false;
            _isFirstWord = true;
        }
    }

    /// <summary>Length of a string, counting a tab as reaching the next tab stop.</summary>
    private int WordLength(string word) =>
        word == null ? 0 : word.Length + word.Count(c => c == '\t') * (_formatterOptions.TabSize - 1);
}
