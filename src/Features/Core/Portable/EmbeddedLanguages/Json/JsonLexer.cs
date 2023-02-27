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

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
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

        public readonly VirtualChar CurrentChar => Text[Position];

        public readonly VirtualCharSequence GetCharsToCurrentPosition(int start)
            => GetSubSequence(start, Position);

        public readonly VirtualCharSequence GetSubSequence(int start, int end)
            => Text.GetSubSequence(TextSpan.FromBounds(start, end));

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

            return diagnostic == null
                ? token
                : token.AddDiagnosticIfNone(diagnostic.Value);
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
                        if (currentCh.Value == openChar.Value)
                            return (GetCharsToCurrentPosition(start), JsonKind.StringToken, diagnostic);

                        continue;

                    case '\\':
                        var escapeDiag = AdvanceToEndOfEscape(start, escapeStart: Position - 1);
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
        /// <see cref="AdvanceToEndOfEscape"/> does not actually lex out an escape token.  Instead, it just moves the
        /// position forward and returns a diagnostic if this was not a valid escape.
        /// </summary>
        private EmbeddedDiagnostic? AdvanceToEndOfEscape(int stringStart, int escapeStart)
        {
            if (this.Position == Text.Length)
            {
                var chars = GetCharsToCurrentPosition(stringStart);
                return new EmbeddedDiagnostic(FeaturesResources.Unterminated_string, GetSpan(chars));
            }

            var currentCh = this.CurrentChar;
            Position++;

            return currentCh.Value switch
            {
                'b' or 't' or 'n' or 'f' or 'r' or '\\' or '"' or '\'' or '/' => null,
                'u' => ScanUnicodeChars(escapeStart, Position),
                _ => new EmbeddedDiagnostic(FeaturesResources.Invalid_escape_sequence, GetSpan(GetCharsToCurrentPosition(escapeStart))),
            };
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
                => ch.Value switch
                {
                    // Standard tokens.
                    '{' or '}' or '[' or ']' or '(' or ')' or ',' or ':' or '\'' or '"' => true,
                    // trivia cases
                    ' ' or '\t' or '/' or '\r' or '\n' => true,
                    // more trivia
                    _ => ch.IsWhiteSpace,
                };
        }

        private (VirtualCharSequence, JsonKind, EmbeddedDiagnostic?) ScanSingleCharToken(JsonKind kind)
        {
            var chars = this.Text.GetSubSequence(new TextSpan(Position, 1));
            Position++;
            return (chars, kind, null);
        }

        private ImmutableArray<JsonTrivia> ScanTrivia(bool leading)
        {
            using var _ = ArrayBuilder<JsonTrivia>.GetInstance(out var result);

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

            return result.ToImmutable();
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
                    new EmbeddedDiagnostic(FeaturesResources.Error_parsing_comment, GetSpan(chars)));
            }

            return null;
        }

        private JsonTrivia ScanSingleLineComment()
        {
            Debug.Assert(IsAt("//"));
            var start = Position;
            Position += 2;

            while (Position < Text.Length && this.CurrentChar.Value is not '\r' and not '\n')
                Position++;

            var chars = GetCharsToCurrentPosition(start);
            if (Position == start + 2)
            {
                // Note: json.net reports an error if the file ends with "//", so we just
                // preserve that behavior.
                return CreateTrivia(JsonKind.SingleLineCommentTrivia, chars,
                    new EmbeddedDiagnostic(FeaturesResources.Unterminated_comment, GetSpan(chars)));
            }

            return CreateTrivia(JsonKind.SingleLineCommentTrivia, chars);
        }

        private JsonTrivia ScanMultiLineComment()
        {
            Debug.Assert(IsAt("/*"));
            var start = Position;
            Position += 2;

            while (Position < Text.Length && !IsAt("*/"))
                Position++;

            if (IsAt("*/"))
            {
                Position += 2;
                return CreateTrivia(JsonKind.MultiLineCommentTrivia, GetCharsToCurrentPosition(start));
            }

            Debug.Assert(Position == Text.Length);
            return CreateTrivia(JsonKind.MultiLineCommentTrivia, GetCharsToCurrentPosition(start),
                new EmbeddedDiagnostic(FeaturesResources.Unterminated_comment, GetTextSpan(start, Position)));
        }

        private readonly TextSpan GetTextSpan(int startInclusive, int endExclusive)
            => TextSpan.FromBounds(Text[startInclusive].Span.Start, Text[endExclusive - 1].Span.End);

        private readonly bool IsAt(string val)
            => TextAt(this.Position, val);

        private readonly bool TextAt(int position, string val)
        {
            for (var i = 0; i < val.Length; i++)
            {
                if (position + i >= Text.Length || Text[position + i] != val[i])
                    return false;
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
