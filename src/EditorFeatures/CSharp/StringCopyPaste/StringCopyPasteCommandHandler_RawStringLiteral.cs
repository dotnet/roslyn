// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    using static StringCopyPasteHelpers;

    internal partial class StringCopyPasteCommandHandler
    {
        /// <summary>
        /// Given an initial raw string literal, and the changes made to it by the paste, determines how many quotes to
        /// add to the start and end to keep things parsing properly.
        /// </summary>
        private static string? GetQuotesToAddToRawLiteral(
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            LiteralExpressionSyntax stringExpressionBeforePaste)
        {
            var contentSpanBeforePaste = GetRawStringLiteralContentSpan(
                snapshotBeforePaste.AsText(), stringExpressionBeforePaste, out var delimiterQuoteCount);
            var contentSpanAfterPaste = MapSpan(contentSpanBeforePaste, snapshotBeforePaste, snapshotAfterPaste);
            var longestQuoteSequence = GetLongestQuoteSequence(snapshotAfterPaste.GetSpan(contentSpanAfterPaste.ToSpan()));

            var quotesToAddCount = (longestQuoteSequence - delimiterQuoteCount) + 1;
            if (quotesToAddCount <= 0)
                return null;

            var quotesToAdd = new string('"', quotesToAddCount);
            return quotesToAdd;
        }

        private static ImmutableArray<TextChange> GetTextChangesForRawStringLiteral(
            Document documentBeforePaste,
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            LiteralExpressionSyntax stringExpressionBeforePaste,
            string newLine,
            CancellationToken cancellationToken)
        {
            // Can't really figure anything out if the raw string is in error.
            if (NodeOrTokenContainsError(stringExpressionBeforePaste))
                return default;

            // If all we're going to do is insert whitespace, then don't make any adjustments to the text. We don't want
            // to end up inserting nothing and having the user very confused why their paste did nothing.
            if (AllWhitespace(snapshotBeforePaste.Version.Changes))
                return default;

            // if the content we're going to add itself contains quotes, then figure out how many start/end quotes the
            // final string literal will need (which also gives us the number of quotes to add to teh start/end).
            var quotesToAdd = GetQuotesToAddToRawLiteral(snapshotBeforePaste, snapshotAfterPaste, stringExpressionBeforePaste);
            return stringExpressionBeforePaste.Token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken
                ? GetTextChangesForSingleLineRawStringLiteral(documentBeforePaste, snapshotBeforePaste, snapshotAfterPaste, stringExpressionBeforePaste, newLine, quotesToAdd, cancellationToken)
                : GetTextChangesForMultiLineRawStringLiteral(snapshotBeforePaste, stringExpressionBeforePaste, newLine, quotesToAdd);
        }

        // Pasting with single line case.

        private static ImmutableArray<TextChange> GetTextChangesForSingleLineRawStringLiteral(
            Document documentBeforePaste,
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            LiteralExpressionSyntax stringExpressionBeforePaste,
            string newLine,
            string? quotesToAdd,
            CancellationToken cancellationToken)
        {
            // When pasting into a single-line raw literal we will keep it a single line if we can.  If the content
            // we're pasting starts/ends with a quote, or contains a newline, then we have to convert to a multiline.
            //
            // Pasting any other content into a single-line raw literal is always legal and needs no extra work on our
            // part.

            var contentSpan = GetRawStringLiteralContentSpan(snapshotBeforePaste.AsText(), stringExpressionBeforePaste);
            var contentSpanAfterPaste = MapSpan(contentSpan, snapshotBeforePaste, snapshotAfterPaste);
            var mustBeMultiLine = MustBeMultiLine(snapshotAfterPaste.GetSpan(contentSpanAfterPaste.ToSpan()));

            var indentationWhitespace = stringExpressionBeforePaste.Token.GetPreferredIndentation(documentBeforePaste, cancellationToken);

            using var _1 = ArrayBuilder<TextChange>.GetInstance(out var finalTextChanges);
            using var _2 = PooledStringBuilder.GetInstance(out var buffer);

            // First, add any extra quotes if we need them.
            if (quotesToAdd != null)
                finalTextChanges.Add(new TextChange(new TextSpan(stringExpressionBeforePaste.SpanStart, 0), quotesToAdd));

            // Then a newline and the indentation to start with.
            if (mustBeMultiLine)
                finalTextChanges.Add(new TextChange(new TextSpan(contentSpan.Start, 0), newLine + indentationWhitespace));

            SourceText? changeText = null;
            for (var i = 0; i < snapshotBeforePaste.Version.Changes.Count; i++)
            {
                var change = snapshotBeforePaste.Version.Changes[i];

                // Create a text object around the change text we're making.  This is a very simple way to get
                // a nice view of the text lines in the change.
                changeText = SourceText.From(change.NewText);
                var commonIndentationPrefix = GetCommonIndentationPrefix(changeText) ?? "";

                buffer.Clear();

                for (var j = 0; j < changeText.Lines.Count; j++)
                {
                    // The actual full line that was pasted in.
                    var currentChangeLine = changeText.Lines[j];
                    var fullChangeLineText = changeText.ToString(currentChangeLine.SpanIncludingLineBreak);

                    if (j == 0)
                    {
                        // on the first line, remove the common indentation if we can. Otherwise leave alone.
                        if (fullChangeLineText.StartsWith(commonIndentationPrefix))
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
                        buffer.Append(indentationWhitespace);
                }

                finalTextChanges.Add(new TextChange(change.OldSpan.ToTextSpan(), buffer.ToString()));
            }

            // if the last change ended at the closing delimiter *and* ended with a newline, then we don't need to add a
            // final newline-space at the end because we will have already done that.
            if (mustBeMultiLine && !LastPastedLineAddedNewLine())
                finalTextChanges.Add(new TextChange(new TextSpan(contentSpan.End, 0), newLine + indentationWhitespace));

            if (quotesToAdd != null)
                finalTextChanges.Add(new TextChange(new TextSpan(stringExpressionBeforePaste.Span.End, 0), quotesToAdd));

            return finalTextChanges.ToImmutable();

            bool LastPastedLineAddedNewLine()
            {
                return changeText != null &&
                    snapshotBeforePaste.Version.Changes.Last().OldEnd == contentSpan.End &&
                      HasNewLine(changeText.Lines.Last());
            }
        }

        private static bool MustBeMultiLine(SnapshotSpan snapshotSpan)
        {
            // Empty raw string must be multiline.
            if (snapshotSpan.IsEmpty)
                return true;

            // Or if it starts/ends with a quote 
            if (snapshotSpan.Start.GetChar() == '"')
                return true;

            if ((snapshotSpan.End - 1).GetChar() == '"')
                return true;

            // or contains a newline
            for (var i = snapshotSpan.Span.Start; i < snapshotSpan.Span.End; i++)
            {
                if (SyntaxFacts.IsNewLine(snapshotSpan.Snapshot[i]))
                    return true;
            }

            return false;
        }

        // Pasting into multi line case.

        private static ImmutableArray<TextChange> GetTextChangesForMultiLineRawStringLiteral(
            ITextSnapshot snapshotBeforePaste,
            LiteralExpressionSyntax stringExpressionBeforePaste,
            string newLine,
            string? quotesToAdd)
        {
            var textBeforePaste = snapshotBeforePaste.AsText();
            var endLine = textBeforePaste.Lines.GetLineFromPosition(stringExpressionBeforePaste.Span.End);

            // The indentation whitespace every line of the final raw string needs.
            var indentationWhitespace = endLine.GetLeadingWhitespace();

            using var _1 = ArrayBuilder<TextChange>.GetInstance(out var finalTextChanges);
            using var _2 = PooledStringBuilder.GetInstance(out var buffer);

            if (quotesToAdd != null)
                finalTextChanges.Add(new TextChange(new TextSpan(stringExpressionBeforePaste.SpanStart, 0), quotesToAdd));

            for (var i = 0; i < snapshotBeforePaste.Version.Changes.Count; i++)
            {
                var change = snapshotBeforePaste.Version.Changes[i];

                // Create a text object around the change text we're making.  This is a very simple way to get
                // a nice view of the text lines in the change.
                var changeText = SourceText.From(change.NewText);
                buffer.Clear();

                var commonIndentationPrefix = GetCommonIndentationPrefix(changeText);

                for (var j = 0; j < changeText.Lines.Count; j++)
                {
                    var firstChange = i == 0 && j == 0;
                    var lastChange = (i == snapshotBeforePaste.Version.Changes.Count - 1) &&
                                     (j == changeText.Lines.Count - 1);

                    // The actual full line that was pasted in.
                    var currentChangeLine = changeText.Lines[j];
                    var fullChangeLineText = changeText.ToString(currentChangeLine.SpanIncludingLineBreak);
                    // The contents of the line, with all leading whitespace removed.
                    var (lineLeadingWhitespace, lineWithoutLeadingWhitespace) = ExtractWhitespace(fullChangeLineText);

                    // This entire if-chain is only concerned with inserting the necessary whitespace a line should have.

                    if (firstChange)
                    {
                        // The first line is often special.  It may be copied without any whitespace (e.g. the user
                        // starts their selection at the start of text on that line, not the start of the line itself).
                        // So we use some heuristics to try to decide what to do depending on how much whitespace we see
                        // on that first copied line.

                        textBeforePaste.GetLineAndOffset(change.OldSpan.Start, out var line, out var offset);

                        // First, ensure that the indentation whitespace of the *inserted* first line is sufficient.
                        if (line == textBeforePaste.Lines.GetLineFromPosition(stringExpressionBeforePaste.SpanStart).LineNumber)
                        {
                            // if the first chunk was pasted into the space after the first `"""` then we need to actually
                            // insert a newline, then the indentation whitespace, then the first line of the change.
                            buffer.Append(newLine);
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
                        if (commonIndentationPrefix != null && lineLeadingWhitespace.StartsWith(commonIndentationPrefix))
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

                        textBeforePaste.GetLineAndOffset(change.OldSpan.End, out var line, out var offset);

                        if (line == textBeforePaste.Lines.GetLineFromPosition(stringExpressionBeforePaste.Span.End).LineNumber)
                        {
                            if (!HasNewLine(currentChangeLine))
                                buffer.Append(newLine);

                            buffer.Append(textBeforePaste.ToString(new TextSpan(textBeforePaste.Lines[line].Start, offset)));
                        }
                    }
                }

                finalTextChanges.Add(new TextChange(change.OldSpan.ToTextSpan(), buffer.ToString()));
            }

            if (quotesToAdd != null)
                finalTextChanges.Add(new TextChange(new TextSpan(stringExpressionBeforePaste.Span.End, 0), quotesToAdd));

            return finalTextChanges.ToImmutable();
        }
    }
}
