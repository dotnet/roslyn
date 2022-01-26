// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json
{
    using static EmbeddedSyntaxHelpers;
    using static JsonHelpers;

    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

    [NonCopyable]
    internal struct JsonLexer
    {
        public readonly VirtualCharSequence Text;
        public int Position;

        public JsonLexer(VirtualCharSequence text) : this()
        {
            Text = text;
        }

        public VirtualChar CurrentChar
        {
            get
            {
                if (Position < Text.Length)
                    return Text[Position];

                Debug.Fail("Indexed past the end of the content");
                return default;
            }
        }

        public VirtualCharSequence GetCharsToCurrentPosition(int start)
            => this.Text.GetSubSequence(TextSpan.FromBounds(start, Position));

        public JsonToken ScanNextToken()
        {
            var leadingTrivia = ScanTrivia(leading: true);
            if (Position == Text.Length)
            {
                return CreateToken(
                    JsonKind.EndOfFile, leadingTrivia,
                    VirtualCharSequence.Empty, ImmutableArray<JsonTrivia>.Empty);
            }

            var (chars, kind, diagnostic) = ScanNextTokenWorker();
            Debug.Assert(chars.Length > 0);

            var trailingTrivia = ScanTrivia(leading: false);
            var token = CreateToken(kind, leadingTrivia, chars, trailingTrivia);

            if (diagnostic != null)
            {
                token = token.AddDiagnosticIfNone(diagnostic.Value);
            }

            return token;
        }

        private (VirtualCharSequence, JsonKind, EmbeddedDiagnostic? diagnostic) ScanNextTokenWorker()
        {
            Debug.Assert(Position < Text.Length);
            return this.CurrentChar.Value switch
            {
                '{' => ScanSingleCharToken(JsonKind.OpenBraceToken),
                '}' => ScanSingleCharToken(JsonKind.CloseBraceToken),
                '[' => ScanSingleCharToken(JsonKind.OpenBracketToken),
                ']' => ScanSingleCharToken(JsonKind.CloseBracketToken),
                '(' => ScanSingleCharToken(JsonKind.OpenParenToken),
                ')' => ScanSingleCharToken(JsonKind.CloseParenToken),
                ',' => ScanSingleCharToken(JsonKind.CommaToken),
                ':' => ScanSingleCharToken(JsonKind.ColonToken),
                '\'' or '"' => ScanString(),
                // It would be tempting to try to scan out numbers here.  However, numbers are
                // actually quite tricky to get right (especially looking one character at a time).
                // So, instead, we take a page from json.net and just consume out a text sequence.
                // Later on, we'll analyze that text sequence as a whole to see if it looks like a
                // number and to also report any issues in line with how json.net and ecmascript
                // handle json numbers.
                _ => ScanText(),
            };
        }

        private (VirtualCharSequence, JsonKind, EmbeddedDiagnostic?) ScanString()
        {
            var start = Position;
            var openChar = this.CurrentChar;
            Position++;

            EmbeddedDiagnostic? diagnostic = null;
            while (Position < Text.Length)
            {
                var currentCh = this.CurrentChar;

                Position++;
                switch (currentCh.Value)
                {
                    case '"':
                    case '\'':
                        if (currentCh == openChar)
                            return (GetCharsToCurrentPosition(start), JsonKind.StringToken, diagnostic);

                        continue;

                    case '\\':
                        var escapeDiag = ScanEscape(start, Position - 1);
                        diagnostic ??= escapeDiag;
                        continue;
                }
            }

            var chars = GetCharsToCurrentPosition(start);
            diagnostic ??= new EmbeddedDiagnostic(
                FeaturesResources.Unterminated_string, GetSpan(chars));
            return (chars, JsonKind.StringToken, diagnostic);
        }

        /// <summary>
        /// <see cref="ScanEscape"/> does not actually lex out an escape token.  Instead, it just
        /// moves the position forward and returns a diagnostic if this was not a valid escape.
        /// </summary>
        private EmbeddedDiagnostic? ScanEscape(int stringStart, int escapeStart)
        {
            if (this.Position == Text.Length)
            {
                var chars = GetCharsToCurrentPosition(stringStart);
                return new EmbeddedDiagnostic(FeaturesResources.Unterminated_string, GetSpan(chars));
            }

            var currentCh = this.CurrentChar;
            switch (currentCh.Value)
            {
                case 'b':
                case 't':
                case 'n':
                case 'f':
                case 'r':
                case '\\':
                case '"':
                case '\'':
                case '/':
                    Position++;
                    return null;

                case 'u':
                    Position++;
                    return ScanUnicodeChars(escapeStart, Position);

                default:
                    Position++;
                    var chars = GetCharsToCurrentPosition(escapeStart);
                    return new EmbeddedDiagnostic(FeaturesResources.Invalid_escape_sequence, GetSpan(chars));
            }
        }

        private EmbeddedDiagnostic? ScanUnicodeChars(int escapeStart, int unicodeCharStart)
        {
            var invalid = false;
            for (var i = 0; this.Position < Text.Length && i < 4; i++)
            {
                var ch = this.CurrentChar;
                Position++;

                invalid |= !IsHexDigit(ch);
            }

            if (invalid || (Position - unicodeCharStart != 4))
            {
                var chars = GetCharsToCurrentPosition(escapeStart);
                return new EmbeddedDiagnostic(FeaturesResources.Invalid_escape_sequence, GetSpan(chars));
            }

            return null;
        }

        private static bool IsHexDigit(VirtualChar c)
            => c.Value is (>= '0' and <= '9') or
                          (>= 'A' and <= 'F') or
                          (>= 'a' and <= 'f');

        private (VirtualCharSequence, JsonKind, EmbeddedDiagnostic?) ScanText()
        {
            var start = Position;

            while (Position < Text.Length && !IsNotPartOfText(this.CurrentChar))
                Position++;

            return (GetCharsToCurrentPosition(start), JsonKind.TextToken, null);

            static bool IsNotPartOfText(VirtualChar ch)
            {
                // Standard tokens.
                switch (ch.Value)
                {
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case '(':
                    case ')':
                    case ',':
                    case ':':
                    case '\'':
                    case '"':
                        return true;

                    case ' ':
                    case '\t':
                    case '/':
                    case '\r':
                    case '\n':
                        // trivia cases
                        return true;
                }

                // more trivia
                if (ch.IsWhiteSpace)
                    return true;

                return false;
            }
        }

        private (VirtualCharSequence, JsonKind, EmbeddedDiagnostic?) ScanSingleCharToken(JsonKind kind)
        {
            var chars = this.Text.GetSubSequence(new TextSpan(Position, 1));
            Position++;
            return (chars, kind, null);
        }

        private ImmutableArray<JsonTrivia> ScanTrivia(bool leading)
        {
            var result = ArrayBuilder<JsonTrivia>.GetInstance();

            while (Position < Text.Length)
            {
                var comment = ScanComment();
                if (comment != null)
                {
                    result.Add(comment.Value);
                    continue;
                }

                var endOfLine = ScanEndOfLine();
                if (endOfLine != null)
                {
                    result.Add(endOfLine.Value);

                    if (leading)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                var whitespace = ScanWhitespace();
                if (whitespace != null)
                {
                    result.Add(whitespace.Value);
                    continue;
                }

                break;
            }

            return result.ToImmutableAndFree();
        }

        private JsonTrivia? ScanEndOfLine()
        {
            var start = Position;
            if (IsAt("\r\n"))
            {
                Position += 2;
                return CreateTrivia(JsonKind.EndOfLineTrivia, GetCharsToCurrentPosition(start));
            }
            else if (IsAt("\r") || IsAt("\n"))
            {
                Position++;
                return CreateTrivia(JsonKind.EndOfLineTrivia, GetCharsToCurrentPosition(start));
            }

            return null;
        }

        public JsonTrivia? ScanComment()
        {
            if (IsAt("//"))
            {
                return ScanSingleLineComment();
            }
            else if (IsAt("/*"))
            {
                return ScanMultiLineComment();
            }
            else if (IsAt("/"))
            {
                var start = Position;
                Position++;

                var chars = GetCharsToCurrentPosition(start);
                return CreateTrivia(JsonKind.SingleLineCommentTrivia, chars,
                    ImmutableArray.Create(new EmbeddedDiagnostic(
                        FeaturesResources.Error_parsing_comment,
                        GetSpan(chars))));
            }

            return null;
        }

        private JsonTrivia ScanSingleLineComment()
        {
            var start = Position;
            Position += 2;

            while (Position < Text.Length &&
                   this.CurrentChar is var ch &&
                   ch != '\r' && ch != '\n')
            {
                Position++;
            }

            var chars = GetCharsToCurrentPosition(start);
            if (Position == start + 2)
            {
                // Note: json.net reports an error if the file ends with "//", so we just
                // preserve that behavior.
                var diagnostics = ImmutableArray.Create(new EmbeddedDiagnostic(
                    FeaturesResources.Unterminated_comment,
                    GetSpan(chars)));
                return CreateTrivia(JsonKind.SingleLineCommentTrivia, chars, diagnostics);
            }

            return CreateTrivia(JsonKind.SingleLineCommentTrivia, chars);
        }

        private JsonTrivia ScanMultiLineComment()
        {
            var start = Position;
            Position += 2;

            while (Position < Text.Length &&
                   !IsAt("*/"))
            {
                Position++;
            }

            if (IsAt("*/"))
            {
                Position += 2;
                return CreateTrivia(JsonKind.MultiLineCommentTrivia, GetCharsToCurrentPosition(start));
            }

            Debug.Assert(Position == Text.Length);
            return CreateTrivia(JsonKind.MultiLineCommentTrivia, GetCharsToCurrentPosition(start),
                ImmutableArray.Create(new EmbeddedDiagnostic(
                    FeaturesResources.Unterminated_comment,
                    GetTextSpan(start, Position))));
        }

        private TextSpan GetTextSpan(int startInclusive, int endExclusive)
            => TextSpan.FromBounds(Text[startInclusive].Span.Start, Text[endExclusive - 1].Span.End);

        private bool IsAt(string val)
            => TextAt(this.Position, val);

        private bool TextAt(int position, string val)
        {
            for (var i = 0; i < val.Length; i++)
            {
                if (position + i >= Text.Length ||
                    Text[position + i] != val[i])
                {
                    return false;
                }
            }

            return true;
        }

        private JsonTrivia? ScanWhitespace()
        {
            var start = Position;
            while (Position < Text.Length && this.CurrentChar.IsWhiteSpace)
                Position++;

            if (Position > start)
                return CreateTrivia(JsonKind.WhitespaceTrivia, GetCharsToCurrentPosition(start));

            return null;
        }
    }
}
