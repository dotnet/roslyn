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
        private void ScanStringLiteral(ref TokenInfo info, bool inDirective)
        {
            var quoteCharacter = TextWindow.PeekChar();
            Debug.Assert(quoteCharacter == '\'' || quoteCharacter == '"');

            TextWindow.AdvanceChar();
            _builder.Length = 0;

            while (true)
            {
                char ch = TextWindow.PeekChar();

                // Normal string & char constants can have escapes. Strings in directives cannot.
                if (ch == '\\' && !inDirective)
                {
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
        internal static SyntaxToken RescanInterpolatedString(InterpolatedStringExpressionSyntax interpolatedString)
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
            private readonly Lexer _lexer;
            private bool _isVerbatim;
            private bool _allowNewlines;
            public SyntaxDiagnosticInfo error;

            public InterpolatedStringScanner(
                Lexer lexer,
                bool isVerbatim)
            {
                this._lexer = lexer;
                this._isVerbatim = isVerbatim;
                this._allowNewlines = isVerbatim;
            }

            private bool IsAtEnd()
            {
                return IsAtEnd(_isVerbatim && _allowNewlines);
            }

            private bool IsAtEnd(bool allowNewline)
            {
                char ch = _lexer.TextWindow.PeekChar();
                return
                    !allowNewline && SyntaxFacts.IsNewLine(ch) ||
                    (ch == SlidingTextWindow.InvalidCharacter && _lexer.TextWindow.IsReallyAtEnd());
            }

            internal void ScanInterpolatedStringLiteralTop(ArrayBuilder<Interpolation> interpolations, ref TokenInfo info, out bool closeQuoteMissing)
            {
                if (_isVerbatim)
                {
                    Debug.Assert(
                        (_lexer.TextWindow.PeekChar() == '@' && _lexer.TextWindow.PeekChar(1) == '$') ||
                        (_lexer.TextWindow.PeekChar() == '$' && _lexer.TextWindow.PeekChar(1) == '@'));

                    // @$ or $@
                    _lexer.TextWindow.AdvanceChar();
                    _lexer.TextWindow.AdvanceChar();
                }
                else
                {
                    Debug.Assert(_lexer.TextWindow.PeekChar() == '$');
                    _lexer.TextWindow.AdvanceChar(); // $
                }

                Debug.Assert(_lexer.TextWindow.PeekChar() == '"');
                _lexer.TextWindow.AdvanceChar(); // "
                ScanInterpolatedStringLiteralContents(interpolations);
                if (_lexer.TextWindow.PeekChar() != '"')
                {
                    Debug.Assert(IsAtEnd());
                    if (error == null)
                    {
                        int position = IsAtEnd(true) ? _lexer.TextWindow.Position - 1 : _lexer.TextWindow.Position;
                        error = _lexer.MakeError(position, 1, _isVerbatim ? ErrorCode.ERR_UnterminatedStringLit : ErrorCode.ERR_NewlineInConst);
                    }

                    closeQuoteMissing = true;
                }
                else
                {
                    // found the closing quote
                    _lexer.TextWindow.AdvanceChar(); // "
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

                    switch (_lexer.TextWindow.PeekChar())
                    {
                        case '"' when RecoveringFromRunawayLexing():
                            // When recovering from mismatched delimiters, we consume the next
                            // quote character as the close quote for the interpolated string. In
                            // practice this gets us out of trouble in scenarios we've encountered.
                            // See, for example, https://github.com/dotnet/roslyn/issues/44789
                            return;
                        case '"':
                            if (_isVerbatim && _lexer.TextWindow.PeekChar(1) == '"')
                            {
                                _lexer.TextWindow.AdvanceChar(); // "
                                _lexer.TextWindow.AdvanceChar(); // "
                                continue;
                            }
                            // found the end of the string
                            return;
                        case '}':
                            var pos = _lexer.TextWindow.Position;
                            _lexer.TextWindow.AdvanceChar(); // }
                            // ensure any } characters are doubled up
                            if (_lexer.TextWindow.PeekChar() == '}')
                            {
                                _lexer.TextWindow.AdvanceChar(); // }
                            }
                            else if (error == null)
                            {
                                error = _lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "}");
                            }
                            continue;
                        case '{':
                            if (_lexer.TextWindow.PeekChar(1) == '{')
                            {
                                _lexer.TextWindow.AdvanceChar();
                                _lexer.TextWindow.AdvanceChar();
                            }
                            else
                            {
                                int openBracePosition = _lexer.TextWindow.Position;
                                _lexer.TextWindow.AdvanceChar();
                                int colonPosition = 0;
                                ScanInterpolatedStringLiteralHoleBalancedText('}', true, ref colonPosition);
                                int closeBracePosition = _lexer.TextWindow.Position;
                                bool closeBraceMissing = false;
                                if (_lexer.TextWindow.PeekChar() == '}')
                                {
                                    _lexer.TextWindow.AdvanceChar();
                                }
                                else
                                {
                                    closeBraceMissing = true;
                                    if (error == null)
                                    {
                                        error = _lexer.MakeError(openBracePosition - 1, 2, ErrorCode.ERR_UnclosedExpressionHole);
                                    }
                                }

                                interpolations?.Add(new Interpolation(openBracePosition, colonPosition, closeBracePosition, closeBraceMissing));
                            }
                            continue;
                        case '\\':
                            if (_isVerbatim)
                            {
                                goto default;
                            }

                            var escapeStart = _lexer.TextWindow.Position;
                            char c2;
                            char ch = _lexer.ScanEscapeSequence(out c2);
                            if ((ch == '{' || ch == '}') && error == null)
                            {
                                error = _lexer.MakeError(escapeStart, _lexer.TextWindow.Position - escapeStart, ErrorCode.ERR_EscapedCurly, ch);
                            }

                            continue;
                        default:
                            // found some other character in the string portion
                            _lexer.TextWindow.AdvanceChar();
                            continue;
                    }
                }
            }

            private void ScanFormatSpecifier()
            {
                Debug.Assert(_lexer.TextWindow.PeekChar() == ':');
                _lexer.TextWindow.AdvanceChar();
                while (true)
                {
                    char ch = _lexer.TextWindow.PeekChar();
                    if (ch == '\\' && !_isVerbatim)
                    {
                        // normal string & char constants can have escapes
                        var pos = _lexer.TextWindow.Position;
                        char c2;
                        ch = _lexer.ScanEscapeSequence(out c2);
                        if ((ch == '{' || ch == '}') && error == null)
                        {
                            error = _lexer.MakeError(pos, 1, ErrorCode.ERR_EscapedCurly, ch);
                        }
                    }
                    else if (ch == '"')
                    {
                        if (_isVerbatim && _lexer.TextWindow.PeekChar(1) == '"')
                        {
                            _lexer.TextWindow.AdvanceChar();
                            _lexer.TextWindow.AdvanceChar();
                        }
                        else
                        {
                            return; // premature end of string! let caller complain about unclosed interpolation
                        }
                    }
                    else if (ch == '{')
                    {
                        var pos = _lexer.TextWindow.Position;
                        _lexer.TextWindow.AdvanceChar();
                        // ensure any { characters are doubled up
                        if (_lexer.TextWindow.PeekChar() == '{')
                        {
                            _lexer.TextWindow.AdvanceChar(); // {
                        }
                        else if (error == null)
                        {
                            error = _lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "{");
                        }
                    }
                    else if (ch == '}')
                    {
                        if (_lexer.TextWindow.PeekChar(1) == '}')
                        {
                            _lexer.TextWindow.AdvanceChar();
                            _lexer.TextWindow.AdvanceChar();
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
                        _lexer.TextWindow.AdvanceChar();
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

                    char ch = _lexer.TextWindow.PeekChar();
                    switch (ch)
                    {
                        case '#':
                            // preprocessor directives not allowed.
                            if (error == null)
                            {
                                error = _lexer.MakeError(_lexer.TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString());
                            }

                            _lexer.TextWindow.AdvanceChar();
                            continue;
                        case '$':
                            if (_lexer.TextWindow.PeekChar(1) == '"' || _lexer.TextWindow.PeekChar(1) == '@' && _lexer.TextWindow.PeekChar(2) == '"')
                            {
                                bool isVerbatimSubstring = _lexer.TextWindow.PeekChar(1) == '@';
                                var interpolations = (ArrayBuilder<Interpolation>)null;
                                var info = default(TokenInfo);
                                bool wasVerbatim = this._isVerbatim;
                                bool wasAllowNewlines = this._allowNewlines;
                                try
                                {
                                    this._isVerbatim = isVerbatimSubstring;
                                    this._allowNewlines &= _isVerbatim;
                                    bool closeQuoteMissing;
                                    ScanInterpolatedStringLiteralTop(interpolations, ref info, out closeQuoteMissing);
                                }
                                finally
                                {
                                    this._isVerbatim = wasVerbatim;
                                    this._allowNewlines = wasAllowNewlines;
                                }
                                continue;
                            }

                            goto default;
                        case ':':
                            // the first colon not nested within matching delimiters is the start of the format string
                            if (isHole)
                            {
                                Debug.Assert(colonPosition == 0);
                                colonPosition = _lexer.TextWindow.Position;
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
                                error = _lexer.MakeError(_lexer.TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString());
                            }

                            goto default;
                        case '"' when RecoveringFromRunawayLexing():
                            // When recovering from mismatched delimiters, we consume the next
                            // quote character as the close quote for the interpolated string. In
                            // practice this gets us out of trouble in scenarios we've encountered.
                            // See, for example, https://github.com/dotnet/roslyn/issues/44789
                            return;
                        case '"':
                        case '\'':
                            // handle string or character literal inside an expression hole.
                            ScanInterpolatedStringLiteralNestedString();
                            continue;
                        case '@':
                            if (_lexer.TextWindow.PeekChar(1) == '"' && !RecoveringFromRunawayLexing())
                            {
                                // check for verbatim string inside an expression hole.
                                ScanInterpolatedStringLiteralNestedVerbatimString();
                                continue;
                            }
                            else if (_lexer.TextWindow.PeekChar(1) == '$' && _lexer.TextWindow.PeekChar(2) == '"')
                            {
                                _lexer.CheckFeatureAvailability(MessageID.IDS_FeatureAltInterpolatedVerbatimStrings);
                                var interpolations = (ArrayBuilder<Interpolation>)null;
                                var info = default(TokenInfo);
                                bool wasVerbatim = this._isVerbatim;
                                bool wasAllowNewlines = this._allowNewlines;
                                try
                                {
                                    this._isVerbatim = true;
                                    this._allowNewlines = true;
                                    bool closeQuoteMissing;
                                    ScanInterpolatedStringLiteralTop(interpolations, ref info, out closeQuoteMissing);
                                }
                                finally
                                {
                                    this._isVerbatim = wasVerbatim;
                                    this._allowNewlines = wasAllowNewlines;
                                }
                                continue;
                            }

                            goto default;
                        case '/':
                            switch (_lexer.TextWindow.PeekChar(1))
                            {
                                case '/':
                                    if (_isVerbatim && _allowNewlines)
                                    {
                                        _lexer.TextWindow.AdvanceChar(); // skip /
                                        _lexer.TextWindow.AdvanceChar(); // skip /
                                        while (!IsAtEnd(false))
                                        {
                                            _lexer.TextWindow.AdvanceChar(); // skip // comment character
                                        }
                                    }
                                    else
                                    {
                                        // error: single-line comment not allowed in an interpolated string
                                        if (error == null)
                                        {
                                            error = _lexer.MakeError(_lexer.TextWindow.Position, 2, ErrorCode.ERR_SingleLineCommentInExpressionHole);
                                        }

                                        _lexer.TextWindow.AdvanceChar();
                                        _lexer.TextWindow.AdvanceChar();
                                    }
                                    continue;
                                case '*':
                                    // check for and scan /* comment */
                                    ScanInterpolatedStringLiteralNestedComment();
                                    continue;
                                default:
                                    _lexer.TextWindow.AdvanceChar();
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
                            _lexer.TextWindow.AdvanceChar();
                            continue;
                    }
                }
            }

            /// <summary>
            /// The lexer can run away consuming the rest of the input when delimiters are mismatched.
            /// This is a test for when we are attempting to recover from that situation.
            /// </summary>
            private bool RecoveringFromRunawayLexing() => this.error != null;

            private void ScanInterpolatedStringLiteralNestedComment()
            {
                Debug.Assert(_lexer.TextWindow.PeekChar() == '/');
                _lexer.TextWindow.AdvanceChar();
                Debug.Assert(_lexer.TextWindow.PeekChar() == '*');
                _lexer.TextWindow.AdvanceChar();
                while (true)
                {
                    if (IsAtEnd())
                    {
                        return; // let the caller complain about the unterminated quote
                    }

                    var ch = _lexer.TextWindow.PeekChar();
                    _lexer.TextWindow.AdvanceChar();
                    if (ch == '*' && _lexer.TextWindow.PeekChar() == '/')
                    {
                        _lexer.TextWindow.AdvanceChar(); // skip */
                        return;
                    }
                }
            }

            private void ScanInterpolatedStringLiteralNestedString()
            {
                var discarded = default(TokenInfo);
                _lexer.ScanStringLiteral(ref discarded, inDirective: false);
            }

            private void ScanInterpolatedStringLiteralNestedVerbatimString()
            {
                var discarded = default(TokenInfo);
                _lexer.ScanVerbatimStringLiteral(ref discarded, _allowNewlines);
            }

            private void ScanInterpolatedStringLiteralHoleBracketed(char start, char end)
            {
                Debug.Assert(start == _lexer.TextWindow.PeekChar());
                _lexer.TextWindow.AdvanceChar();
                int colon = 0;
                ScanInterpolatedStringLiteralHoleBalancedText(end, false, ref colon);
                if (_lexer.TextWindow.PeekChar() == end)
                {
                    _lexer.TextWindow.AdvanceChar();
                }
                else
                {
                    // an error was given by the caller
                }
            }
        }
    }
}
