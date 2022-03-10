// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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

                for (int i = 0, n = changeText.Lines.Count; i < n; i++)
                {
                    if (i == 0)
                    {
                        text.GetLineAndOffset(change.OldSpan.Start, out var line, out var offset);

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
                    }
                    else
                    {
                        // On any other line we're adding, ensure we have enough indentation whitespace to proceed.
                        buffer.Append(indentationWhitespace);
                    }

                    buffer.Append(TrimStart(changeText.ToString(changeText.Lines[i].SpanIncludingLineBreak)));
                }

                finalTextChanges.Add(new TextChange(change.OldSpan.ToTextSpan(), buffer.ToString()));
            }

            if (quotesToAdd != null)
                finalTextChanges.Add(new TextChange(new TextSpan(literalExpression.Span.End, 0), quotesToAdd));

            return finalTextChanges.ToImmutable();
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
