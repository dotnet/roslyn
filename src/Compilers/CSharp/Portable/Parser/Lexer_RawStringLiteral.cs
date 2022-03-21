// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

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

        private void ScanRawStringLiteral(ref TokenInfo info)
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

            // If we encounter any errors while scanning this raw string then we can't really determine the true
            // value of the string.  So just do what we do with the normal strings and treat the contents as the
            // value from after the starting quote to the current position.  Note that for normal strings this will
            // have interpreted things like escape sequences.  However, as we're a raw string and there are no 
            // escapes, we can just grab the text block directly.  This does mean that things like leading indentation
            // will not be stripped, and that multiline raw strings will contain the contents of their first line.
            // However, as this is error code anyways, the interpretation of the value is fine for us to define
            // however we want.  The user can (and should) check for the presence of diagnostics before blindly
            // trusting the contents.
            if (this.HasErrors)
            {
                var afterStartDelimiter = TextWindow.LexemeStartPosition + startingQuoteCount;
                var valueLength = TextWindow.Position - afterStartDelimiter;

                info.StringValue = TextWindow.GetText(
                    position: afterStartDelimiter,
                    length: valueLength,
                    intern: true);
            }
            else
            {
                // If we didn't have an error, the subroutines better have set the string value for this literal.
                Debug.Assert(info.StringValue != null);
            }

            info.Text = TextWindow.GetText(intern: true);
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

                var beforeEndDelimiter = TextWindow.Position;
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
                var afterStartDelimiter = TextWindow.LexemeStartPosition + startingQuoteCount;
                var valueLength = beforeEndDelimiter - afterStartDelimiter;

                info.StringValue = TextWindow.GetText(
                    position: afterStartDelimiter,
                    length: valueLength,
                    intern: true);
                return;
            }
        }

        private void ScanMultiLineRawStringLiteral(ref TokenInfo info, int startingQuoteCount)
        {
            info.Kind = SyntaxKind.MultiLineRawStringLiteralToken;

            // The indentation-whitespace computed from the very last line of the raw string literal
            var indentationWhitespace = PooledStringBuilder.GetInstance();

            // The leading whitespace of whatever line we are currently on.
            var currentLineWhitespace = PooledStringBuilder.GetInstance();
            try
            {
                // Do the first pass, finding the end of the raw string, and determining the 'indentation whitespace'
                // that must be complimentary with all content lines of the raw string literal.
                var afterStartDelimiter = TextWindow.Position;
                Debug.Assert(SyntaxFacts.IsNewLine(TextWindow.PeekChar()));

                var contentLineCount = 0;
                while (ScanMultiLineRawStringLiteralLine(startingQuoteCount, indentationWhitespace.Builder))
                    contentLineCount++;

                // If the initial scan failed then just bail out without a constant value.
                if (this.HasErrors)
                    return;

                // The trivial raw string literal is not legal in the language.
                if (contentLineCount == 0)
                {
                    this.AddError(
                        position: TextWindow.Position - startingQuoteCount,
                        width: startingQuoteCount,
                        ErrorCode.ERR_RawStringMustContainContent);
                    return;
                }

                // Now, do the second pass, building up the literal value.  This may produce an error as well if the
                // indentation whitespace of the lines isn't complimentary.

                // Reset us to right after the starting delimiter.  Note: if we fail to generate a constant value we'll
                // ensure that we reset back to the original end we scanned to above.
                var tokenEnd = TextWindow.Position;
                TextWindow.Reset(afterStartDelimiter);
                Debug.Assert(SyntaxFacts.IsNewLine(TextWindow.PeekChar()));

                for (var currentLine = 0; currentLine < contentLineCount; currentLine++)
                {
                    AddMultiLineRawStringLiteralLineContents(
                        indentationWhitespace.Builder,
                        currentLineWhitespace.Builder,
                        firstContentLine: currentLine == 0);

                    // If processing the line produced errors, then bail out from continued processing.
                    if (this.HasErrors)
                        break;
                }

                info.StringValue = this.HasErrors ? "" : TextWindow.Intern(_builder);

                // Make sure that even if we fail to determine the constant content value of the string that
                // we still consume all the way to original end that we computed.
                TextWindow.Reset(tokenEnd);
            }
            finally
            {
                indentationWhitespace.Free();
                currentLineWhitespace.Free();
            }
        }

        private bool ScanMultiLineRawStringLiteralLine(
            int startingQuoteCount, StringBuilder indentationWhitespace)
        {
            TextWindow.AdvancePastNewLine();

            indentationWhitespace.Clear();
            ConsumeWhitespace(indentationWhitespace);

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

        private void AddMultiLineRawStringLiteralLineContents(
            StringBuilder indentationWhitespace,
            StringBuilder currentLineWhitespace,
            bool firstContentLine)
        {
            Debug.Assert(SyntaxFacts.IsNewLine(TextWindow.PeekChar()));

            var newLineWidth = TextWindow.GetNewLineWidth();
            for (var i = 0; i < newLineWidth; i++)
            {
                // the initial newline in `"""   \r\n` is not added to the contents.
                if (!firstContentLine)
                    _builder.Append(TextWindow.PeekChar());

                TextWindow.AdvanceChar();
            }

            var lineStartPosition = TextWindow.Position;
            currentLineWhitespace.Clear();
            ConsumeWhitespace(currentLineWhitespace);

            if (!StartsWith(currentLineWhitespace, indentationWhitespace))
            {
                // We have a line where the indentation of that line isn't a prefix of indentation
                // whitespace.
                //
                // If we're not on a blank line then this is bad.  That's a content line that doesn't start
                // with the indentation whitespace.  If we are on a blank line then it's ok if the whitespace
                // we do have is a prefix of the indentation whitespace.
                var isBlankLine = SyntaxFacts.IsNewLine(TextWindow.PeekChar());
                var isLegalBlankLine = isBlankLine && StartsWith(indentationWhitespace, currentLineWhitespace);
                if (!isLegalBlankLine)
                {
                    // Specialized error message if this is a spacing difference.
                    if (CheckForSpaceDifference(
                            currentLineWhitespace, indentationWhitespace,
                            out var currentLineWhitespaceChar, out var indentationWhitespaceChar))
                    {
                        this.AddError(
                            lineStartPosition,
                            width: TextWindow.Position - lineStartPosition,
                            ErrorCode.ERR_LineContainsDifferentWhitespace,
                            currentLineWhitespaceChar, indentationWhitespaceChar);
                    }
                    else
                    {
                        this.AddError(
                            lineStartPosition,
                            width: TextWindow.Position - lineStartPosition,
                            ErrorCode.ERR_LineDoesNotStartWithSameWhitespace);
                    }
                    return;
                }
            }

            // Skip the leading whitespace that matches the terminator line and add any whitespace past that to the
            // string value.  Note: if the current line is shorter than the indentation whitespace, this will
            // intentionally copy nothing.
#if NETCOREAPP
            _builder.Append(currentLineWhitespace, startIndex: indentationWhitespace.Length, count: Math.Max(0, currentLineWhitespace.Length - indentationWhitespace.Length));
#else
            for (var i = indentationWhitespace.Length; i < currentLineWhitespace.Length; i++)
                _builder.Append(currentLineWhitespace[i]);
#endif

            // Consume up to the next new line.
            while (true)
            {
                var currentChar = TextWindow.PeekChar();

                if (SyntaxFacts.IsNewLine(currentChar))
                    return;

                _builder.Append(currentChar);
                TextWindow.AdvanceChar();
            }
        }

        private static bool CheckForSpaceDifference(
            StringBuilder currentLineWhitespace,
            StringBuilder indentationLineWhitespace,
            [NotNullWhen(true)] out string? currentLineMessage,
            [NotNullWhen(true)] out string? indentationLineMessage)
        {
            for (int i = 0, n = Math.Min(currentLineWhitespace.Length, indentationLineWhitespace.Length); i < n; i++)
            {
                var currentLineChar = currentLineWhitespace[i];
                var indentationLineChar = indentationLineWhitespace[i];

                if (currentLineChar != indentationLineChar &&
                    SyntaxFacts.IsWhitespace(currentLineChar) &&
                    SyntaxFacts.IsWhitespace(indentationLineChar))
                {
                    currentLineMessage = CharToString(currentLineChar);
                    indentationLineMessage = CharToString(indentationLineChar);
                    return true;
                }
            }

            currentLineMessage = null;
            indentationLineMessage = null;
            return false;
        }

        public static string CharToString(char ch)
        {
            return ch switch
            {
                '\t' => @"\t",
                '\v' => @"\v",
                '\f' => @"\f",
                _ => @$"\u{(int)ch:x4}",
            };
        }

        /// <summary>
        /// Returns true if <paramref name="sb"/> starts with <paramref name="value"/>.
        /// </summary>
        private static bool StartsWith(StringBuilder sb, StringBuilder value)
        {
            if (sb.Length < value.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (sb[i] != value[i])
                    return false;
            }

            return true;
        }
    }
}
