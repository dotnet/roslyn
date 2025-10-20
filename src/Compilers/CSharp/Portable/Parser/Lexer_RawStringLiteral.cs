// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        /// <returns>The number of quotes that were consumed</returns>
        private int ConsumeCharSequence(char ch)
        {
            var start = TextWindow.Position;
            while (TextWindow.PeekChar() == ch)
                TextWindow.AdvanceChar();

            return TextWindow.Position - start;
        }

        private int ConsumeQuoteSequence()
            => ConsumeCharSequence('"');

        private int ConsumeDollarSignSequence()
            => ConsumeCharSequence('$');

        private int ConsumeAtSignSequence()
            => ConsumeCharSequence('@');

        private int ConsumeOpenBraceSequence()
            => ConsumeCharSequence('{');

        private int ConsumeCloseBraceSequence()
            => ConsumeCharSequence('}');

        private void ConsumeWhitespace(StringBuilder? builder)
        {
            while (true)
            {
                var ch = TextWindow.PeekChar();
                if (!SyntaxFacts.IsWhitespace(ch))
                    break;

                builder?.Append(ch);
                TextWindow.AdvanceChar();
            }
        }

        private bool IsAtEndOfText(char currentChar)
            => currentChar == SlidingTextWindow.InvalidCharacter && TextWindow.IsReallyAtEnd();

        private void ScanRawStringLiteral(ref TokenInfo info, bool inDirective)
        {
            _builder.Length = 0;

            var startingQuoteCount = ConsumeQuoteSequence();

            Debug.Assert(startingQuoteCount >= 3);

            // Keep consuming whitespace after the initial quote sequence.
            ConsumeWhitespace(builder: null);

            if (SyntaxFacts.IsNewLine(TextWindow.PeekChar()))
            {
                // Past the initial whitespace, and we hit a newline, this is a multi line raw string literal.
                ScanMultiLineRawStringLiteral(ref info, startingQuoteCount);
            }
            else
            {
                // Past the initial whitespace, and we hit anything else, this is a single line raw string literal.
                ScanSingleLineRawStringLiteral(ref info, startingQuoteCount);
            }

            Debug.Assert(info.StringValue == null);
            Debug.Assert(info.Kind is (SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken));

            if (!inDirective && ScanUtf8Suffix())
            {
                switch (info.Kind)
                {
                    case SyntaxKind.SingleLineRawStringLiteralToken:
                        info.Kind = SyntaxKind.Utf8SingleLineRawStringLiteralToken;
                        break;

                    case SyntaxKind.MultiLineRawStringLiteralToken:
                        info.Kind = SyntaxKind.Utf8MultiLineRawStringLiteralToken;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(info.Kind);
                }
            }

            info.StringValue = info.Text = this.GetInternedLexemeText();
        }

        private void ScanSingleLineRawStringLiteral(ref TokenInfo info, int startingQuoteCount)
        {
            info.Kind = SyntaxKind.SingleLineRawStringLiteralToken;

            while (true)
            {
                var currentChar = TextWindow.PeekChar();

                // See if we reached the end of the line or file before hitting the end.
                if (SyntaxFacts.IsNewLine(currentChar))
                {
                    this.AddError(TextWindow.Position, width: TextWindow.GetNewLineWidth(), ErrorCode.ERR_UnterminatedRawString);
                    return;
                }
                else if (IsAtEndOfText(currentChar))
                {
                    this.AddError(TextWindow.Position, width: 0, ErrorCode.ERR_UnterminatedRawString);
                    return;
                }

                if (currentChar != '"')
                {
                    // anything not a quote sequence just moves it forward.
                    TextWindow.AdvanceChar();
                    continue;
                }

                var currentQuoteCount = ConsumeQuoteSequence();

                // A raw string literal starting with some number of quotes can contain a quote sequence with fewer quotes.
                if (currentQuoteCount < startingQuoteCount)
                    continue;

                // A raw string could never be followed by another string.  So once we've consumed all the closing quotes
                // if we have any more closing quotes then that's an error we can give a message for.
                if (currentQuoteCount > startingQuoteCount)
                {
                    var excessQuoteCount = currentQuoteCount - startingQuoteCount;
                    this.AddError(
                        position: TextWindow.Position - excessQuoteCount,
                        width: excessQuoteCount,
                        ErrorCode.ERR_TooManyQuotesForRawString);
                }

                // We have enough quotes to finish this string at this point.
                return;
            }
        }

        private void ScanMultiLineRawStringLiteral(ref TokenInfo info, int startingQuoteCount)
        {
            info.Kind = SyntaxKind.MultiLineRawStringLiteralToken;

            Debug.Assert(SyntaxFacts.IsNewLine(TextWindow.PeekChar()));
            while (ScanMultiLineRawStringLiteralLine(startingQuoteCount))
                ;
        }

        private bool ScanMultiLineRawStringLiteralLine(int startingQuoteCount)
        {
            TextWindow.AdvancePastNewLine();

            ConsumeWhitespace(builder: null);

            // after the whitespace see if this the line that ends the multiline literal.
            var currentQuoteCount = ConsumeQuoteSequence();
            if (currentQuoteCount >= startingQuoteCount)
            {
                // A raw string could never be followed by another string.  So once we've consumed all the closing quotes
                // if we have any more closing quotes then that's an error we can give a message for.
                if (currentQuoteCount > startingQuoteCount)
                {
                    var excessQuoteCount = currentQuoteCount - startingQuoteCount;
                    this.AddError(
                        position: TextWindow.Position - excessQuoteCount,
                        width: excessQuoteCount,
                        ErrorCode.ERR_TooManyQuotesForRawString);
                }

                // Done scanning lines.
                return false;
            }

            // We're not on the terminating line. Consume a normal content line.  Eat to the end of line (or file in the
            // case of errors).
            while (true)
            {
                var currentChar = TextWindow.PeekChar();
                if (IsAtEndOfText(currentChar))
                {
                    this.AddError(TextWindow.Position, width: 0, ErrorCode.ERR_UnterminatedRawString);
                    return false;
                }

                if (SyntaxFacts.IsNewLine(currentChar))
                    return true;

                if (currentChar == '"')
                {
                    // Don't allow a content line to contain a quote sequence that looks like a delimiter (or longer)
                    currentQuoteCount = ConsumeQuoteSequence();
                    if (currentQuoteCount >= startingQuoteCount)
                    {
                        this.AddError(
                            position: TextWindow.Position - currentQuoteCount,
                            width: currentQuoteCount,
                            ErrorCode.ERR_RawStringDelimiterOnOwnLine);
                        return false;
                    }
                }
                else
                {
                    TextWindow.AdvanceChar();
                }
            }
        }
    }
}
