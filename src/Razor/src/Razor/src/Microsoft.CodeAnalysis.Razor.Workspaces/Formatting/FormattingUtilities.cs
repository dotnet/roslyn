// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal static class FormattingUtilities
{
    /// <summary>
    /// Counts the number of non-whitespace characters in a given span of text.
    /// </summary>
    /// <param name="text">The source text</param>
    /// <param name="start">Inclusive position for where to start counting</param>
    /// <param name="endExclusive">Exclusive for where to stop counting</param>
    public static int CountNonWhitespaceChars(SourceText text, int start, int endExclusive)
    {
        var count = 0;
        for (var i = start; i < endExclusive; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                count++;
            }
        }

        return count;
    }

    public static int GetIndentationLevel(TextLine line, int firstNonWhitespaceCharacterPosition, bool insertSpaces, int tabSize, out int additionalIndentation)
    {
        if (firstNonWhitespaceCharacterPosition > line.End)
        {
            throw new ArgumentOutOfRangeException(nameof(firstNonWhitespaceCharacterPosition), "The first non-whitespace character position must be within the line.");
        }

        // For spaces, the actual indentation needs to be divided by the tab size to get the level, and additional is the remainder
        var currentIndentationWidth = firstNonWhitespaceCharacterPosition - line.Start;
        if (insertSpaces)
        {
            return GetIndentationLevel(currentIndentationWidth, tabSize, out additionalIndentation);
        }

        // For tabs, we just count the tabs, and additional is any spaces at the end.
        var tabCount = 0;
        var text = line.Text.AssumeNotNull();
        for (var i = line.Start; i < firstNonWhitespaceCharacterPosition; i++)
        {
            if (text[i] == '\t')
            {
                tabCount++;
            }
            else
            {
                Debug.Assert(text[i] == ' ');
                additionalIndentation = firstNonWhitespaceCharacterPosition - i;
                return tabCount;
            }
        }

        additionalIndentation = 0;
        return tabCount;
    }

    public static int GetIndentationLevel(int length, int tabSize, out int additionalIndentation)
    {
        var indentationLevel = length / tabSize;
        additionalIndentation = length % tabSize;
        return indentationLevel;
    }

    /// <summary>
    /// Given a <paramref name="indentation"/> amount of characters, generate a string representing the configured indentation.
    /// </summary>
    /// <param name="indentation">An amount of characters to represent the indentation.</param>
    /// <param name="insertSpaces">Whether spaces are used for indentation.</param>
    /// <param name="tabSize">The size of a tab and indentation.</param>
    /// <returns>A whitespace string representation indentation.</returns>
    public static string GetIndentationString(int indentation, bool insertSpaces, int tabSize)
        => IndentCache.GetIndentString(indentation, insertSpaces, tabSize);

    /// <summary>
    /// Unindents a span of text with a few caveats:
    ///
    /// 1. This assumes consistency in tabs/spaces for starting whitespace per line
    /// 2. This doesn't format the text, just attempts to remove leading whitespace in a uniform way
    /// 3. It will never remove non-whitespace
    ///
    /// This was made with extracting code into a new file in mind because it's not trivial to format that text and make
    /// sure the indentation is right. Use with caution.
    /// </summary>
    public static void NaivelyUnindentSubstring(SourceText text, TextSpan extractionSpan, System.Text.StringBuilder builder)
    {
        var extractedText = text.ToString(extractionSpan);
        var range = text.GetRange(extractionSpan);
        if (range.Start.Line == range.End.Line)
        {
            builder.Append(extractedText);
            return;
        }

        var extractedTextSpan = extractedText.AsSpan();
        var indentation = GetNormalizedIndentation(text, extractionSpan);

        foreach (var lineRange in GetLineRanges(extractedText))
        {
            var lineSpan = extractedTextSpan[lineRange];
            lineSpan = UnindentLine(lineSpan, indentation);

            foreach (var c in lineSpan)
            {
                builder.Append(c);
            }
        }

        //
        // Local Methods
        //

        static ReadOnlySpan<char> UnindentLine(ReadOnlySpan<char> line, int indentation)
        {
            var startCharacter = 0;
            while (startCharacter < indentation && IsTabOrSpace(line[startCharacter]))
            {
                startCharacter++;
            }

            return line[startCharacter..];
        }

        // Gets the smallest indentation of all the lines in a given span
        static int GetNormalizedIndentation(SourceText sourceText, TextSpan span)
        {
            var indentation = int.MaxValue;
            foreach (var line in sourceText.Lines)
            {
                if (!span.OverlapsWith(line.Span))
                {
                    continue;
                }

                indentation = Math.Min(indentation, GetIndentation(line));
            }

            return indentation;
        }

        static int GetIndentation(TextLine line)
        {
            if (line.Text is null)
            {
                return 0;
            }

            var indentation = 0;
            for (var position = line.Span.Start; position < line.Span.End; position++)
            {
                var c = line.Text[position];
                if (!IsTabOrSpace(c))
                {
                    break;
                }

                indentation++;
            }

            return indentation;
        }

        static bool IsTabOrSpace(char c)
        {
            return c == ' ' || c == '\t';
        }

        static ImmutableArray<Range> GetLineRanges(string text)
        {
            using var builder = new PooledArrayBuilder<Range>();
            var start = 0;
            var end = text.IndexOf('\n');
            while (true)
            {
                if (end == -1)
                {
                    builder.Add(new(start, text.Length));
                    break;
                }

                // end + 1 to include the new line
                builder.Add(new(start, end + 1));
                start = end + 1;
                if (start == text.Length)
                {
                    break;
                }

                end = text.IndexOf('\n', start);
            }

            return builder.ToImmutableAndClear();
        }
    }

    /// <summary>
    /// Sometimes the Html language server will send back an edit that contains a tilde, because the generated
    /// document we send them has lots of tildes. In those cases, we need to do some extra work to compute the
    /// minimal text edits
    /// </summary>
    public static TextEdit[] FixHtmlTextEdits(SourceText htmlSourceText, TextEdit[] edits)
    {
        // Avoid computing a minimal diff if we don't need to
        if (!edits.Any(static e => e.NewText.Contains('~')))
            return edits;

        var changes = edits.SelectAsArray(htmlSourceText.GetTextChange);

        var fixedChanges = htmlSourceText.MinimizeTextChanges(changes);
        return fixedChanges.SelectAsPlainArray(htmlSourceText.GetTextEdit);
    }

    internal static SumType<TextEdit, AnnotatedTextEdit>[] FixHtmlTextEdits(SourceText htmlSourceText, SumType<TextEdit, AnnotatedTextEdit>[] edits)
    {
        // Avoid computing a minimal diff if we don't need to
        if (!edits.Any(static e => ((TextEdit)e).NewText.Contains('~')))
            return edits;

        var changes = edits.SelectAsArray(e => htmlSourceText.GetTextChange((TextEdit)e));

        var fixedChanges = htmlSourceText.MinimizeTextChanges(changes);
        return fixedChanges.SelectAsPlainArray<TextChange, SumType<TextEdit, AnnotatedTextEdit>>(c => htmlSourceText.GetTextEdit(c));
    }

    public static void GetOriginalDocumentChangesFromLineInfo(FormattingContext context, SourceText originalText, ImmutableArray<LineInfo> formattedLineInfo, SourceText formattedText, ILogger logger, Func<int, bool>? shouldKeepInsertedNewlineAtPosition, ref PooledArrayBuilder<TextChange> formattingChanges, out int lastFormattedTextLine)
    {
        var iFormatted = 0;
        for (var iOriginal = 0; iOriginal < originalText.Lines.Count; iOriginal++, iFormatted++)
        {
            var lineInfo = formattedLineInfo[iOriginal];

            if (lineInfo.SkippedPreviousLineOriginOffset is { } skippedPreviousLineOriginOffset)
            {
                var skippedPreviousLineIndentationWidth = GetFixedIndentationWidth(context, lineInfo);
                var currentLineWasCollapsedIntoSkippedPreviousLine =
                    CurrentLineIsOnlyAnOpenBrace(originalText, iOriginal) &&
                    CurrentLineEndsWithAnOpenBrace(formattedText, iFormatted);

                FormatSkippedPreviousLine(
                    context,
                    originalText,
                    formattedText,
                    logger,
                    shouldKeepInsertedNewlineAtPosition,
                    skippedPreviousLineOriginOffset,
                    skippedPreviousLineIndentationWidth,
                    ref formattingChanges,
                    iOriginal,
                    ref iFormatted);

                if (currentLineWasCollapsedIntoSkippedPreviousLine)
                {
                    // The carried-forward previous line already consumed this brace-only line by pulling `{`
                    // back up onto the previous line, so the normal per-line processing below must skip it.
                    continue;
                }

                iFormatted++;
            }

            if (iFormatted >= formattedText.Lines.Count)
            {
                break;
            }

            string? indentationString = null;

            var formattedLine = formattedText.Lines[iFormatted];
            if (formattedLine.GetFirstNonWhitespaceOffset() is { } formattedIndentation)
            {
                var originalLine = originalText.Lines[iOriginal];
                var originalLineOffset = originalLine.GetFirstNonWhitespaceOffset().GetValueOrDefault();
                var fixedIndentationWidth = GetFixedIndentationWidth(context, lineInfo);

                if (lineInfo.ProcessIndentation)
                {
                    // First up, we take the indentation from the formatted file, and add on the fixed indentation level from the line info, and
                    // replace whatever was in the original file with it.
                    indentationString = GetAdjustedIndentationString(context, formattedLine, fixedIndentationWidth);
                    formattingChanges.Add(new TextChange(new TextSpan(originalLine.Start, originalLineOffset), indentationString));
                }

                // Now we handle the formatting, which is changes to the right of the first non-whitespace character.
                if (lineInfo.ProcessFormatting)
                {
                    // The offset and length properties of the line info are relative to the indented content in their respective documents.
                    // In other words, relative to the first non-whitespace character on the line.
                    var originalStart = originalLine.Start + originalLineOffset + lineInfo.OriginOffset;
                    var length = lineInfo.FormattedLength == 0
                        ? originalLine.End - originalStart
                        : lineInfo.FormattedLength;
                    var initialFormattedLine = iFormatted;
                    var formattedStart = formattedLine.Start + formattedIndentation + lineInfo.FormattedOffset;
                    var formattedEnd = formattedLine.End - lineInfo.FormattedOffsetFromEndOfLine;
                    if (lineInfo.FormattedOffsetFromEndOfLine > 0)
                    {
                        // This is the partial-line case: we cannot use ConsumeNewLines because we're intentionally
                        // trimming trailing generated text, so instead we only advance the formatted side until
                        // we've captured all of the non-whitespace Roslyn moved onto wrapped lines.
                        var originalNonWhitespace = CountNonWhitespaceChars(originalText, originalStart, originalStart + length);
                        var formattedNonWhitespace = CountNonWhitespaceChars(formattedText, formattedStart, Math.Max(formattedStart, formattedEnd));

                        while (originalNonWhitespace > formattedNonWhitespace &&
                            iFormatted + 1 < formattedText.Lines.Count)
                        {
                            iFormatted++;
                            formattedLine = formattedText.Lines[iFormatted];
                            formattedEnd = formattedLine.End - lineInfo.FormattedOffsetFromEndOfLine;
                            formattedNonWhitespace = CountNonWhitespaceChars(formattedText, formattedStart, Math.Max(formattedStart, formattedEnd));
                        }
                    }

                    if (formattedEnd > formattedStart)
                    {
                        // Start with Roslyn's raw formatted slice, then reapply Razor's fixed indentation
                        // if the formatter wrapped it across lines so closing braces/trailing text stay aligned.
                        string replacementText;
                        if (iFormatted > initialFormattedLine)
                        {
                            using var _ = StringBuilderPool.GetPooledObject(out var replacementBuilder);

                            replacementBuilder.Append(formattedText.ToString(TextSpan.FromBounds(
                                formattedStart,
                                formattedText.Lines[initialFormattedLine].EndIncludingLineBreak)));

                            for (var wrappedLineIndex = initialFormattedLine + 1; wrappedLineIndex < iFormatted; wrappedLineIndex++)
                            {
                                replacementBuilder.Append(GetAdjustedFormattedLineText(context, formattedText.Lines[wrappedLineIndex], fixedIndentationWidth, formattedText.Lines[wrappedLineIndex].EndIncludingLineBreak));
                            }

                            replacementBuilder.Append(GetAdjustedFormattedLineText(context, formattedText.Lines[iFormatted], fixedIndentationWidth, formattedEnd));
                            replacementText = replacementBuilder.ToString();
                        }
                        else
                        {
                            replacementText = formattedText.ToString(TextSpan.FromBounds(formattedStart, formattedEnd));
                        }

                        formattingChanges.Add(new TextChange(new TextSpan(originalStart, length), replacementText));
                    }

                    if (lineInfo.CheckForNewLines)
                    {
                        Debug.Assert(lineInfo.FormattedLength == 0, "Can't have a FormattedLength if we're looking for new lines. The logic is incompatible.");
                        Debug.Assert(lineInfo.FormattedOffsetFromEndOfLine == 0, "Can't have a FormattedOffsetFromEndOfLine if we're looking for new lines. The logic is incompatible.");

                        ConsumeNewLines(
                            context, originalText, formattedText, logger, shouldKeepInsertedNewlineAtPosition,
                            ref formattingChanges, ref iOriginal, ref iFormatted, ref originalLine, ref formattedLine,
                            originalStart, formattedStart, fixedIndentationWidth);
                    }

                    // The above "CheckForNewLines" means new lines inserted in the middle of a line of the original text, but
                    // the formatter may have inserted a blank line after the current line too. In that case we need to make sure
                    // we advance the formatted line pointer past it, but also include it. This only applies if the line after the
                    // blank line matches the next original line and the next original line isn't blank (ie, an actual insertion)
                    if (iFormatted + 1 < formattedText.Lines.Count &&
                        formattedText.Lines[iFormatted + 1].Span.Length == 0 &&
                        iOriginal + 1 < originalText.Lines.Count &&
                        originalText.Lines[iOriginal + 1] is { } nextOriginalLine &&
                        nextOriginalLine.Span.Length != 0)
                    {
                        // Next formatted line is blank but next original line isn't, so the
                        // formatter inserted a blank line. Consume it and preserve it in the output.
                        // We insert at EndIncludingLineBreak so the blank line appears after any
                        // wrapped lines that ConsumeNewLines inserted (which also use EndIncludingLineBreak).
                        iFormatted++;
                        formattingChanges.Add(new TextChange(new(originalLine.EndIncludingLineBreak, 0), context.NewLineString));
                    }
                }
            }

            if (lineInfo.SkipNextLine)
            {
                iFormatted++;
            }
            else if (lineInfo.SkipNextLineIfBrace)
            {
                // If the next line is a brace, we skip it. This is used for synthetic lines like:
                //
                //     class @code {
                //     () => {
                //
                // Roslyn may keep the brace on that same line, move it down to the next line, or pull an original
                // next-line brace back up. We have to tolerate all of those shapes when mapping the formatted C#
                // back onto the Razor document.
                if (NextLineIsOnlyAnOpenBrace(formattedText, iFormatted))
                {
                    iFormatted++;
                }

                // The reverse case is an original next-line brace being collapsed up by Roslyn. For example, Razor may
                // start with:
                //
                //     @code
                //     {
                //
                // while the generated C# becomes `class @code {` on one line. In that case we skip the original brace
                // line and copy the previous line's indentation onto it so the surviving brace still lands where Roslyn
                // intended. Fortunately `@code {\r\n {` is illegal in Razor, so there are no false positives here.
                if (NextLineIsOnlyAnOpenBrace(originalText, iOriginal))
                {
                    iOriginal++;

                    // We're skipping a line in the original document, because Roslyn brought it up to the previous
                    // line, but the fact is the opening brace was in the original document, and might need its indentation
                    // adjusted. Since we can't reason about this line in any way, because Roslyn has changed it, we just
                    // apply the indentation from the previous line.
                    //
                    // If we didn't adjust the indentation of the previous line, then we really have no information to go
                    // on at all, so hopefully the user is happy with where their open brace is.
                    if (indentationString is not null)
                    {
                        var originalLine = originalText.Lines[iOriginal];
                        var originalLineOffset = originalLine.GetFirstNonWhitespaceOffset().GetValueOrDefault();
                        formattingChanges.Add(new TextChange(new TextSpan(originalLine.Start, originalLineOffset), indentationString));
                    }
                }
            }
        }

        lastFormattedTextLine = iFormatted;
    }

    private static int GetFixedIndentationWidth(FormattingContext context, LineInfo lineInfo)
        => context.GetIndentationOffsetForLevel(lineInfo.FixedIndentLevel) + (lineInfo.AdditionalIndentation ?? 0);

    /// <summary>
    /// Recomputes only the leading indentation for a formatted line after Razor's fixed indentation has been applied.
    /// </summary>
    private static string GetAdjustedIndentationString(FormattingContext context, TextLine formattedLine, int fixedIndentationWidth)
    {
        var indentationWidth = formattedLine.GetIndentationSize(context.Options.TabSize) + fixedIndentationWidth;
        if (indentationWidth < 0)
        {
            indentationWidth = 0;
        }

        return GetIndentationString(indentationWidth, context.Options.InsertSpaces, context.Options.TabSize);
    }

    /// <summary>
    /// Returns a formatted line slice with adjusted indentation.
    /// Unlike <see cref="GetAdjustedIndentationString"/>, this preserves Roslyn's original slice when no adjustment is needed.
    /// </summary>
    private static string GetAdjustedFormattedLineText(FormattingContext context, TextLine formattedLine, int fixedIndentationWidth, int end)
    {
        var lineText = formattedLine.Text.AssumeNotNull();
        if (fixedIndentationWidth == 0)
        {
            return lineText.ToString(TextSpan.FromBounds(formattedLine.Start, end));
        }

        var indentationString = GetAdjustedIndentationString(context, formattedLine, fixedIndentationWidth);

        if (formattedLine.GetFirstNonWhitespaceOffset() is { } firstNonWhitespace &&
            formattedLine.Start + firstNonWhitespace < end)
        {
            return indentationString + lineText.ToString(TextSpan.FromBounds(formattedLine.Start + firstNonWhitespace, end));
        }

        return end > formattedLine.End
            ? indentationString + lineText.ToString(TextSpan.FromBounds(formattedLine.End, end))
            : indentationString;
    }

    private static void FormatSkippedPreviousLine(
        FormattingContext context,
        SourceText originalText,
        SourceText formattedText,
        ILogger logger,
        Func<int, bool>? shouldKeepInsertedNewlineAtPosition,
        int skippedPreviousLineOriginOffset,
        int fixedIndentationWidth,
        ref PooledArrayBuilder<TextChange> formattingChanges,
        int iOriginal,
        ref int iFormatted)
    {
        Debug.Assert(iOriginal > 0);
        Debug.Assert(iFormatted < formattedText.Lines.Count);

        if (iOriginal == 0 || iFormatted >= formattedText.Lines.Count)
        {
            // This helper only applies to a carried-forward previous line from a multiline expression. If we're on the
            // first original line, or we've already consumed all formatted lines, there is nothing safe to map back.
            return;
        }

        var previousOriginalLineIndex = iOriginal - 1;
        var originalLine = originalText.Lines[previousOriginalLineIndex];
        var formattedLine = formattedText.Lines[iFormatted];
        if (formattedLine.GetFirstNonWhitespaceOffset() is not { } formattedIndentation)
        {
            // Roslyn can leave the carried-forward line blank or whitespace-only. In that case there is no formatted
            // content to apply to the previous original line, so we leave it alone and let normal processing continue.
            return;
        }

        var originalStart = originalLine.Start + skippedPreviousLineOriginOffset;
        var formattedStart = formattedLine.Start + formattedIndentation;
        formattingChanges.Add(new TextChange(
            TextSpan.FromBounds(originalStart, originalLine.End),
            formattedText.ToString(TextSpan.FromBounds(formattedStart, formattedLine.End))));

        ConsumeNewLines(
            context, originalText, formattedText, logger, shouldKeepInsertedNewlineAtPosition,
            ref formattingChanges, ref previousOriginalLineIndex, ref iFormatted, ref originalLine, ref formattedLine,
            originalStart, formattedStart, fixedIndentationWidth);
    }

    private static bool NextLineIsOnlyAnOpenBrace(SourceText text, int lineNumber)
        => lineNumber + 1 < text.Lines.Count &&
            CurrentLineIsOnlyAnOpenBrace(text, lineNumber + 1);

    private static bool CurrentLineIsOnlyAnOpenBrace(SourceText text, int lineNumber)
        => lineNumber >= 0 &&
            lineNumber < text.Lines.Count &&
            text.Lines[lineNumber] is { Span.Length: > 0 } line &&
            line.GetFirstNonWhitespaceOffset() is { } firstNonWhitespace &&
            line.Start + firstNonWhitespace == line.End - 1 &&
            line.CharAt(firstNonWhitespace) == '{';

    private static bool CurrentLineEndsWithAnOpenBrace(SourceText text, int lineNumber)
    {
        if (lineNumber < 0 || lineNumber >= text.Lines.Count)
        {
            return false;
        }

        var line = text.Lines[lineNumber];
        for (var i = line.End - 1; i >= line.Start; i--)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return text[i] == '{';
            }
        }

        return false;
    }

    /// <summary>
    /// Handles the case where the external formatter has changed the number of lines by inserting or removing newlines.
    /// The primary side (formatted lines when inserting, original lines when removing) is consumed first, and then
    /// the secondary side is consumed to keep content aligned if the formatter wrapped at a different point.
    /// </summary>
    private static void ConsumeNewLines(
        FormattingContext context,
        SourceText originalText,
        SourceText formattedText,
        ILogger logger,
        Func<int, bool>? shouldKeepInsertedNewlineAtPosition,
        ref PooledArrayBuilder<TextChange> formattingChanges,
        ref int iOriginal,
        ref int iFormatted,
        ref TextLine originalLine,
        ref TextLine formattedLine,
        int originalStart,
        int formattedStart,
        int fixedIndentationWidth)
    {
        // We assume the external formatter won't change anything but whitespace, so we can just apply the
        // changes directly, but it could very well be adding whitespace in the form of newlines, for example
        // taking "if (true) {" and making it run over two lines, or even "string Prop { get" and making it
        // span three lines. Since we assume it won't change anything non-whitespace, we just keep inserting
        // the formatted lines of C# until we match the original line contents.
        // Of course, the formatter could just as easily remove whitespace, eg making a "class Goo\n{" into
        // "class Goo {", so whilst the same theory applies, instead of inserting formatted lines, we eat
        // the original lines.

        var originalNonWhitespace = CountNonWhitespaceChars(originalText, originalStart, originalLine.End);
        var formattedNonWhitespace = CountNonWhitespaceChars(formattedText, formattedStart, formattedLine.End);

        if (originalNonWhitespace == formattedNonWhitespace)
        {
            return;
        }

        var formatterInsertedNewLines = originalNonWhitespace > formattedNonWhitespace;

        // Before we start skipping formatted lines, we need the info to work out where exactly the newline is being added
        var originalPosition = originalStart + (formattedLine.End - formattedStart);
        var consumedFromSecondarySide = false;

        while (!originalText.NonWhitespaceContentEquals(formattedText, originalStart, originalLine.End, formattedStart, formattedLine.End))
        {
            // Consume from the primary side: formatted lines if the formatter inserted newlines, original lines if it removed them.
            var didAdvance = formatterInsertedNewLines
                ? TryAdvanceLine(context, logger, "formatted", formattedText, ref iFormatted, ref formattedLine, iOriginal, originalText.Lines.Count)
                : TryAdvanceLine(context, logger, "original", originalText, ref iOriginal, ref originalLine, iFormatted, formattedText.Lines.Count);

            if (!didAdvance)
            {
                break;
            }

            // After consuming from the primary side, the other side's content may now be insufficient
            // (e.g., the formatter wrapped at a different point). Consume from the secondary side to keep aligned.
            var originalNonWS = CountNonWhitespaceChars(originalText, originalStart, originalLine.End);
            var formattedNonWS = CountNonWhitespaceChars(formattedText, formattedStart, formattedLine.End);
            var secondaryNeedsAdvancing = formatterInsertedNewLines
                ? originalNonWS < formattedNonWS
                : originalNonWS > formattedNonWS;

            while (secondaryNeedsAdvancing)
            {
                didAdvance = formatterInsertedNewLines
                    ? TryAdvanceLine(context, logger, "original", originalText, ref iOriginal, ref originalLine, iFormatted, formattedText.Lines.Count)
                    : TryAdvanceLine(context, logger, "formatted", formattedText, ref iFormatted, ref formattedLine, iOriginal, originalText.Lines.Count);

                if (!didAdvance)
                {
                    break;
                }

                consumedFromSecondarySide = true;

                originalNonWS = CountNonWhitespaceChars(originalText, originalStart, originalLine.End);
                formattedNonWS = CountNonWhitespaceChars(formattedText, formattedStart, formattedLine.End);
                secondaryNeedsAdvancing = formatterInsertedNewLines
                    ? originalNonWS < formattedNonWS
                    : originalNonWS > formattedNonWS;
            }

            // When we haven't consumed from the secondary side, the formatter purely added or removed lines,
            // so we emit per-line text changes.
            if (!consumedFromSecondarySide)
            {
                if (formatterInsertedNewLines)
                {
                    // The current line has been split into multiple lines, but its up to whoever called us to decide if we're keeping that.
                    if (shouldKeepInsertedNewlineAtPosition?.Invoke(originalPosition) ?? true)
                    {
                        // If we're keeping it, we insert this newline after the original line, with the correct indentation.
                        formattingChanges.Add(new TextChange(
                            new(originalLine.EndIncludingLineBreak, 0),
                            GetAdjustedFormattedLineText(context, formattedLine, fixedIndentationWidth, formattedLine.EndIncludingLineBreak)));
                    }
                    else
                    {
                        // If we're not keeping the newline, we need to restore this line back to the original line it came from
                        formattingChanges.Add(new TextChange(new(originalLine.End, 0), formattedText.ToString(formattedLine.Span)));
                    }
                }
                else
                {
                    // The formatter has removed newlines, so we need to remove the original lines.
                    formattingChanges.Add(new TextChange(TextSpan.FromBounds(originalText.Lines[iOriginal - 1].End, originalLine.End), ""));
                }
            }
        }

        if (consumedFromSecondarySide)
        {
            // The formatter re-wrapped content at a different point, consuming lines from both sides.
            // Update the formatting change to cover the full range of consumed original and formatted lines.
            formattingChanges[^1] = new TextChange(
                TextSpan.FromBounds(originalStart, originalLine.End),
                formattedText.ToString(TextSpan.FromBounds(formattedStart, formattedLine.End)));
        }
    }

    private static bool TryAdvanceLine(
        FormattingContext context,
        ILogger logger,
        string label,
        SourceText text,
        ref int lineIndex,
        ref TextLine line,
        int otherLineIndex,
        int otherLineCount)
    {
        lineIndex++;
        if (lineIndex >= text.Lines.Count)
        {
            context.Logger?.LogMessage($"Ran out of {label} lines at index {lineIndex} of {text.Lines.Count} (other side: {otherLineIndex} of {otherLineCount})");
            logger.LogError(SR.Formatting_Error);
            return false;
        }

        line = text.Lines[lineIndex];
        return true;
    }
}
