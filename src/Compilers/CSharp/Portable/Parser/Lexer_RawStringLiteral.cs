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
            Debug.Assert(TextWindow.PeekChar() == '"');

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
                if (SyntaxFacts.IsNewLine(currentChar) || IsAtEndOfText(currentChar))
                {
                    this.AddError(this.TextWindow.Position, width: 1, ErrorCode.ERR_Unterminated_raw_string_literal);
                    info.StringValue = "";
                    return;
                }

                if (currentChar != '"')
                {
                    // anything not a quote sequence just moves it forward.
                    TextWindow.AdvanceChar();
                    continue;
                }

                var beforeEndDelimeter = TextWindow.Position;
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
                var afterStartDelimeter = this.TextWindow.LexemeStartPosition + startingQuoteCount;
                var valueLength = beforeEndDelimeter - afterStartDelimeter;

                info.StringValue = this.TextWindow.GetText(
                    position: afterStartDelimeter,
                    length: valueLength,
                    intern: true);
                return;
            }
        }

        private void ScanMultiLineRawStringLiteral(ref TokenInfo info, int startingQuoteCount)
        {
            info.Kind = SyntaxKind.MultiLineRawStringLiteralToken;

            var indentationWhitespace = PooledStringBuilder.GetInstance();
            var currentLineWhitespace = PooledStringBuilder.GetInstance();
            try
            {
                // Do the first pass, finding the end of the raw string.
                var afterStartDelimeter = this.TextWindow.Position;
                Debug.Assert(SyntaxFacts.IsNewLine(this.TextWindow.PeekChar()));

                var lineCount = 0;
                while (ScanMultiLineRawStringLiteralLine(
                    startingQuoteCount,
                    indentationWhitespace.Builder))
                {
                    lineCount++;
                }

                // If the initial scan failed then just bail out without a constant value.
                if (this.HasErrors)
                    return;

                // The last line will be the `        """` line and will not count toward content.
                var contentLineCount = lineCount - 1;

                // Now, do the second pass, building up the literal value.  This may produce an error as well if the
                // indentation whitespace of the lines isn't complimentary.
                var tokenEnd = this.TextWindow.Position;
                this.TextWindow.Reset(afterStartDelimeter);
                Debug.Assert(SyntaxFacts.IsNewLine(this.TextWindow.PeekChar()));

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

                info.StringValue = this.HasErrors ? "" : this.TextWindow.Intern(_builder);
                this.TextWindow.Reset(tokenEnd);
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
            TextWindow.AdvanceChar();

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
                        position: this.TextWindow.Position - excessQuoteCount,
                        width: excessQuoteCount,
                        ErrorCode.ERR_Too_many_closing_quotes_for_raw_string_literal);
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
                    this.AddError(this.TextWindow.Position, width: 1, ErrorCode.ERR_Unterminated_raw_string_literal);
                    return false;
                }

                if (SyntaxFacts.IsNewLine(currentChar))
                    return true;

                TextWindow.AdvanceChar();
            }
        }

        private void AddMultiLineRawStringLiteralLineContents(
            StringBuilder indentationWhitespace,
            StringBuilder currentLineWhitespace,
            bool firstContentLine)
        {
            Debug.Assert(SyntaxFacts.IsNewLine(TextWindow.PeekChar()));

            // the initial newline in `"""   \r\n` is not added to the contents.
            if (!firstContentLine)
                _builder.Append(TextWindow.PeekChar());

            TextWindow.AdvanceChar();

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

                // this whitespace line is longer then the indentation whitespace.  Include everything past the matching
                // prefix to the string value.  If it is the same length or shorter, then ignore the contents of the
                // blank line.
                if (currentLineWhitespace.Length > indentationWhitespace.Length)
                {
                    for (var i = indentationWhitespace.Length; i < currentLineWhitespace.Length; i++)
                        _builder.Append(currentLineWhitespace[i]);
                }

                return;
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

                // Skip the leading whitespace that matches the terminator line and add any whitespace past that to the
                // string value.
                for (var i = indentationWhitespace.Length; i < currentLineWhitespace.Length; i++)
                    _builder.Append(currentLineWhitespace[i]);
            }

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
        private bool StartsWith(StringBuilder sb, StringBuilder value)
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
