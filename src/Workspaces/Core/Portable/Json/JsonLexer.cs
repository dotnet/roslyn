// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VirtualChars;

namespace Microsoft.CodeAnalysis.Json
{
    using System;
    using static JsonHelpers;

    internal struct JsonLexer
    {
        public readonly ImmutableArray<VirtualChar> Text;
        public int Position;

        public JsonLexer(ImmutableArray<VirtualChar> text) : this()
        {
            Text = text;
        }

        public VirtualChar CurrentChar => Position < Text.Length ? Text[Position] : new VirtualChar((char)0, default);

        public ImmutableArray<VirtualChar> GetSubPattern(int start, int end)
        {
            var result = ArrayBuilder<VirtualChar>.GetInstance(end - start);
            for (var i = start; i < end; i++)
            {
                result.Add(Text[i]);
            }

            return result.ToImmutableAndFree();
        }

        public JsonToken ScanNextToken()
        {
            var leadingTrivia = ScanTrivia(leading: true);
            if (Position == Text.Length)
            {
                return new JsonToken(
                    JsonKind.EndOfFile, leadingTrivia, 
                    ImmutableArray<VirtualChar>.Empty, ImmutableArray<JsonTrivia>.Empty);
            }

            var (chars, kind, diagnostic) = ScanNextTokenWorker();
            Debug.Assert(chars.Length > 0);

            var trailingTrivia = ScanTrivia(leading: false);
            var token = new JsonToken(kind, leadingTrivia, chars, trailingTrivia);

            if (diagnostic != null)
            {
                token = token.AddDiagnosticIfNone(diagnostic.Value);
            }

            return token;
        }

        private bool IsSpecial(char ch)
        {
            // Standard tokens.
            switch (ch)
            {
                case '{': case '}':
                case '[': case ']':
                case ',': case ':': 
                case '\'': case '"': 
                    return true;

                case ' ': case '\t': case '/': case '\r': case '\n':
                    // trivia cases
                    return true;
            }

            // more trivia
            if (char.IsWhiteSpace(ch))
            {
                return true;
            }

            return false;
        }

        private (ImmutableArray<VirtualChar>, JsonKind, JsonDiagnostic? diagnostic) ScanNextTokenWorker()
        {
            Debug.Assert(Position < Text.Length);
            switch (this.CurrentChar)
            {
                case '{': return ScanSingleCharToken(JsonKind.OpenBraceToken);
                case '}': return ScanSingleCharToken(JsonKind.CloseBraceToken);
                case '[': return ScanSingleCharToken(JsonKind.OpenBracketToken);
                case ']': return ScanSingleCharToken(JsonKind.CloseBracketToken);
                case ',': return ScanSingleCharToken(JsonKind.CommaToken);
                case ':': return ScanSingleCharToken(JsonKind.ColonToken);

                case '\'': case '"':
                    return ScanString();

                //case '-': case '.':
                //case '0': case '1': case '2': case '3': case '4':
                //case '5': case '6': case '7': case '8': case '9':
                //    return ScanNumber();

                default:
                    return ScanText();
            }
        }

        private (ImmutableArray<VirtualChar>, JsonKind, JsonDiagnostic?) ScanString()
        {
            var start = Position;
            var startChar = this.CurrentChar.Char;
            Position++;

            JsonDiagnostic? diagnostic = null;
            while (Position < Text.Length)
            {
                var currentCh = this.CurrentChar.Char;

                Position++;
                switch (currentCh)
                {
                    case '"':
                    case '\'':
                        if (currentCh == startChar)
                        {
                            return (GetSubPattern(start, Position), JsonKind.StringToken, diagnostic);
                        }
                        continue;

                    case '\\':
                        var escapeDiag = ScanEscape(start, Position - 1);
                        diagnostic = diagnostic ?? escapeDiag;
                        continue;
                }
            }

            var chars = GetSubPattern(start, Position);
            diagnostic = diagnostic ?? new JsonDiagnostic(
                WorkspacesResources.Unterminated_string, GetSpan(chars));
            return (chars, JsonKind.StringToken, diagnostic);
        }

        private JsonDiagnostic? ScanEscape(int stringStart, int escapeStart)
        {
            if (this.Position == Text.Length)
            {
                var chars = GetSubPattern(stringStart, Position);
                return new JsonDiagnostic(WorkspacesResources.Unterminated_string, GetSpan(chars));
            }

            var currentCh = this.CurrentChar;
            switch (currentCh)
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
                    var chars = GetSubPattern(escapeStart, Position);
                    return new JsonDiagnostic(WorkspacesResources.Invalid_escape_sequence, GetSpan(chars));
            }
        }

        private JsonDiagnostic? ScanUnicodeChars(int escapeStart, int unicodeCharStart)
        {
            var invalid = false;
            for (int i = 0; this.Position < Text.Length && i < 4; i++)
            {
                var ch = this.CurrentChar;
                Position++;

                invalid |= !IsHexDigit(ch);
            }

            if (invalid || (Position - unicodeCharStart != 4))
            {
                var chars = GetSubPattern(escapeStart, Position);
                return new JsonDiagnostic(WorkspacesResources.Invalid_escape_sequence, GetSpan(chars));
            }

            return null;
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }

        //private (ImmutableArray<VirtualChar>, JsonKind, JsonDiagnostic?) ScanNumber()
        //{
        //    var result = TryScanText("-Infinity", JsonKind.NegativeInfinityLiteralToken);
        //    if (result != null)
        //    {
        //        return result.Value;
        //    }

        //    var start = this.Position;
        //    while (Position < Text.Length && IsNumberChar(this.CurrentChar))
        //    {
        //        this.Position++;
        //    }

        //    var chars = GetSubPattern(start, this.Position);
        //    var diagnostic = CheckNumberChars(chars);

        //    // Validation of the number happens during the CheckSyntax phase.
        //    return (GetSubPattern(start, this.Position), JsonKind.NumberToken, diagnostic);
        //}

        //private JsonDiagnostic? CheckNumberChars(ImmutableArray<VirtualChar> chars)
        //{
        //    var firstChar = chars[0].Char;

        //    var singleDigit = char.IsDigit(firstChar) && chars.Length == 1;
        //    if (singleDigit)
        //    {
        //        return null;
        //    }

        //    var nonBase10 =
        //        firstChar == '0' && chars.Length > 1 &&
        //        chars[1] != '.' && chars[1] != 'e' && chars[1] != 'E';

        //    var literalText = new string(chars.Select(vc => vc.Char).ToArray());

        //    if (nonBase10)
        //    {
        //        Debug.Assert(chars.Length > 1);
        //        var b = chars[1] == 'x' || chars[2] == 'X' ? 16 : 8;

        //        try
        //        {
        //            Convert.ToInt64(literalText.Substring(2), b);
        //        }
        //        catch (Exception)
        //        {
        //            return new JsonDiagnostic(
        //                WorkspacesResources.Invalid_number,
        //                GetSpan(chars));
        //        }
        //    }
        //    else if (!double.TryParse(literalText, out _))
        //    {
        //        return new JsonDiagnostic(
        //            WorkspacesResources.Invalid_number,
        //            GetSpan(chars));
        //    }

        //    return null;
        //}

        private static bool IsNumberChar(char ch)
        {
            switch (ch)
            {
                case 'x': case 'X':
                case '-': case '+': case '.':
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                case 'a': case 'b': case 'c': case 'd': case 'e': case 'f':
                case 'A': case 'B': case 'C': case 'D': case 'E': case 'F':
                    return true;
                default:
                    return false;
            }
        }

        private (ImmutableArray<VirtualChar>, JsonKind, JsonDiagnostic?) ScanText()
        {
            var start = Position;
            //var result = TryScanText("true", JsonKind.TrueLiteralToken) ??
            //             TryScanText("false", JsonKind.FalseLiteralToken) ??
            //             TryScanText("null", JsonKind.NullLiteralToken) ??
            //             TryScanText("undefined", JsonKind.UndefinedLiteralToken) ??
            //             TryScanText("NaN", JsonKind.NaNLiteralToken) ??
            //             TryScanText("Infinity", JsonKind.InfinityLiteralToken);
            //if (result != null)
            //{
            //    return result.Value;
            //}

            var firstChar = this.CurrentChar;
            while (Position < Text.Length && !IsSpecial(this.CurrentChar))
            {
                Position++;
            }

            return (GetSubPattern(start, Position), JsonKind.TextToken, null);
        }

        private (ImmutableArray<VirtualChar>, JsonKind, JsonDiagnostic?)? TryScanText(
            string text, JsonKind kind)
        {
            var start = this.Position;
            if (!IsAt(text))
            {
                return null;
            }

            Position += text.Length;
            return (GetSubPattern(start, Position), kind, null);
        }

        private (ImmutableArray<VirtualChar>, JsonKind, JsonDiagnostic?) ScanSingleCharToken(JsonKind kind)
        {
            var chars = ImmutableArray.Create(this.CurrentChar);
            Position++;
            return (chars, kind, null);
        }

        private ImmutableArray<JsonTrivia> ScanTrivia(bool leading)
        {
            var result = ArrayBuilder<JsonTrivia>.GetInstance();

            var start = Position;

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
                return new JsonTrivia(JsonKind.EndOfLineTrivia, GetSubPattern(start, Position));
            }
            else if (IsAt("\r") || IsAt("\n"))
            {
                Position++;
                return new JsonTrivia(JsonKind.EndOfLineTrivia, GetSubPattern(start, Position));
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

                var chars = GetSubPattern(start, Position);
                return new JsonTrivia(JsonKind.SingleLineCommentTrivia, chars,
                    ImmutableArray.Create(new JsonDiagnostic(
                        WorkspacesResources.Error_parsing_comment,
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

            var chars = GetSubPattern(start, Position);
            if (Position == start + 2)
            {
                var diagnostics = ImmutableArray.Create(new JsonDiagnostic(
                    WorkspacesResources.Unterminated_comment,
                    GetSpan(chars)));
                return new JsonTrivia(JsonKind.SingleLineCommentTrivia, chars, diagnostics);
            }

            return new JsonTrivia(JsonKind.SingleLineCommentTrivia, chars);
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
                return new JsonTrivia(JsonKind.MultiLineCommentTrivia, GetSubPattern(start, Position));
            }

            Debug.Assert(Position == Text.Length);
            return new JsonTrivia(JsonKind.MultiLineCommentTrivia, GetSubPattern(start, Position),
                ImmutableArray.Create(new JsonDiagnostic(
                    WorkspacesResources.Unterminated_comment,
                    GetTextSpan(start, Position))));
        }

        public TextSpan GetTextSpan(int startInclusive, int endExclusive)
            => TextSpan.FromBounds(Text[startInclusive].Span.Start, Text[endExclusive - 1].Span.End);

        public bool IsAt(string val)
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
            while (Position < Text.Length && 
                   char.IsWhiteSpace(this.CurrentChar))
            {
                Position++;
            }

            if (Position > start)
            {
                return new JsonTrivia(JsonKind.WhitespaceTrivia, GetSubPattern(start, Position));
            }

            return null;
        }
    }
}
