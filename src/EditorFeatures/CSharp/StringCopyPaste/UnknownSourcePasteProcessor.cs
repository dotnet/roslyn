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
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    using static StringCopyPasteHelpers;

    /// <summary>
    /// Paste processor responsible for determining how text should be treated if it came from a source outside of the
    /// editor we're in.  In that case, we don't know what any particular piece of text means.  For example, <c>\t</c>
    /// might be a tab or it could be the literal two characters <c>\</c> and <c>t</c>.
    /// </summary>
    internal class UnknownSourcePasteProcessor : AbstractPasteProcessor
    {
        public UnknownSourcePasteProcessor(
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            Document documentBeforePaste,
            Document documentAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            string newLine,
            IndentationOptions indentationOptions,
            bool pasteWasSuccessful)
            : base(snapshotBeforePaste, snapshotAfterPaste, documentBeforePaste, documentAfterPaste, stringExpressionBeforePaste, newLine, indentationOptions, pasteWasSuccessful)
        {
        }

        public ImmutableArray<TextChange> GetEdits(CancellationToken cancellationToken)
        {
            // If we have a raw-string, then we always want to check for changes to make, even if the paste was
            // technically legal.  This is because we may want to touch up things like indentation to make the
            // pasted text look good for raw strings.
            //
            // Check for certain things we always think we should escape.
            if (!IsAnyRawStringExpression(StringExpressionBeforePaste) && !ShouldAlwaysEscapeTextForNonRawString())
            {
                // If the pasting was successful, then no need to change anything.
                if (PasteWasSuccessful)
                    return default;
            }

            // Ok, the user pasted text that couldn't cleanly be added to this token without issue. Repaste the
            // contents, but this time properly escaped/manipulated so that it follows the rule of the particular token
            // kind.

            // For pastes into non-raw strings, we can just determine how the change should be escaped in-line at that
            // same location the paste originally happened at.  For raw-strings things get more complex as we have to
            // deal with things like indentation and potentially adding newlines to make things legal.
            return IsAnyRawStringExpression(StringExpressionBeforePaste)
                ? GetEditsForRawString(cancellationToken)
                : GetEditsForNonRawString();
        }

        private bool ShouldAlwaysEscapeTextForNonRawString()
        {
            // Pasting a control character into a normal string literal is normally not desired.  So even if this
            // is legal, we still escape the contents to make the pasted code clear.
            return !IsVerbatimStringExpression(StringExpressionBeforePaste) && ContainsControlCharacter(Changes);
        }

        private ImmutableArray<TextChange> GetEditsForNonRawString()
        {
            var isVerbatim = IsVerbatimStringExpression(StringExpressionBeforePaste);
            var isInterpolated = StringExpressionBeforePaste is InterpolatedStringExpressionSyntax;

            using var textChanges = TemporaryArray<TextChange>.Empty;

            foreach (var change in Changes)
                textChanges.Add(new TextChange(change.OldSpan.ToTextSpan(), EscapeForNonRawStringLiteral(isVerbatim, isInterpolated, change.NewText)));

            return textChanges.ToImmutableAndClear();
        }

        /// <summary>
        /// Given an initial raw string literal, and the changes made to it by the paste, determines how many quotes to
        /// add to the start and end to keep things parsing properly.
        /// </summary>
        private string? GetQuotesToAddToRawString()
        {
            Contract.ThrowIfFalse(IsAnyRawStringExpression(StringExpressionBeforePaste));

            var longestQuoteSequence = TextContentsSpansAfterPaste.Max(ts => GetLongestQuoteSequence(TextAfterPaste, ts));

            var quotesToAddCount = (longestQuoteSequence - DelimiterQuoteCount) + 1;
            return quotesToAddCount <= 0 ? null : new string('"', quotesToAddCount);
        }

        /// <summary>
        /// Given an initial raw string literal, and the changes made to it by the paste, determines how many dollar
        /// signs to add to the start to keep things parsing properly.
        /// </summary>
        private string? GetDollarSignsToAddToRawString()
        {
            Contract.ThrowIfFalse(IsAnyRawStringExpression(StringExpressionBeforePaste));

            // Only have to do this for interpolated strings.  Other strings never have a $ in their starting delimiter.
            if (StringExpressionBeforePaste is not InterpolatedStringExpressionSyntax)
                return null;

            var longestBraceSequence = TextContentsSpansAfterPaste.Max(
                ts => Math.Max(
                    GetLongestOpenBraceSequence(TextAfterPaste, ts),
                    GetLongestCloseBraceSequence(TextAfterPaste, ts)));

            var dollarsToAddCount = (longestBraceSequence - DelimiterDollarCount) + 1;
            return dollarsToAddCount <= 0 ? null : new string('$', dollarsToAddCount);
        }

        private ImmutableArray<TextChange> GetEditsForRawString(CancellationToken cancellationToken)
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
            var quotesToAdd = PasteWasSuccessful ? null : GetQuotesToAddToRawString();
            var dollarSignsToAdd = PasteWasSuccessful ? null : GetDollarSignsToAddToRawString();

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var editsToMake);

            // First, add any extra dollar signs needed.
            if (dollarSignsToAdd != null)
                editsToMake.Add(new TextChange(new TextSpan(StringExpressionBeforePaste.Span.Start, 0), dollarSignsToAdd));

            // Then any quotes to your starting delimiter
            if (quotesToAdd != null)
                editsToMake.Add(new TextChange(new TextSpan(TextContentsSpansBeforePaste.First().Start, 0), quotesToAdd));

            // Then add the actual changes in the content.
            var isSingleLine =
                StringExpressionBeforePaste is LiteralExpressionSyntax { Token.RawKind: (int)SyntaxKind.SingleLineRawStringLiteralToken } ||
                StringExpressionBeforePaste is InterpolatedStringExpressionSyntax { StringStartToken.RawKind: (int)SyntaxKind.InterpolatedSingleLineRawStringStartToken };

            if (isSingleLine)
                AdjustWhitespaceAndAddTextChangesForSingleLineRawStringLiteral(editsToMake, cancellationToken);
            else
                AdjustWhitespaceAndAddTextChangesForMultiLineRawStringLiteral(editsToMake);

            // Then add any extra end quotes needed.
            if (quotesToAdd != null)
                editsToMake.Add(new TextChange(new TextSpan(TextContentsSpansBeforePaste.Last().End, 0), quotesToAdd));

            return editsToMake.ToImmutable();
        }

        // Pasting with single line case.

        private void AdjustWhitespaceAndAddTextChangesForSingleLineRawStringLiteral(
            ArrayBuilder<TextChange> editsToMake,
            CancellationToken cancellationToken)
        {
            // When pasting into a single-line raw literal we will keep it a single line if we can.  If the content
            // we're pasting starts/ends with a quote, or contains a newline, then we have to convert to a multiline.
            //
            // Pasting any other content into a single-line raw literal is always legal and needs no extra work on our
            // part.

            var mustBeMultiLine = RawContentMustBeMultiLine(TextAfterPaste, TextContentsSpansAfterPaste);

            var indentationWhitespace = StringExpressionBeforePaste.GetFirstToken().GetPreferredIndentation(DocumentBeforePaste, IndentationOptions, cancellationToken);

            using var _ = PooledStringBuilder.GetInstance(out var buffer);

            // Then a newline and the indentation to start with.
            if (mustBeMultiLine)
                editsToMake.Add(new TextChange(new TextSpan(TextContentsSpansBeforePaste.First().Start, 0), NewLine + indentationWhitespace));

            SourceText? textOfCurrentChange = null;
            var commonIndentationPrefix = GetCommonIndentationPrefix(Changes) ?? "";

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
                        buffer.Append(indentationWhitespace);
                }

                editsToMake.Add(new TextChange(change.OldSpan.ToTextSpan(), buffer.ToString()));
            }

            // if the last change ended at the closing delimiter *and* ended with a newline, then we don't need to add a
            // final newline-space at the end because we will have already done that.
            if (mustBeMultiLine && !LastPastedLineAddedNewLine())
                editsToMake.Add(new TextChange(new TextSpan(TextContentsSpansBeforePaste.Last().End, 0), NewLine + indentationWhitespace));

            return;

            bool LastPastedLineAddedNewLine()
            {
                return textOfCurrentChange != null &&
                    Changes.Last().OldEnd == TextContentsSpansBeforePaste.Last().End &&
                      HasNewLine(textOfCurrentChange.Lines.Last());
            }
        }

        // Pasting into multi line case.

        private void AdjustWhitespaceAndAddTextChangesForMultiLineRawStringLiteral(
            ArrayBuilder<TextChange> editsToMake)
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

                editsToMake.Add(new TextChange(change.OldSpan.ToTextSpan(), buffer.ToString()));
            }
        }
    }
}
