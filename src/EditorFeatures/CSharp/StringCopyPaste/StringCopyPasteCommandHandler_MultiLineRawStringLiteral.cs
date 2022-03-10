// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    using static StringCopyPasteHelpers;

    internal partial class StringCopyPasteCommandHandler
    {
        private static ImmutableArray<TextChange> GetEscapedTextChangesForMultiLineRawStringLiteral(
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            LiteralExpressionSyntax literalExpression,
            INormalizedTextChangeCollection changes,
            string newLine)
        {
            // Can't really figure anything out if the raw string is in error.
            if (NodeOrTokenContainsError(literalExpression))
                return default;

            // If all we're going to do is insert whitespace, then don't make any 
            if (AllWhitespace(changes))
                return default;

            var token = literalExpression.Token;
            var text = snapshotBeforePaste.AsText();
            var endLine = text.Lines.GetLineFromPosition(token.Span.End);

            // The indentation whitespace every line of the final raw string needs.
            var indentationWhitespace = endLine.GetLeadingWhitespace();

            using var _1 = ArrayBuilder<TextChange>.GetInstance(out var finalTextChanges);
            using var _2 = PooledStringBuilder.GetInstance(out var buffer);

            var quotesToAdd = GetQuotesToAddToMultiLineRawLiteral(snapshotBeforePaste, snapshotAfterPaste, literalExpression, text);
            if (quotesToAdd != null)
                finalTextChanges.Add(new TextChange(new TextSpan(literalExpression.SpanStart, 0), quotesToAdd));

            foreach (var change in changes)
            {
                // Create a text object around the change text we're making.  This is a very simple way to get
                // a nice view of the text lines in the change.
                var changeText = SourceText.From(change.NewText);
                buffer.Clear();

                var commonIndentationPrefix = GetCommonIndentationPrefix(changeText);

                for (int i = 0, n = changeText.Lines.Count; i < n; i++)
                {
                    var firstLine = i == 0;
                    var lastLine = i == n - 1;

                    // The actual full line that was pasted in.
                    var fullInitialLine = changeText.ToString(changeText.Lines[i].SpanIncludingLineBreak);
                    // The contents of the line, with all leading whitespace removed.
                    var lineWithoutLeadingWhitespace = TrimStart(fullInitialLine);
                    var lineLeadingWhitespace = fullInitialLine[0..^lineWithoutLeadingWhitespace.Length];

                    // This entire if-chain is only concerned with inserting the necessary whitespace a line should have.

                    if (firstLine)
                    {
                        // The first line is often special.  It may be copied without any whitespace (e.g. the user
                        // starts their selection at the start of text on that line, not the start of the line itself).
                        // So we use some heuristics to try to decide what to do depending on how much whitespace we see
                        // on that first copied line.

                        text.GetLineAndOffset(change.OldSpan.Start, out var line, out var offset);

                        // First, ensure that the indentation whitespace of the *inserted* first line is sufficient.
                        if (line == text.Lines.GetLineFromPosition(literalExpression.SpanStart).LineNumber)
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
                        if (lineLeadingWhitespace.StartsWith(commonIndentationPrefix))
                            buffer.Append(lineLeadingWhitespace[commonIndentationPrefix.Length..]);
                    }
                    else if (!lastLine && lineWithoutLeadingWhitespace.Length > 0 && SyntaxFacts.IsNewLine(lineWithoutLeadingWhitespace[0]))
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
                        buffer.Append(lineLeadingWhitespace[commonIndentationPrefix.Length..]);
                    }

                    // After the necessary whitespace has been added, add the actual non-whitespace contents of the
                    // line.
                    buffer.Append(lineWithoutLeadingWhitespace);
                }

                finalTextChanges.Add(new TextChange(change.OldSpan.ToTextSpan(), buffer.ToString()));
            }

            if (quotesToAdd != null)
                finalTextChanges.Add(new TextChange(new TextSpan(literalExpression.Span.End, 0), quotesToAdd));

            return finalTextChanges.ToImmutable();
        }

        /// <summary>
        /// Given a set of source text lines, determines what common whitespace prefix each line has.  Note that this
        /// does *not* include the first line as it's super common for someone to copy a set of lines while only
        /// starting the selection at the start of the content on the first line.  This also does not include empty
        /// lines as they're also very common, but are clearly not a way of indicating indentation indent for the normal
        /// lines.
        /// </summary>
        private static string GetCommonIndentationPrefix(SourceText text)
        {
            string? commonIndentPrefix = null;

            for (int i = 1, n = text.Lines.Count; i < n; i++)
            {
                var line = text.Lines[i];
                var nonWhitespaceIndex = GetFirstNonWhitespaceIndex(text, line);
                if (nonWhitespaceIndex >= 0)
                    commonIndentPrefix = GetCommonIndentationPrefix(commonIndentPrefix, text, TextSpan.FromBounds(line.Start, nonWhitespaceIndex));
            }

            return commonIndentPrefix ?? "";
        }

        private static string? GetCommonIndentationPrefix(string? commonIndentPrefix, SourceText text, TextSpan lineWhitespaceSpan)
        {
            // first line with indentation whitespace we're seeing.  Just keep track of that.
            if (commonIndentPrefix == null)
                return text.ToString(lineWhitespaceSpan);

            // we have indentation whitespace from a previous line.  Figure out the max commonality between it and the
            // line we're currently looking at.
            var commonPrefixLength = 0;
            for (var n = Math.Min(commonIndentPrefix.Length, lineWhitespaceSpan.Length); commonPrefixLength < n; commonPrefixLength++)
            {
                if (commonIndentPrefix[commonPrefixLength] != text[lineWhitespaceSpan.Start + commonPrefixLength])
                    break;
            }

            return commonIndentPrefix[..commonPrefixLength];
        }

        private static string? GetQuotesToAddToMultiLineRawLiteral(ITextSnapshot snapshotBeforePaste, ITextSnapshot snapshotAfterPaste, LiteralExpressionSyntax literalExpression, SourceText text)
        {
            var contentSpanBeforePaste = GetRawStringLiteralContentSpan(text, literalExpression, out var delimiterQuoteCount);
            var contentSpanAfterPaste = snapshotBeforePaste.CreateTrackingSpan(contentSpanBeforePaste.ToSpan(), SpanTrackingMode.EdgeInclusive)
                                                           .GetSpan(snapshotAfterPaste);
            var longestQuoteSequence = GetLongestQuoteSequence(contentSpanAfterPaste);

            var quotesToAddCount = (longestQuoteSequence - delimiterQuoteCount) + 1;
            if (quotesToAddCount <= 0)
                return null;

            var quotesToAdd = new string('"', quotesToAddCount);
            return quotesToAdd;
        }
    }
}
