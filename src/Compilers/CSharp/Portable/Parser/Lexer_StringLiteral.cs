// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
                    ch = this.ScanEscapeSequence(out var c2);
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

            info.Text = TextWindow.GetText(intern: true);
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

        /// <summary>
        /// Returns an appropriate error code if scanning this verbatim literal ran into an error.
        /// </summary>
        private ErrorCode? ScanVerbatimStringLiteral(ref TokenInfo info)
        {
            _builder.Length = 0;

            Debug.Assert(TextWindow.PeekChar() == '@' && TextWindow.PeekChar(1) == '"');
            TextWindow.AdvanceChar(2);

            ErrorCode? error = null;
            while (true)
            {
                var ch = TextWindow.PeekChar();
                if (ch == '"')
                {
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '"')
                    {
                        // Doubled quote -- skip & put the single quote in the string and keep going.
                        TextWindow.AdvanceChar();
                        _builder.Append(ch);
                        continue;
                    }

                    // otherwise, the string is finished.
                    break;
                }

                if (ch == SlidingTextWindow.InvalidCharacter && TextWindow.IsReallyAtEnd())
                {
                    // Reached the end of the source without finding the end-quote.  Give an error back at the
                    // starting point. And finish lexing this string.
                    error ??= ErrorCode.ERR_UnterminatedStringLit;
                    break;
                }

                TextWindow.AdvanceChar();
                _builder.Append(ch);
            }

            info.Kind = SyntaxKind.StringLiteralToken;
            info.Text = TextWindow.GetText(intern: false);
            info.StringValue = _builder.ToString();

            return error;
        }

        private void ScanInterpolatedStringLiteral(ref TokenInfo info)
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

            ScanInterpolatedStringLiteralTop(ref info, out var error, openQuoteRange: out _, interpolations: null, closeQuoteRange: out _);
            this.AddError(error);
        }

        internal void ScanInterpolatedStringLiteralTop(
            ref TokenInfo info,
            out SyntaxDiagnosticInfo? error,
            out Range openQuoteRange,
            ArrayBuilder<Interpolation>? interpolations,
            out Range closeQuoteRange)
        {
            var subScanner = new InterpolatedStringScanner(this);
            subScanner.ScanInterpolatedStringLiteralTop(out openQuoteRange, interpolations, out closeQuoteRange);
            error = subScanner.Error;
            info.Kind = SyntaxKind.InterpolatedStringToken;
            info.Text = TextWindow.GetText(intern: false);
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

        [NonCopyable]
        private struct InterpolatedStringScanner
        {
            private readonly Lexer _lexer;
            private readonly bool _isVerbatim;

            /// <summary>
            /// There are two types of errors we can encounter when trying to scan out an interpolated string (and its
            /// interpolations).  The first are true syntax errors where we do not know what it is going on and have no
            /// good strategy to get back on track.  This happens when we see things in the interpolation we truly do
            /// not know what to do with, or when we find we've gotten into an unbalanced state with the bracket pairs
            /// we're consuming.  In this case, we will often choose to bail out rather than go on and potentially make
            /// things worse.
            /// </summary>
            public SyntaxDiagnosticInfo? Error = null;
            private bool EncounteredUnrecoverableError = false;

            public InterpolatedStringScanner(Lexer lexer)
            {
                _lexer = lexer;

                _isVerbatim = (lexer.TextWindow.PeekChar(0) == '$' && lexer.TextWindow.PeekChar(1) == '@') ||
                              (lexer.TextWindow.PeekChar(0) == '@' && lexer.TextWindow.PeekChar(1) == '$');
            }

            private bool IsAtEnd()
            {
                return IsAtEnd(_isVerbatim);
            }

            private bool IsAtEnd(bool allowNewline)
            {
                char ch = PeekChar();
                return
                    (!allowNewline && SyntaxFacts.IsNewLine(ch)) ||
                    (ch == SlidingTextWindow.InvalidCharacter && _lexer.TextWindow.IsReallyAtEnd());
            }

            private char PeekChar()
                => _lexer.TextWindow.PeekChar();

            private char PeekChar(int n)
                => _lexer.TextWindow.PeekChar(n);

            private void AdvanceChar()
                => _lexer.TextWindow.AdvanceChar();

            private void AdvanceChar(int n)
                => _lexer.TextWindow.AdvanceChar(n);

            private void TrySetUnrecoverableError(SyntaxDiagnosticInfo error)
            {
                // only need to record the first error we hit
                Error ??= error;

                // No matter what, ensure that we know we hit an error we can't recover from.
                EncounteredUnrecoverableError = true;
            }

            private void TrySetRecoverableError(SyntaxDiagnosticInfo error)
            {
                // only need to record the first error we hit
                Error ??= error;

                // Do not touch 'EncounteredUnrecoverableError'.  If we already encountered something unrecoverable,
                // that doesn't change.  And if we haven't hit something unrecoverable then we stay in that mode as this
                // is a recoverable error.
            }

            internal void ScanInterpolatedStringLiteralTop(
                out Range openQuoteRange,
                ArrayBuilder<Interpolation>? interpolations,
                out Range closeQuoteRange)
            {
                ScanInterpolatedStringLiteralStart(out openQuoteRange);
                ScanInterpolatedStringLiteralContents(interpolations);
                ScanInterpolatedStringLiteralEnd(out closeQuoteRange);
            }

            private void ScanInterpolatedStringLiteralStart(out Range openQuoteRange)
            {
                // Handles reading the start of the interpolated string literal (up to where the content begins)
                var start = _lexer.TextWindow.Position;
                if (_isVerbatim)
                {
                    // skip past @$ or $!
                    AdvanceChar(2);
                }
                else
                {
                    Debug.Assert(PeekChar() == '$');
                    AdvanceChar();
                }

                Debug.Assert(PeekChar() == '"');
                AdvanceChar();

                openQuoteRange = new Range(start, _lexer.TextWindow.Position);
            }

            private void ScanInterpolatedStringLiteralEnd(out Range closeQuoteRange)
            {
                // Handles reading the end of the interpolated string literal (after where the content ends)

                var closeQuotePosition = _lexer.TextWindow.Position;
                if (PeekChar() != '"')
                {
                    Debug.Assert(IsAtEnd());
                    int position = IsAtEnd(allowNewline: true) ? _lexer.TextWindow.Position - 1 : _lexer.TextWindow.Position;
                    TrySetUnrecoverableError(_lexer.MakeError(position, 1, _isVerbatim ? ErrorCode.ERR_UnterminatedStringLit : ErrorCode.ERR_NewlineInConst));
                }
                else
                {
                    // found the closing quote
                    AdvanceChar(); // "
                }

                closeQuoteRange = new Range(closeQuotePosition, _lexer.TextWindow.Position);
            }

            private void ScanInterpolatedStringLiteralContents(ArrayBuilder<Interpolation>? interpolations)
            {
                while (true)
                {
                    if (IsAtEnd())
                    {
                        // error: end of line before end of string
                        return;
                    }

                    switch (PeekChar())
                    {
                        case '"' when RecoveringFromRunawayLexing():
                            // When recovering from mismatched delimiters, we consume the next
                            // quote character as the close quote for the interpolated string. In
                            // practice this gets us out of trouble in scenarios we've encountered.
                            // See, for example, https://github.com/dotnet/roslyn/issues/44789
                            return;
                        case '"':
                            if (_isVerbatim && PeekChar(1) == '"')
                            {
                                AdvanceChar(2); // ""
                                continue;
                            }
                            // found the end of the string
                            return;
                        case '}':
                            var pos = _lexer.TextWindow.Position;
                            AdvanceChar(); // }
                            // ensure any } characters are doubled up
                            if (PeekChar() == '}')
                            {
                                AdvanceChar(); // }
                            }
                            else
                            {
                                TrySetUnrecoverableError(_lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "}"));
                            }
                            continue;
                        case '{':
                            if (PeekChar(1) == '{')
                            {
                                AdvanceChar(2); // {{
                            }
                            else
                            {
                                int openBracePosition = _lexer.TextWindow.Position;
                                AdvanceChar();
                                ScanInterpolatedStringLiteralHoleBalancedText('}', isHole: true, out var colonRange);
                                int closeBracePosition = _lexer.TextWindow.Position;
                                if (PeekChar() == '}')
                                {
                                    AdvanceChar();
                                }
                                else
                                {
                                    TrySetUnrecoverableError(_lexer.MakeError(openBracePosition - 1, 2, ErrorCode.ERR_UnclosedExpressionHole));
                                }

                                interpolations?.Add(new Interpolation(
                                    new Range(openBracePosition, openBracePosition + 1),
                                    colonRange,
                                    new Range(closeBracePosition, _lexer.TextWindow.Position)));
                            }
                            continue;
                        case '\\':
                            if (_isVerbatim)
                            {
                                goto default;
                            }

                            var escapeStart = _lexer.TextWindow.Position;
                            char ch = _lexer.ScanEscapeSequence(surrogateCharacter: out _);
                            if (ch == '{' || ch == '}')
                            {
                                TrySetUnrecoverableError(_lexer.MakeError(escapeStart, _lexer.TextWindow.Position - escapeStart, ErrorCode.ERR_EscapedCurly, ch));
                            }

                            continue;
                        default:
                            // found some other character in the string portion
                            AdvanceChar();
                            continue;
                    }
                }
            }

            private void ScanFormatSpecifier()
            {
                Debug.Assert(PeekChar() == ':');
                AdvanceChar();
                while (true)
                {
                    char ch = PeekChar();
                    if (ch == '\\' && !_isVerbatim)
                    {
                        // normal string & char constants can have escapes
                        var pos = _lexer.TextWindow.Position;
                        ch = _lexer.ScanEscapeSequence(surrogateCharacter: out _);
                        if (ch == '{' || ch == '}')
                        {
                            TrySetUnrecoverableError(_lexer.MakeError(pos, 1, ErrorCode.ERR_EscapedCurly, ch));
                        }
                    }
                    else if (ch == '"')
                    {
                        if (_isVerbatim && PeekChar(1) == '"')
                        {
                            AdvanceChar(2); // ""
                        }
                        else
                        {
                            return; // premature end of string! let caller complain about unclosed interpolation
                        }
                    }
                    else if (ch == '{')
                    {
                        var pos = _lexer.TextWindow.Position;
                        AdvanceChar();
                        // ensure any { characters are doubled up
                        if (PeekChar() == '{')
                        {
                            AdvanceChar(); // {
                        }
                        else
                        {
                            TrySetUnrecoverableError(_lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "{"));
                        }
                    }
                    else if (ch == '}')
                    {
                        if (PeekChar(1) == '}')
                        {
                            AdvanceChar(2); // }}
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
                        AdvanceChar();
                    }
                }
            }

            /// <summary>
            /// Scan past the hole inside an interpolated string literal, leaving the current character on the '}' (if any)
            /// </summary>
            private void ScanInterpolatedStringLiteralHoleBalancedText(char endingChar, bool isHole, out Range colonRange)
            {
                colonRange = default;
                while (true)
                {
                    char ch = PeekChar();

                    // Note: within a hole newlines are always allowed.  The restriction on if newlines are allowed or not
                    // is only within a text-portion of the interpolated string.
                    if (IsAtEnd(allowNewline: true))
                    {
                        // the caller will complain
                        return;
                    }

                    switch (ch)
                    {
                        case '#':
                            // preprocessor directives not allowed.
                            TrySetUnrecoverableError(_lexer.MakeError(_lexer.TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString()));
                            AdvanceChar();
                            continue;
                        case '$':
                            if (PeekChar(1) == '"' || (PeekChar(1) == '@' && PeekChar(2) == '"'))
                            {
                                var discarded = default(TokenInfo);
                                _lexer.ScanInterpolatedStringLiteral(ref discarded);
                                continue;
                            }

                            goto default;
                        case ':':
                            // the first colon not nested within matching delimiters is the start of the format string
                            if (isHole)
                            {
                                Debug.Assert(colonRange.Equals(default(Range)));
                                colonRange = new Range(_lexer.TextWindow.Position, _lexer.TextWindow.Position + 1);
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

                            TrySetUnrecoverableError(_lexer.MakeError(_lexer.TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString()));
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
                            if (PeekChar(1) == '"' && !RecoveringFromRunawayLexing())
                            {
                                // check for verbatim string inside an expression hole.
                                var nestedStringPosition = _lexer.TextWindow.Position;

                                // Note that this verbatim string may encounter an error (for example if it contains a
                                // new line and we don't allow that).  This should be reported to the user, but should
                                // not put us into an unrecoverable position.  We can clearly see that this was intended
                                // to be a normal verbatim string, so we can continue on an attempt to understand the
                                // outer interpolated string properly.
                                var discarded = default(TokenInfo);
                                var errorCode = _lexer.ScanVerbatimStringLiteral(ref discarded);
                                if (errorCode is ErrorCode code)
                                {
                                    TrySetRecoverableError(_lexer.MakeError(nestedStringPosition, width: 2, code));
                                }

                                continue;
                            }
                            else if (PeekChar(1) == '$' && PeekChar(2) == '"')
                            {
                                var discarded = default(TokenInfo);
                                _lexer.ScanInterpolatedStringLiteral(ref discarded);
                                continue;
                            }

                            goto default;
                        case '/':
                            switch (PeekChar(1))
                            {
                                case '/':
                                    _lexer.ScanToEndOfLine();
                                    continue;
                                case '*':
                                    _lexer.ScanMultiLineComment(out _);
                                    continue;
                                default:
                                    AdvanceChar();
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
                            AdvanceChar();
                            continue;
                    }
                }
            }

            /// <summary>
            /// The lexer can run away consuming the rest of the input when delimiters are mismatched. This is a test
            /// for when we are attempting to recover from that situation.  Note that just running into new lines will
            /// not make us think we're in runaway lexing.
            /// </summary>
            private bool RecoveringFromRunawayLexing() => this.EncounteredUnrecoverableError;

            private void ScanInterpolatedStringLiteralNestedString()
            {
                var discarded = default(TokenInfo);
                _lexer.ScanStringLiteral(ref discarded, inDirective: false);
            }

            private void ScanInterpolatedStringLiteralHoleBracketed(char start, char end)
            {
                Debug.Assert(start == PeekChar());
                AdvanceChar();
                ScanInterpolatedStringLiteralHoleBalancedText(end, isHole: false, out _);
                if (PeekChar() == end)
                {
                    AdvanceChar();
                }
                else
                {
                    // an error was given by the caller
                }
            }
        }
    }
}
