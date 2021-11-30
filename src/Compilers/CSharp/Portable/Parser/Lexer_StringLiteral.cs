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

        private void ScanVerbatimStringLiteral(ref TokenInfo info)
        {
            _builder.Length = 0;

            Debug.Assert(TextWindow.PeekChar() == '@' && TextWindow.PeekChar(1) == '"');
            TextWindow.AdvanceChar(2);

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
                    this.AddError(ErrorCode.ERR_UnterminatedStringLit);
                    break;
                }

                TextWindow.AdvanceChar();
                _builder.Append(ch);
            }

            info.Kind = SyntaxKind.StringLiteralToken;
            info.Text = TextWindow.GetText(intern: false);
            info.StringValue = _builder.ToString();
        }

        private void ScanInterpolatedStringLiteral(ref TokenInfo info)
        {
            // We have a string of one of the forms
            //                $" ... "
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

            ScanInterpolatedStringLiteralTop(
                ref info,
                out var error,
                kind: out _,
                openQuoteRange: out _,
                interpolations: null,
                closeQuoteRange: out _);

            this.AddError(error);
        }

        internal void ScanInterpolatedStringLiteralTop(
            ref TokenInfo info,
            out SyntaxDiagnosticInfo? error,
            out InterpolatedStringKind kind,
            out Range openQuoteRange,
            ArrayBuilder<Interpolation>? interpolations,
            out Range closeQuoteRange)
        {
            var subScanner = new InterpolatedStringScanner(this);
            subScanner.ScanInterpolatedStringLiteralTop(out kind, out openQuoteRange, interpolations, out closeQuoteRange);
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

        internal enum InterpolatedStringKind
        {
            /// <summary>
            /// Normal interpolated string that just starts with $"
            /// </summary>
            Normal,
            /// <summary>
            /// Verbatim interpolated string that starts with $@" or @$"
            /// </summary>
            Verbatim,
        }

        [NonCopyable]
        private ref struct InterpolatedStringScanner
        {
            private readonly Lexer _lexer;
            private readonly InterpolatedStringKind _kind;

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

                _kind =
                    (lexer.TextWindow.PeekChar(0) == '$' && lexer.TextWindow.PeekChar(1) == '@') ||
                    (lexer.TextWindow.PeekChar(0) == '@' && lexer.TextWindow.PeekChar(1) == '$')
                        ? InterpolatedStringKind.Verbatim
                        : InterpolatedStringKind.Normal;
            }

            private bool IsAtEnd()
            {
                return IsAtEnd(_kind is InterpolatedStringKind.Verbatim);
            }

            private bool IsAtEnd(bool allowNewline)
            {
                char ch = _lexer.TextWindow.PeekChar();
                return
                    (!allowNewline && SyntaxFacts.IsNewLine(ch)) ||
                    (ch == SlidingTextWindow.InvalidCharacter && _lexer.TextWindow.IsReallyAtEnd());
            }

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
                out InterpolatedStringKind kind,
                out Range openQuoteRange,
                ArrayBuilder<Interpolation>? interpolations,
                out Range closeQuoteRange)
            {
                kind = _kind;
                ScanInterpolatedStringLiteralStart(out openQuoteRange);
                ScanInterpolatedStringLiteralContents(interpolations);
                ScanInterpolatedStringLiteralEnd(out closeQuoteRange);
            }

            private void ScanInterpolatedStringLiteralStart(out Range openQuoteRange)
            {
                // Handles reading the start of the interpolated string literal (up to where the content begins)
                var start = _lexer.TextWindow.Position;
                if (_kind is InterpolatedStringKind.Verbatim)
                {
                    // skip past @$ or $@
                    _lexer.TextWindow.AdvanceChar(2);
                }
                else
                {
                    Debug.Assert(_lexer.TextWindow.PeekChar() == '$');
                    _lexer.TextWindow.AdvanceChar();
                }

                Debug.Assert(_lexer.TextWindow.PeekChar() == '"');
                _lexer.TextWindow.AdvanceChar();

                openQuoteRange = new Range(start, _lexer.TextWindow.Position);
            }

            private void ScanInterpolatedStringLiteralEnd(out Range closeQuoteRange)
            {
                // Handles reading the end of the interpolated string literal (after where the content ends)

                var closeQuotePosition = _lexer.TextWindow.Position;
                if (_lexer.TextWindow.PeekChar() != '"')
                {
                    Debug.Assert(IsAtEnd());
                    int position = IsAtEnd(allowNewline: true) ? _lexer.TextWindow.Position - 1 : _lexer.TextWindow.Position;
                    TrySetUnrecoverableError(_lexer.MakeError(position, 1, _kind is InterpolatedStringKind.Verbatim ? ErrorCode.ERR_UnterminatedStringLit : ErrorCode.ERR_NewlineInConst));
                }
                else
                {
                    // found the closing quote
                    _lexer.TextWindow.AdvanceChar(); // "
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

                    switch (_lexer.TextWindow.PeekChar())
                    {
                        case '"' when RecoveringFromRunawayLexing():
                            // When recovering from mismatched delimiters, we consume the next
                            // quote character as the close quote for the interpolated string. In
                            // practice this gets us out of trouble in scenarios we've encountered.
                            // See, for example, https://github.com/dotnet/roslyn/issues/44789
                            return;
                        case '"':
                            if (_kind is InterpolatedStringKind.Verbatim && _lexer.TextWindow.PeekChar(1) == '"')
                            {
                                _lexer.TextWindow.AdvanceChar(2); // ""
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
                            else
                            {
                                TrySetUnrecoverableError(_lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "}"));
                            }
                            continue;
                        case '{':
                            if (_lexer.TextWindow.PeekChar(1) == '{')
                            {
                                _lexer.TextWindow.AdvanceChar(2); // {{
                            }
                            else
                            {
                                int openBracePosition = _lexer.TextWindow.Position;
                                _lexer.TextWindow.AdvanceChar();
                                ScanInterpolatedStringLiteralHoleBalancedText('}', isHole: true, out var colonRange);
                                int closeBracePosition = _lexer.TextWindow.Position;
                                if (_lexer.TextWindow.PeekChar() == '}')
                                {
                                    _lexer.TextWindow.AdvanceChar();
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
                            if (_kind is InterpolatedStringKind.Verbatim)
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
                    if (ch == '\\' && _kind is InterpolatedStringKind.Normal)
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
                        if (_kind is InterpolatedStringKind.Verbatim && _lexer.TextWindow.PeekChar(1) == '"')
                        {
                            _lexer.TextWindow.AdvanceChar(2); // ""
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
                        else
                        {
                            TrySetUnrecoverableError(_lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "{"));
                        }
                    }
                    else if (ch == '}')
                    {
                        if (_lexer.TextWindow.PeekChar(1) == '}')
                        {
                            _lexer.TextWindow.AdvanceChar(2); // }}
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
            private void ScanInterpolatedStringLiteralHoleBalancedText(char endingChar, bool isHole, out Range colonRange)
            {
                colonRange = default;
                while (true)
                {
                    char ch = _lexer.TextWindow.PeekChar();

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
                            _lexer.TextWindow.AdvanceChar();
                            continue;
                        case '$':
                            {
                                var discarded = default(TokenInfo);
                                if (_lexer.TryScanInterpolatedString(ref discarded))
                                {
                                    continue;
                                }

                                goto default;
                            }
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
                            {
                                var discarded = default(TokenInfo);
                                if (_lexer.TryScanAtStringToken(ref discarded))
                                    continue;

                                // Wasn't an @"" or @$"" string.  Just consume this as normal code.
                                goto default;
                            }
                        case '/':
                            switch (_lexer.TextWindow.PeekChar(1))
                            {
                                case '/':
                                    _lexer.ScanToEndOfLine();
                                    continue;
                                case '*':
                                    _lexer.ScanMultiLineComment(out _);
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
                Debug.Assert(start == _lexer.TextWindow.PeekChar());
                _lexer.TextWindow.AdvanceChar();
                ScanInterpolatedStringLiteralHoleBalancedText(end, isHole: false, colonRange: out _);
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
