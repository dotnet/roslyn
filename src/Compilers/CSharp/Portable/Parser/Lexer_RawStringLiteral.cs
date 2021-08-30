// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        /// <returns>The number of quotes that were consumed</returns>
        private int ConsumeQuoteSequence()
        {
            var start = TextWindow.Position;
            while (TextWindow.PeekChar() == '"')
                TextWindow.AdvanceChar();

            return TextWindow.Position - start;
        }

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

            // If we encounter any errors while scanning this raw string, then always treat its constant value
            // as unknown.
            if (this.HasErrors)
                info.StringValue = "";

            Debug.Assert(info.StringValue != null);
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
                    this.AddError(TextWindow.Position, width: GetNewLineWidth(currentChar), ErrorCode.ERR_UnterminatedRawString);
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

        private int GetNewLineWidth(char currentChar)
        {
            Debug.Assert(SyntaxFacts.IsNewLine(currentChar));
            return currentChar == '\r' && TextWindow.PeekChar(1) == '\n' ? 2 : 1;
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
                        ErrorCode.ERR_Multi_line_raw_string_literals_must_contain_at_least_one_line_of_content);
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
            Debug.Assert(SyntaxFacts.IsNewLine(TextWindow.PeekChar()));
            TextWindow.AdvanceChar(GetNewLineWidth(TextWindow.PeekChar()));

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
                            ErrorCode.ERR_Raw_string_literal_delimiter_must_be_on_its_own_line);
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

            var newLineWidth = GetNewLineWidth(TextWindow.PeekChar());
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

            if (SyntaxFacts.IsNewLine(TextWindow.PeekChar()))
            {
                // a whitespace-only content line.  The indentation whitespace must be a prefix of the current line whitespace,
                // or vice versa.  It is an error otherwise.
                if (!StartsWith(indentationWhitespace, currentLineWhitespace) &&
                    !StartsWith(currentLineWhitespace, indentationWhitespace))
                {
                    this.AddError(
                        lineStartPosition,
                        width: TextWindow.Position - lineStartPosition,
                        ErrorCode.ERR_Line_does_not_start_with_the_same_whitespace_as_the_last_line_of_the_raw_string_literal);
                    return;
                }
            }
            else
            {
                // a content line with non-whitespace.  The indentation whitespace must be a prefix of the current line
                // whitespace.  It is an error otherwise.
                if (!StartsWith(currentLineWhitespace, indentationWhitespace))
                {
                    this.AddError(
                        lineStartPosition,
                        width: TextWindow.Position - lineStartPosition,
                        ErrorCode.ERR_Line_does_not_start_with_the_same_whitespace_as_the_last_line_of_the_raw_string_literal);
                    return;
                }
            }

            // Skip the leading whitespace that matches the terminator line and add any whitespace past that to the
            // string value.
            for (var i = indentationWhitespace.Length; i < currentLineWhitespace.Length; i++)
                _builder.Append(currentLineWhitespace[i]);

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
