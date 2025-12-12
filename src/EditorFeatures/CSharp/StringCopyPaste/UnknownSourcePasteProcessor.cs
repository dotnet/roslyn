// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

using static StringCopyPasteHelpers;

/// <summary>
/// Paste processor responsible for determining how text should be treated if it came from a source outside of the
/// editor we're in.  In that case, we don't know what any particular piece of text means.  For example, <c>\t</c>
/// might be a tab or it could be the literal two characters <c>\</c> and <c>t</c>.
/// </summary>
internal sealed class UnknownSourcePasteProcessor(
    string newLine,
    string indentationWhitespace,
    ITextSnapshot snapshotBeforePaste,
    ITextSnapshot snapshotAfterPaste,
    Document documentBeforePaste,
    Document documentAfterPaste,
    ExpressionSyntax stringExpressionBeforePaste,
    bool pasteWasSuccessful) : AbstractPasteProcessor(newLine, indentationWhitespace, snapshotBeforePaste, snapshotAfterPaste, documentBeforePaste, documentAfterPaste, stringExpressionBeforePaste)
{
    /// <summary>
    /// Whether or not the string expression remained successfully parseable after the paste.  <see
    /// cref="StringCopyPasteCommandHandler.PasteWasSuccessful"/>.  If it can still be successfully parsed subclasses
    /// can adjust their view on which pieces of content need to be escaped or not.
    /// </summary>
    private readonly bool _pasteWasSuccessful = pasteWasSuccessful;

    public override ImmutableArray<TextChange> GetEdits()
    {
        // If we have a raw-string, then we always want to check for changes to make, even if the paste was
        // technically legal.  This is because we may want to touch up things like indentation to make the
        // pasted text look good for raw strings.
        //
        // Check for certain things we always think we should escape.
        if (!IsAnyRawStringExpression(StringExpressionBeforePaste) && !ShouldAlwaysEscapeTextForNonRawString())
        {
            // If the pasting was successful, then no need to change anything.
            if (_pasteWasSuccessful)
                return default;
        }

        // Ok, the user pasted text that couldn't cleanly be added to this token without issue. Repaste the
        // contents, but this time properly escaped/manipulated so that it follows the rule of the particular token
        // kind.

        // For pastes into non-raw strings, we can just determine how the change should be escaped in-line at that
        // same location the paste originally happened at.  For raw-strings things get more complex as we have to
        // deal with things like indentation and potentially adding newlines to make things legal.
        return IsAnyRawStringExpression(StringExpressionBeforePaste)
            ? GetEditsForRawString()
            : GetEditsForNonRawString();
    }

    private string EscapeForNonRawStringLiteral(string value)
        => EscapeForNonRawStringLiteral_DoNotCallDirectly(
            IsVerbatimStringExpression(StringExpressionBeforePaste),
            StringExpressionBeforePaste is InterpolatedStringExpressionSyntax,
            // We do not want to try skipping escapes in the 'value'.  We don't know where it came from, and if it
            // had some escapes in it, it's probably a good idea to remove to keep the final pasted text clean.
            trySkipExistingEscapes: true,
            value);

    private bool ShouldAlwaysEscapeTextForNonRawString()
    {
        // Pasting a control character into a normal string literal is normally not desired.  So even if this
        // is legal, we still escape the contents to make the pasted code clear.
        return !IsVerbatimStringExpression(StringExpressionBeforePaste) && ContainsControlCharacter(Changes);
    }

    private ImmutableArray<TextChange> GetEditsForNonRawString()
    {
        using var textChanges = TemporaryArray<TextChange>.Empty;

        foreach (var change in Changes)
        {
            // We're pasting from an unknown source.  If we see a viable escape in that source treat it as an escape
            // instead of escaping it one more time upon paste.
            textChanges.Add(new TextChange(
                change.OldSpan.ToTextSpan(),
                EscapeForNonRawStringLiteral(change.NewText)));
        }

        return textChanges.ToImmutableAndClear();
    }

    private ImmutableArray<TextChange> GetEditsForRawString()
    {
        // Can't really figure anything out if the raw string is in error.
        if (NodeOrTokenContainsError(StringExpressionBeforePaste))
            return default;

        // If all we're going to do is insert whitespace, then don't make any adjustments to the text. We don't want
        // to end up inserting nothing and having the user very confused why their paste did nothing.
        if (AllWhitespace(SnapshotBeforePaste.Version.Changes))
            return default;

        // if the content we're going to add itself contains quotes, then figure out how many start/end quotes the
        // final string literal will need (which also gives us the number of quotes to add to the start/end).
        //
        // note: we don't have to do this if the paste was successful.  Instead, we'll just process the contents,
        // adjusting whitespace below.
        var quotesToAdd = _pasteWasSuccessful ? null : GetQuotesToAddToRawString();
        var dollarSignsToAdd = _pasteWasSuccessful ? null : GetDollarSignsToAddToRawString();

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);

        // First, add any extra dollar signs needed.
        if (dollarSignsToAdd != null)
            edits.Add(new TextChange(new TextSpan(StringExpressionBeforePaste.Span.Start, 0), dollarSignsToAdd));

        // Then any quotes to the start delimiter.
        if (quotesToAdd != null)
            edits.Add(new TextChange(new TextSpan(StringExpressionBeforePasteInfo.ContentSpans.First().Start, 0), quotesToAdd));

        // Then add the actual changes in the content.

        if (IsAnyMultiLineRawStringExpression(StringExpressionBeforePaste))
            AdjustWhitespaceAndAddTextChangesForMultiLineRawStringLiteral(edits);
        else
            AdjustWhitespaceAndAddTextChangesForSingleLineRawStringLiteral(edits);

        // Then  any extra quotes to the end delimiter.
        if (quotesToAdd != null)
            edits.Add(new TextChange(new TextSpan(StringExpressionBeforePasteInfo.EndDelimiterSpanWithoutSuffix.End, 0), quotesToAdd));

        return edits.ToImmutableAndClear();
    }

    /// <inheritdoc cref="AbstractPasteProcessor.GetQuotesToAddToRawString(SourceText, ImmutableArray{TextSpan})" />
    private string? GetQuotesToAddToRawString()
        => GetQuotesToAddToRawString(TextAfterPaste, TextContentsSpansAfterPaste);

    /// <inheritdoc cref="AbstractPasteProcessor.GetDollarSignsToAddToRawString(SourceText, ImmutableArray{TextSpan})" />
    private string? GetDollarSignsToAddToRawString()
        => GetDollarSignsToAddToRawString(TextAfterPaste, TextContentsSpansAfterPaste);

    // Pasting with single line case.

    private void AdjustWhitespaceAndAddTextChangesForSingleLineRawStringLiteral(ArrayBuilder<TextChange> edits)
    {
        // When pasting into a single-line raw literal we will keep it a single line if we can.  If the content
        // we're pasting starts/ends with a quote, or contains a newline, then we have to convert to a multiline.
        //
        // Pasting any other content into a single-line raw literal is always legal and needs no extra work on our
        // part.

        var mustBeMultiLine = RawContentMustBeMultiLine(TextAfterPaste, TextContentsSpansAfterPaste);

        using var _ = PooledStringBuilder.GetInstance(out var buffer);

        // A newline and the indentation to start with.
        if (mustBeMultiLine)
            edits.Add(new TextChange(new TextSpan(StringExpressionBeforePasteInfo.StartDelimiterSpan.End, 0), NewLine + IndentationWhitespace));

        // Only if we're ending with a multi-line raw string do we want to consider the first line when determining
        // the common indentation prefix to trim out.  If we don't have a multi-line raw string, then that means we
        // pasted a boring single-line string into a single-line raw string, and in that case, we don't want to touch
        // the contents at all.
        var commonIndentationPrefix = SpansContainsNewLine(TextAfterPaste, TextContentsSpansAfterPaste)
            ? GetCommonIndentationPrefix(Changes) ?? ""
            : "";

        SourceText? textOfCurrentChange = null;
        foreach (var change in Changes)
        {
            // Create a text object around the change text we're making.  This is a very simple way to get
            // a nice view of the text lines in the change.
            textOfCurrentChange = SourceText.From(change.NewText);

            buffer.Clear();

            for (var i = 0; i < textOfCurrentChange.Lines.Count; i++)
            {
                // The actual full line that was pasted in.
                var currentChangeLine = textOfCurrentChange.Lines[i];
                var fullChangeLineText = textOfCurrentChange.ToString(currentChangeLine.SpanIncludingLineBreak);

                if (i == 0)
                {
                    // on the first line, remove the common indentation if we can. Otherwise leave alone.
                    if (fullChangeLineText.StartsWith(commonIndentationPrefix, StringComparison.OrdinalIgnoreCase))
                        buffer.Append(fullChangeLineText[commonIndentationPrefix.Length..]);
                    else
                        buffer.Append(fullChangeLineText);
                }
                else
                {
                    // on all the rest of the lines, always remove the common indentation.
                    buffer.Append(fullChangeLineText[commonIndentationPrefix.Length..]);
                }

                // if we ended with a newline, make sure the next line is indented enough.
                if (HasNewLine(currentChangeLine))
                    buffer.Append(IndentationWhitespace);
            }

            edits.Add(new TextChange(change.OldSpan.ToTextSpan(), buffer.ToString()));
        }

        // if the last change ended at the closing delimiter *and* ended with a newline, then we don't need to add a
        // final newline-space at the end because we will have already done that.
        if (mustBeMultiLine && !LastPastedLineAddedNewLine())
            edits.Add(new TextChange(new TextSpan(StringExpressionBeforePasteInfo.EndDelimiterSpan.Start, 0), NewLine + IndentationWhitespace));

        return;

        bool LastPastedLineAddedNewLine()
        {
            return textOfCurrentChange != null &&
                Changes.Last().OldEnd == StringExpressionBeforePasteInfo.ContentSpans.Last().End &&
                  HasNewLine(textOfCurrentChange.Lines.Last());
        }
    }

    // Pasting into multi line case.

    private void AdjustWhitespaceAndAddTextChangesForMultiLineRawStringLiteral(
        ArrayBuilder<TextChange> edits)
    {
        var endLine = TextBeforePaste.Lines.GetLineFromPosition(StringExpressionBeforePaste.Span.End);

        // The indentation whitespace every line of the final raw string needs.
        var indentationWhitespace = endLine.GetLeadingWhitespace();

        using var _ = PooledStringBuilder.GetInstance(out var buffer);

        var commonIndentationPrefix = GetCommonIndentationPrefix(Changes);

        for (int changeIndex = 0, lastChangeIndex = Changes.Count; changeIndex < lastChangeIndex; changeIndex++)
        {
            var change = Changes[changeIndex];

            // Create a text object around the change text we're making.  This is a very simple way to get
            // a nice view of the text lines in the change.
            var textOfCurrentChange = SourceText.From(change.NewText);
            buffer.Clear();

            for (int lineIndex = 0, lastLineIndex = textOfCurrentChange.Lines.Count; lineIndex < lastLineIndex; lineIndex++)
            {
                var firstChange = changeIndex == 0 && lineIndex == 0;
                var lastChange = (changeIndex == lastChangeIndex - 1) && (lineIndex == lastLineIndex - 1);

                // The actual full line that was pasted in.
                var currentChangeLine = textOfCurrentChange.Lines[lineIndex];
                var fullChangeLineText = textOfCurrentChange.ToString(currentChangeLine.SpanIncludingLineBreak);
                // The contents of the line, with all leading whitespace removed.
                var (lineLeadingWhitespace, lineWithoutLeadingWhitespace) = ExtractWhitespace(fullChangeLineText);

                // This entire if-chain is only concerned with inserting the necessary whitespace a line should have.

                if (firstChange)
                {
                    // The first line is often special.  It may be copied without any whitespace (e.g. the user
                    // starts their selection at the start of text on that line, not the start of the line itself).
                    // So we use some heuristics to try to decide what to do depending on how much whitespace we see
                    // on that first copied line.

                    TextBeforePaste.GetLineAndOffset(change.OldSpan.Start, out var line, out var offset);

                    // First, ensure that the indentation whitespace of the *inserted* first line is sufficient.
                    if (line == TextBeforePaste.Lines.GetLineFromPosition(StringExpressionBeforePaste.SpanStart).LineNumber)
                    {
                        // if the first chunk was pasted into the space after the first `"""` then we need to actually
                        // insert a newline, then the indentation whitespace, then the first line of the change.
                        buffer.Append(NewLine);
                        buffer.Append(indentationWhitespace);
                    }
                    else if (offset < indentationWhitespace.Length)
                    {
                        // On the first line, we were pasting into the indentation whitespace.  Ensure we add enough
                        // whitespace so that the trimmed line starts at an acceptable position.
                        buffer.Append(indentationWhitespace[offset..]);
                    }

                    // Now, we want to actually insert any whitespace the line itself should have *if* it's got more
                    // than the common indentation whitespace.
                    if (commonIndentationPrefix != null && lineLeadingWhitespace.StartsWith(commonIndentationPrefix, StringComparison.OrdinalIgnoreCase))
                        buffer.Append(lineLeadingWhitespace[commonIndentationPrefix.Length..]);
                }
                else if (!lastChange && lineWithoutLeadingWhitespace.Length > 0 && SyntaxFacts.IsNewLine(lineWithoutLeadingWhitespace[0]))
                {
                    // if it's just an empty line, don't bother adding any whitespace at all.  This will just end up
                    // inserting the blank line here.  We don't do this on the last line as we want to still insert
                    // the right amount of indentation so that the user's caret is placed properly in the text.  We
                    // could technically not insert the whitespace and attempt to place the caret using a virtual
                    // position, but this adds a lot of complexity to this system, so we avoid that for now and go
                    // for the simpler approach..
                }
                else
                {
                    // On any other line we're adding, ensure we have enough indentation whitespace to proceed.
                    // Add the necessary whitespace the literal needs, then add the line contents without 
                    // the common whitespace included.
                    buffer.Append(indentationWhitespace);
                    if (commonIndentationPrefix != null)
                        buffer.Append(lineLeadingWhitespace[commonIndentationPrefix.Length..]);
                }

                // After the necessary whitespace has been added, add the actual non-whitespace contents of the
                // line.
                buffer.Append(lineWithoutLeadingWhitespace);

                if (lastChange)
                {
                    // Similar to the check we do for the first-change, if the last change was pasted into the space
                    // before the last `"""` then we need potentially insert a newline, then enough indentation
                    // whitespace to keep delimiter in the right location.

                    TextBeforePaste.GetLineAndOffset(change.OldSpan.End, out var line, out var offset);

                    if (line == TextBeforePaste.Lines.GetLineFromPosition(StringExpressionBeforePaste.Span.End).LineNumber)
                    {
                        if (!HasNewLine(currentChangeLine))
                            buffer.Append(NewLine);

                        buffer.Append(TextBeforePaste.ToString(new TextSpan(TextBeforePaste.Lines[line].Start, offset)));
                    }
                }
            }

            edits.Add(new TextChange(change.OldSpan.ToTextSpan(), buffer.ToString()));
        }
    }
}
