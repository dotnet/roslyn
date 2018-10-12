// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private void ScanStringLiteral(ref TokenInfo info, bool allowEscapes = true)
        {
            var quoteCharacter = TextWindow.PeekChar();
            if (quoteCharacter == '\'' || quoteCharacter == '"')
            {
                TextWindow.AdvanceChar();
                _builder.Length = 0;
                while (true)
                {
                    char ch = TextWindow.PeekChar();
                    if (ch == '\\' && allowEscapes)
                    {
                        // normal string & char constants can have escapes
                        char c2;
                        ch = this.ScanEscapeSequence(out c2);
                        _builder.Append(ch);
                        if (c2 != SlidingTextWindow.InvalidCharacter)
                        {
                            _builder.Append(c2);
                        }
                    }
                    else if (ch == quoteCharacter)
                    {
                        TextWindow.AdvanceChar();
                        break;
                    }
                    else if (SyntaxFacts.IsNewLine(ch) ||
                            (ch == SlidingTextWindow.InvalidCharacter && TextWindow.IsReallyAtEnd()))
                    {
                        //String and character literals can contain any Unicode character. They are not limited
                        //to valid UTF-16 characters. So if we get the SlidingTextWindow's sentinel value,
                        //double check that it was not real user-code contents. This will be rare.
                        Debug.Assert(TextWindow.Width > 0);
                        this.AddError(ErrorCode.ERR_NewlineInConst);
                        break;
                    }
                    else
                    {
                        TextWindow.AdvanceChar();
                        _builder.Append(ch);
                    }
                }

                info.Text = TextWindow.GetText(true);
                if (quoteCharacter == '\'')
                {
                    info.Kind = SyntaxKind.CharacterLiteralToken;
                    if (_builder.Length != 1)
                    {
                        this.AddError((_builder.Length != 0) ? ErrorCode.ERR_TooManyCharsInConst : ErrorCode.ERR_EmptyCharConst);
                    }

                    if (_builder.Length > 0)
                    {
                        info.StringValue = TextWindow.Intern(_builder);
                        info.CharValue = info.StringValue[0];
                    }
                    else
                    {
                        info.StringValue = string.Empty;
                        info.CharValue = SlidingTextWindow.InvalidCharacter;
                    }
                }
                else
                {
                    info.Kind = SyntaxKind.StringLiteralToken;
                    if (_builder.Length > 0)
                    {
                        info.StringValue = TextWindow.Intern(_builder);
                    }
                    else
                    {
                        info.StringValue = string.Empty;
                    }
                }
            }
            else
            {
                info.Kind = SyntaxKind.None;
                info.Text = null;
            }
        }

        private char ScanEscapeSequence(out char surrogateCharacter)
        {
            var start = TextWindow.Position;
            surrogateCharacter = SlidingTextWindow.InvalidCharacter;
            char ch = TextWindow.NextChar();
            Debug.Assert(ch == '\\');

            ch = TextWindow.NextChar();
            switch (ch)
            {
                // escaped characters that translate to themselves
                case '\'':
                case '"':
                case '\\':
                    break;
                // translate escapes as per C# spec 2.4.4.4
                case '0':
                    ch = '\u0000';
                    break;
                case 'a':
                    ch = '\u0007';
                    break;
                case 'b':
                    ch = '\u0008';
                    break;
                case 'f':
                    ch = '\u000c';
                    break;
                case 'n':
                    ch = '\u000a';
                    break;
                case 'r':
                    ch = '\u000d';
                    break;
                case 't':
                    ch = '\u0009';
                    break;
                case 'v':
                    ch = '\u000b';
                    break;
                case 'x':
                case 'u':
                case 'U':
                    TextWindow.Reset(start);
                    SyntaxDiagnosticInfo error;
                    ch = TextWindow.NextUnicodeEscape(surrogateCharacter: out surrogateCharacter, info: out error);
                    AddError(error);
                    break;
                default:
                    this.AddError(start, TextWindow.Position - start, ErrorCode.ERR_IllegalEscape);
                    break;
            }

            return ch;
        }

        private void ScanVerbatimStringLiteral(ref TokenInfo info, bool allowNewlines = true)
        {
            _builder.Length = 0;

            if (TextWindow.PeekChar() == '@' && TextWindow.PeekChar(1) == '"')
            {
                TextWindow.AdvanceChar(2);
                bool done = false;
                char ch;
                _builder.Length = 0;
                while (!done)
                {
                    switch (ch = TextWindow.PeekChar())
                    {
                        case '"':
                            TextWindow.AdvanceChar();
                            if (TextWindow.PeekChar() == '"')
                            {
                                // Doubled quote -- skip & put the single quote in the string
                                TextWindow.AdvanceChar();
                                _builder.Append(ch);
                            }
                            else
                            {
                                done = true;
                            }

                            break;

                        case SlidingTextWindow.InvalidCharacter:
                            if (!TextWindow.IsReallyAtEnd())
                            {
                                goto default;
                            }

                            // Reached the end of the source without finding the end-quote.  Give
                            // an error back at the starting point.
                            this.AddError(ErrorCode.ERR_UnterminatedStringLit);
                            done = true;
                            break;

                        default:
                            if (!allowNewlines && SyntaxFacts.IsNewLine(ch))
                            {
                                this.AddError(ErrorCode.ERR_UnterminatedStringLit);
                                done = true;
                                break;
                            }

                            TextWindow.AdvanceChar();
                            _builder.Append(ch);
                            break;
                    }
                }

                info.Kind = SyntaxKind.StringLiteralToken;
                info.Text = TextWindow.GetText(false);
                info.StringValue = _builder.ToString();
            }
            else
            {
                info.Kind = SyntaxKind.None;
                info.Text = null;
                info.StringValue = null;
            }
        }

        private void ScanInterpolatedStringLiteral(bool isVerbatim, ref TokenInfo info)
        {
            // We have a string of the form
            //                $" ... "
            // or, if isVerbatim is true, of possible forms
            //                $@" ... "
            //                @$" ... "
            // Where the contents contains zero or more sequences
            //                { STUFF }
            // where these curly braces delimit STUFF in expression "holes".
            // In order to properly find the closing quote of the whole string,
            // we need to locate the closing brace of each hole, as strings
            // may appear in expressions in the holes. So we
            // need to match up any braces that appear between them.
            // But in order to do that, we also need to match up any
            // /**/ comments, ' characters quotes, () parens
            // [] brackets, and "" strings, including interpolated holes in the latter.

            SyntaxDiagnosticInfo error = null;
            bool closeQuoteMissing;
            ScanInterpolatedStringLiteralTop(null, isVerbatim, ref info, ref error, out closeQuoteMissing);
            this.AddError(error);
        }

        internal void ScanInterpolatedStringLiteralTop(ArrayBuilder<Interpolation> interpolations, bool isVerbatim, ref TokenInfo info, ref SyntaxDiagnosticInfo error, out bool closeQuoteMissing)
        {
            var subScanner = new InterpolatedStringScanner(this, isVerbatim);
            subScanner.ScanInterpolatedStringLiteralTop(interpolations, ref info, out closeQuoteMissing);
            error = subScanner.error;
            info.Text = TextWindow.GetText(false);
        }

        internal struct Interpolation
        {
            public readonly int OpenBracePosition;
            public readonly int ColonPosition;
            public readonly int CloseBracePosition;
            public readonly bool CloseBraceMissing;
            public bool ColonMissing => ColonPosition <= 0;
            public bool HasColon => ColonPosition > 0;
            public int LastPosition => CloseBraceMissing ? CloseBracePosition - 1 : CloseBracePosition;
            public int FormatEndPosition => CloseBracePosition - 1;
            public Interpolation(int openBracePosition, int colonPosition, int closeBracePosition, bool closeBraceMissing)
            {
                this.OpenBracePosition = openBracePosition;
                this.ColonPosition = colonPosition;
                this.CloseBracePosition = closeBracePosition;
                this.CloseBraceMissing = closeBraceMissing;
            }
        }

        /// <summary>
        /// Turn a (parsed) interpolated string nonterminal into an interpolated string token.
        /// </summary>
        /// <param name="interpolatedString"></param>
        static internal SyntaxToken RescanInterpolatedString(InterpolatedStringExpressionSyntax interpolatedString)
        {
            var text = interpolatedString.ToString();
            var kind = SyntaxKind.InterpolatedStringToken;
            // TODO: scan the contents (perhaps using ScanInterpolatedStringLiteralContents) to reconstruct any lexical
            // errors such as // inside an expression hole
            return SyntaxFactory.Literal(
                interpolatedString.GetFirstToken().GetLeadingTrivia(),
                text,
                kind,
                text,
                interpolatedString.GetLastToken().GetTrailingTrivia());
        }

        private class InterpolatedStringScanner
        {
            public readonly Lexer lexer;
            public bool isVerbatim;
            public bool allowNewlines;
            public SyntaxDiagnosticInfo error;
            public InterpolatedStringScanner(
                Lexer lexer,
                bool isVerbatim)
            {
                this.lexer = lexer;
                this.isVerbatim = isVerbatim;
                this.allowNewlines = isVerbatim;
            }

            private bool IsAtEnd()
            {
                return IsAtEnd(isVerbatim && allowNewlines);
            }

            private bool IsAtEnd(bool allowNewline)
            {
                char ch = lexer.TextWindow.PeekChar();
                return
                    !allowNewline && SyntaxFacts.IsNewLine(ch) ||
                    (ch == SlidingTextWindow.InvalidCharacter && lexer.TextWindow.IsReallyAtEnd());
            }

            internal void ScanInterpolatedStringLiteralTop(ArrayBuilder<Interpolation> interpolations, ref TokenInfo info, out bool closeQuoteMissing)
            {
                if (isVerbatim)
                {
                    Debug.Assert(
                        (lexer.TextWindow.PeekChar() == '@' && lexer.TextWindow.PeekChar(1) == '$') ||
                        (lexer.TextWindow.PeekChar() == '$' && lexer.TextWindow.PeekChar(1) == '@'));

                    // @$ or $@
                    lexer.TextWindow.AdvanceChar();
                    lexer.TextWindow.AdvanceChar();
                }
                else
                {
                    Debug.Assert(lexer.TextWindow.PeekChar() == '$');
                    lexer.TextWindow.AdvanceChar(); // $
                }

                Debug.Assert(lexer.TextWindow.PeekChar() == '"');
                lexer.TextWindow.AdvanceChar(); // "
                ScanInterpolatedStringLiteralContents(interpolations);
                if (lexer.TextWindow.PeekChar() != '"')
                {
                    Debug.Assert(IsAtEnd());
                    if (error == null)
                    {
                        int position = IsAtEnd(true) ? lexer.TextWindow.Position - 1 : lexer.TextWindow.Position;
                        error = lexer.MakeError(position, 1, isVerbatim ? ErrorCode.ERR_UnterminatedStringLit : ErrorCode.ERR_NewlineInConst);
                    }

                    closeQuoteMissing = true;
                }
                else
                {
                    // found the closing quote
                    lexer.TextWindow.AdvanceChar(); // "
                    closeQuoteMissing = false;
                }

                info.Kind = SyntaxKind.InterpolatedStringToken;
            }

            private void ScanInterpolatedStringLiteralContents(ArrayBuilder<Interpolation> interpolations)
            {
                while (true)
                {
                    if (IsAtEnd())
                    {
                        // error: end of line before end of string
                        return;
                    }

                    switch (lexer.TextWindow.PeekChar())
                    {
                        case '"':
                            if (isVerbatim && lexer.TextWindow.PeekChar(1) == '"')
                            {
                                lexer.TextWindow.AdvanceChar(); // "
                                lexer.TextWindow.AdvanceChar(); // "
                                continue;
                            }
                            // found the end of the string
                            return;
                        case '}':
                            var pos = lexer.TextWindow.Position;
                            lexer.TextWindow.AdvanceChar(); // }
                            // ensure any } characters are doubled up
                            if (lexer.TextWindow.PeekChar() == '}')
                            {
                                lexer.TextWindow.AdvanceChar(); // }
                            }
                            else if (error == null)
                            {
                                error = lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "}");
                            }
                            continue;
                        case '{':
                            if (lexer.TextWindow.PeekChar(1) == '{')
                            {
                                lexer.TextWindow.AdvanceChar();
                                lexer.TextWindow.AdvanceChar();
                            }
                            else
                            {
                                int openBracePosition = lexer.TextWindow.Position;
                                lexer.TextWindow.AdvanceChar();
                                int colonPosition = 0;
                                ScanInterpolatedStringLiteralHoleBalancedText('}', true, ref colonPosition);
                                int closeBracePosition = lexer.TextWindow.Position;
                                bool closeBraceMissing = false;
                                if (lexer.TextWindow.PeekChar() == '}')
                                {
                                    lexer.TextWindow.AdvanceChar();
                                }
                                else
                                {
                                    closeBraceMissing = true;
                                    if (error == null)
                                    {
                                        error = lexer.MakeError(openBracePosition - 1, 2, ErrorCode.ERR_UnclosedExpressionHole);
                                    }
                                }

                                interpolations?.Add(new Interpolation(openBracePosition, colonPosition, closeBracePosition, closeBraceMissing));
                            }
                            continue;
                        case '\\':
                            if (isVerbatim)
                            {
                                goto default;
                            }

                            var escapeStart = lexer.TextWindow.Position;
                            char c2;
                            char ch = lexer.ScanEscapeSequence(out c2);
                            if ((ch == '{' || ch == '}') && error == null)
                            {
                                error = lexer.MakeError(escapeStart, lexer.TextWindow.Position - escapeStart, ErrorCode.ERR_EscapedCurly, ch);
                            }

                            continue;
                        default:
                            // found some other character in the string portion
                            lexer.TextWindow.AdvanceChar();
                            continue;
                    }
                }
            }

            private void ScanFormatSpecifier()
            {
                Debug.Assert(lexer.TextWindow.PeekChar() == ':');
                lexer.TextWindow.AdvanceChar();
                while (true)
                {
                    char ch = lexer.TextWindow.PeekChar();
                    if (ch == '\\' && !isVerbatim)
                    {
                        // normal string & char constants can have escapes
                        var pos = lexer.TextWindow.Position;
                        char c2;
                        ch = lexer.ScanEscapeSequence(out c2);
                        if ((ch == '{' || ch == '}') && error == null)
                        {
                            error = lexer.MakeError(pos, 1, ErrorCode.ERR_EscapedCurly, ch);
                        }
                    }
                    else if (ch == '"')
                    {
                        if (isVerbatim && lexer.TextWindow.PeekChar(1) == '"')
                        {
                            lexer.TextWindow.AdvanceChar();
                            lexer.TextWindow.AdvanceChar();
                        }
                        else
                        {
                            return; // premature end of string! let caller complain about unclosed interpolation
                        }
                    }
                    else if (ch == '{')
                    {
                        var pos = lexer.TextWindow.Position;
                        lexer.TextWindow.AdvanceChar();
                        // ensure any { characters are doubled up
                        if (lexer.TextWindow.PeekChar() == '{')
                        {
                            lexer.TextWindow.AdvanceChar(); // {
                        }
                        else if (error == null)
                        {
                            error = lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "{");
                        }
                    }
                    else if (ch == '}')
                    {
                        if (lexer.TextWindow.PeekChar(1) == '}')
                        {
                            lexer.TextWindow.AdvanceChar();
                            lexer.TextWindow.AdvanceChar();
                        }
                        else
                        {
                            return; // end of interpolation
                        }
                    }
                    else if (IsAtEnd())
                    {
                        return; // premature end; let caller complain
                    }
                    else
                    {
                        lexer.TextWindow.AdvanceChar();
                    }
                }
            }

            /// <summary>
            /// Scan past the hole inside an interpolated string literal, leaving the current character on the '}' (if any)
            /// </summary>
            private void ScanInterpolatedStringLiteralHoleBalancedText(char endingChar, bool isHole, ref int colonPosition)
            {
                while (true)
                {
                    if (IsAtEnd())
                    {
                        // the caller will complain
                        return;
                    }

                    char ch = lexer.TextWindow.PeekChar();
                    switch (ch)
                    {
                        case '#':
                            // preprocessor directives not allowed.
                            if (error == null)
                            {
                                error = lexer.MakeError(lexer.TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString());
                            }

                            lexer.TextWindow.AdvanceChar();
                            continue;
                        case '$':
                            if (lexer.TextWindow.PeekChar(1) == '"' || lexer.TextWindow.PeekChar(1) == '@' && lexer.TextWindow.PeekChar(2) == '"')
                            {
                                bool isVerbatimSubstring = lexer.TextWindow.PeekChar(1) == '@';
                                var interpolations = default(ArrayBuilder<Interpolation>);
                                var info = default(TokenInfo);
                                bool wasVerbatim = this.isVerbatim;
                                bool wasAllowNewlines = this.allowNewlines;
                                try
                                {
                                    this.isVerbatim = isVerbatimSubstring;
                                    this.allowNewlines &= isVerbatim;
                                    bool closeQuoteMissing;
                                    ScanInterpolatedStringLiteralTop(interpolations, ref info, out closeQuoteMissing);
                                }
                                finally
                                {
                                    this.isVerbatim = wasVerbatim;
                                    this.allowNewlines = wasAllowNewlines;
                                }
                                continue;
                            }

                            goto default;
                        case ':':
                            // the first colon not nested within matching delimiters is the start of the format string
                            if (isHole)
                            {
                                Debug.Assert(colonPosition == 0);
                                colonPosition = lexer.TextWindow.Position;
                                ScanFormatSpecifier();
                                return;
                            }

                            goto default;
                        case '}':
                        case ')':
                        case ']':
                            if (ch == endingChar)
                            {
                                return;
                            }

                            if (error == null)
                            {
                                error = lexer.MakeError(lexer.TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString());
                            }

                            goto default;
                        case '"':
                        case '\'':
                            // handle string or character literal inside an expression hole.
                            ScanInterpolatedStringLiteralNestedString();
                            continue;
                        case '@':
                            if (lexer.TextWindow.PeekChar(1) == '"')
                            {
                                // check for verbatim string inside an expression hole.
                                ScanInterpolatedStringLiteralNestedVerbatimString();
                                continue;
                            }

                            goto default;
                        case '/':
                            switch (lexer.TextWindow.PeekChar(1))
                            {
                                case '/':
                                    if (isVerbatim && allowNewlines)
                                    {
                                        lexer.TextWindow.AdvanceChar(); // skip /
                                        lexer.TextWindow.AdvanceChar(); // skip /
                                        while (!IsAtEnd(false))
                                        {
                                            lexer.TextWindow.AdvanceChar(); // skip // comment character
                                        }
                                    }
                                    else
                                    {
                                        // error: single-line comment not allowed in an interpolated string
                                        if (error == null)
                                        {
                                            error = lexer.MakeError(lexer.TextWindow.Position, 2, ErrorCode.ERR_SingleLineCommentInExpressionHole);
                                        }

                                        lexer.TextWindow.AdvanceChar();
                                        lexer.TextWindow.AdvanceChar();
                                    }
                                    continue;
                                case '*':
                                    // check for and scan /* comment */
                                    ScanInterpolatedStringLiteralNestedComment();
                                    continue;
                                default:
                                    lexer.TextWindow.AdvanceChar();
                                    continue;
                            }
                        case '{':
                            // TODO: after the colon this has no special meaning.
                            ScanInterpolatedStringLiteralHoleBracketed('{', '}');
                            continue;
                        case '(':
                            // TODO: after the colon this has no special meaning.
                            ScanInterpolatedStringLiteralHoleBracketed('(', ')');
                            continue;
                        case '[':
                            // TODO: after the colon this has no special meaning.
                            ScanInterpolatedStringLiteralHoleBracketed('[', ']');
                            continue;
                        default:
                            // part of code in the expression hole
                            lexer.TextWindow.AdvanceChar();
                            continue;
                    }
                }
            }

            private void ScanInterpolatedStringLiteralNestedComment()
            {
                Debug.Assert(lexer.TextWindow.PeekChar() == '/');
                lexer.TextWindow.AdvanceChar();
                Debug.Assert(lexer.TextWindow.PeekChar() == '*');
                lexer.TextWindow.AdvanceChar();
                while (true)
                {
                    if (IsAtEnd())
                    {
                        return; // let the caller complain about the unterminated quote
                    }

                    var ch = lexer.TextWindow.PeekChar();
                    lexer.TextWindow.AdvanceChar();
                    if (ch == '*' && lexer.TextWindow.PeekChar() == '/')
                    {
                        lexer.TextWindow.AdvanceChar(); // skip */
                        return;
                    }
                }
            }

            private void ScanInterpolatedStringLiteralNestedString()
            {
                var discarded = default(TokenInfo);
                lexer.ScanStringLiteral(ref discarded, true);
            }

            private void ScanInterpolatedStringLiteralNestedVerbatimString()
            {
                var discarded = default(TokenInfo);
                lexer.ScanVerbatimStringLiteral(ref discarded, allowNewlines: allowNewlines);
            }

            private void ScanInterpolatedStringLiteralHoleBracketed(char start, char end)
            {
                Debug.Assert(start == lexer.TextWindow.PeekChar());
                lexer.TextWindow.AdvanceChar();
                int colon = 0;
                ScanInterpolatedStringLiteralHoleBalancedText(end, false, ref colon);
                if (lexer.TextWindow.PeekChar() == end)
                {
                    lexer.TextWindow.AdvanceChar();
                }
                else
                {
                    // an error was given by the caller
                }
            }
        }
    }
}
