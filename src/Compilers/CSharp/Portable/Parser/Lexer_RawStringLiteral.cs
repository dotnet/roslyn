// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        /// <returns>The number of quotes that were consumed</returns>
        private int ConsumeQuoteSequence()
        {
            Debug.Assert(TextWindow.PeekChar() == '"');

            int quoteCount = 0;
            while (TextWindow.PeekChar() == '"')
            {
                quoteCount++;
                TextWindow.AdvanceChar();
            }

            return quoteCount;
        }

        private void ScanRawStringLiteral(ref TokenInfo info)
        {
            _builder.Length = 0;

            var startingQuoteCount = ConsumeQuoteSequence();

            Debug.Assert(startingQuoteCount >= 3);
            while (true)
            {
                var currentChar = TextWindow.PeekChar();

                // Keep consuming whitespace after the initial quote sequence.
                if (SyntaxFacts.IsWhitespace(currentChar))
                    continue;

                // See if we reached the end of the file while attempting to consume the whitespace.
                if (currentChar == SlidingTextWindow.InvalidCharacter && TextWindow.IsReallyAtEnd())
                {
                    this.AddError(this.TextWindow.Position, width: 1, ErrorCode.ERR_Unterminated_single_line_raw_string_literal);
                    info.Kind = SyntaxKind.SingleLineRawStringLiteralToken;

                    var valueStartPos = this.TextWindow.LexemeStartPosition + startingQuoteCount;
                    info.StringValue = this.TextWindow.GetText(
                        position: this.TextWindow.LexemeStartPosition + startingQuoteCount,
                        length: this.TextWindow.Text.Length - valueStartPos, intern: true);
                }
                else if (SyntaxFacts.IsNewLine(currentChar))
                {
                    // Past the initial whitespace, and we hit a newline, this is a multi line raw string literal.
                    ScanMultiLineRawStringLiteral(ref info);
                }
                else
                {
                    // Past the initial whitespae, and we hit anything else, this is a single line raw string literal.
                    ScanSingleLineRawStringLiteral(ref info, startingQuoteCount);
                }

                break;
            }

            Debug.Assert(info.StringValue != null);
            info.Text = TextWindow.GetText(intern: true);
        }

        private void ScanSingleLineRawStringLiteral(ref TokenInfo info, int startingQuoteCount)
        {
            info.Kind = SyntaxKind.SingleLineRawStringLiteralToken;

            // The point in the literal token where the value ends (effectively the point prior to the ending quotes).
            int valueEndPosition;
            while (true)
            {
                var currentChar = TextWindow.PeekChar();

                // See if we reached the end of the line or file before hitting the end.
                if (SyntaxFacts.IsNewLine(currentChar) ||
                    (currentChar == SlidingTextWindow.InvalidCharacter && TextWindow.IsReallyAtEnd()))
                {
                    this.AddError(ErrorCode.ERR_Unterminated_single_line_raw_string_literal);
                    valueEndPosition = this.TextWindow.Position;
                    break;
                }

                if (currentChar == '"')
                {
                    valueEndPosition = TextWindow.Position;
                    var currentQuoteCount = ConsumeQuoteSequence();

                    // A raw string literal starting with some number of quotes can contain a quote sequence with less quotes.
                    if (currentQuoteCount < startingQuoteCount)
                        continue;

                    // A raw string could never be followed by another string.  So once we've consumed all the closing quotes
                    // if we have any more closing quotes then that's an error we can give a message for.
                    if (currentQuoteCount > startingQuoteCount)
                    {
                        var excessQuoteCount = currentQuoteCount - startingQuoteCount;
                        this.AddError(
                            position: this.TextWindow.Position - excessQuoteCount,
                            width: excessQuoteCount,
                            ErrorCode.ERR_Too_many_closing_quotes_for_raw_string_literal);
                    }

                    // We have enough quotes to finish this string at this point.
                    break;
                }

                // anything else just moves us forward.
                TextWindow.AdvanceChar();
            }

            var valueStartPosition = this.TextWindow.LexemeStartPosition + startingQuoteCount;
            var valueLength = valueEndPosition - valueStartPosition;

            info.StringValue = this.TextWindow.GetText(
                position: valueStartPosition,
                length: valueLength,
                intern: true);
        }
    }
}
