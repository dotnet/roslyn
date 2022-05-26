// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        private void ScanStringLiteral(ref TokenInfo info, bool inDirective)
        {
            var quoteCharacter = TextWindow.PeekChar();
            Debug.Assert(quoteCharacter is '\'' or '"');

            if (TextWindow.PeekChar() == '"' &&
                TextWindow.PeekChar(1) == '"' &&
                TextWindow.PeekChar(2) == '"')
            {
                ScanRawStringLiteral(ref info, inDirective);
                if (inDirective)
                {
                    // Reinterpret this as just a string literal so that the directive parser can consume this.  
                    // But report this is illegal so that the user knows to fix this up to be a normal string.
                    info.Kind = SyntaxKind.StringLiteralToken;
                    info.StringValue = "";
                    this.AddError(ErrorCode.ERR_RawStringNotInDirectives);
                }
                return;
            }

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

            if (quoteCharacter == '\'')
            {
                info.Text = TextWindow.GetText(intern: true);
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
                if (!inDirective && ScanUTF8Suffix())
                {
                    info.Kind = SyntaxKind.UTF8StringLiteralToken;
                }
                else
                {
                    info.Kind = SyntaxKind.StringLiteralToken;
                }

                info.Text = TextWindow.GetText(intern: true);

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

        private bool ScanUTF8Suffix()
        {
            if (TextWindow.PeekChar() is ('u' or 'U') && TextWindow.PeekChar(1) == '8')
            {
                TextWindow.AdvanceChar(2);
                return true;
            }

            return false;
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
            Debug.Assert(TextWindow.PeekChar() == '@');
            _builder.Length = 0;

            var start = TextWindow.Position;
            while (TextWindow.PeekChar() == '@')
            {
                TextWindow.AdvanceChar();
            }

            if (TextWindow.Position - start >= 2)
            {
                this.AddError(start, width: TextWindow.Position - start, ErrorCode.ERR_IllegalAtSequence);
            }

            Debug.Assert(TextWindow.PeekChar() == '"');
            TextWindow.AdvanceChar();

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

            if (ScanUTF8Suffix())
            {
                info.Kind = SyntaxKind.UTF8StringLiteralToken;
            }
            else
            {
                info.Kind = SyntaxKind.StringLiteralToken;
            }

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
            /// Normal interpolated string that just starts with <c>$"</c>
            /// </summary>
            Normal,
            /// <summary>
            /// Verbatim interpolated string that starts with <c>$@"</c> or <c>@$"</c>
            /// </summary>
            Verbatim,
            /// <summary>
            /// Single-line raw interpolated string that starts with at least one <c>$</c>, and at least three <c>"</c>s.
            /// </summary>
            SingleLineRaw,
            /// <summary>
            /// Multi-line raw interpolated string that starts with at least one <c>$</c>, and at least three <c>"</c>s.
            /// </summary>
            MultiLineRaw,
        }

        /// <summary>
        /// Non-copyable ref-struct so that this will only live on the stack for the lifetime of the lexer/parser
        /// recursing to process interpolated strings.
        /// </summary>
        [NonCopyable]
        private ref struct InterpolatedStringScanner
        {
            private readonly Lexer _lexer;

            /// <summary>
            /// Error encountered while scanning.  If we run into an error, then we'll attempt to stop parsing at the
            /// next potential ending location to prevent compounding the issue.
            /// </summary>
            public SyntaxDiagnosticInfo? Error = null;

            public InterpolatedStringScanner(Lexer lexer)
            {
                _lexer = lexer;
            }

            private bool IsAtEnd(InterpolatedStringKind kind)
            {
                return IsAtEnd(allowNewline: kind is InterpolatedStringKind.Verbatim or InterpolatedStringKind.MultiLineRaw);
            }

            private bool IsAtEnd(bool allowNewline)
            {
                char ch = _lexer.TextWindow.PeekChar();
                return
                    (!allowNewline && SyntaxFacts.IsNewLine(ch)) ||
                    (ch == SlidingTextWindow.InvalidCharacter && _lexer.TextWindow.IsReallyAtEnd());
            }

            private void TrySetError(SyntaxDiagnosticInfo error)
            {
                // only need to record the first error we hit
                Error ??= error;
            }

            internal void ScanInterpolatedStringLiteralTop(
                out InterpolatedStringKind kind,
                out Range openQuoteRange,
                ArrayBuilder<Interpolation>? interpolations,
                out Range closeQuoteRange)
            {
                // Scan through the open-quote portion of this literal, determining important information the rest of
                // the scanning needs.
                var start = _lexer.TextWindow.Position;
                var succeeded = ScanOpenQuote(out kind, out var startingDollarSignCount, out var startingQuoteCount);
                Debug.Assert(_lexer.TextWindow.Position != start);

                openQuoteRange = start.._lexer.TextWindow.Position;

                if (!succeeded)
                {
                    // Processing the start of this literal didn't give us enough information to proceed.  Stop now,
                    // terminating the string to the furthest point we reached.
                    closeQuoteRange = _lexer.TextWindow.Position.._lexer.TextWindow.Position;
                    return;
                }

                ScanInterpolatedStringLiteralContents(kind, startingDollarSignCount, startingQuoteCount, interpolations);
                ScanInterpolatedStringLiteralEnd(kind, startingQuoteCount, out closeQuoteRange);
            }

            /// <param name="startingDollarSignCount">
            /// Number of '$' characters this interpolated string started with.  We'll need to see that many '{' in a
            /// row to start an interpolation.  Any less and we'll treat that as just text.  Note if this count is '1'
            /// then this is a normal (non-raw) interpolation and `{{` is treated as an escape.
            /// </param>
            /// <param name="startingQuoteCount">Number of '"' characters this interpolated string started with.</param>
            /// <returns><see langword="true"/> if we successfully processed the open quote range and can proceed to the
            /// rest of the literal. <see langword="false"/> if we were not successful and should stop
            /// processing.</returns>
            private bool ScanOpenQuote(
                out InterpolatedStringKind kind,
                out int startingDollarSignCount,
                out int startingQuoteCount)
            {
                // Handles reading the start of the interpolated string literal (up to where the content begins)
                var window = _lexer.TextWindow;
                var start = window.Position;

                if ((window.PeekChar(0), window.PeekChar(1), window.PeekChar(2)) is ('$', '@', '"') or ('@', '$', '"'))
                {
                    // $@" or @$"
                    //
                    // Note: we do not consider $@""" as the start of raw-string (in error conditions) as that's a legal
                    // verbatim string beginning already.

                    kind = InterpolatedStringKind.Verbatim;
                    startingDollarSignCount = 1;
                    startingQuoteCount = 1;
                    window.AdvanceChar(3);
                    return true;
                }

                if ((window.PeekChar(0), window.PeekChar(1), window.PeekChar(2), window.PeekChar(3)) is
                        ('$', '"', not '"', _) or ('$', '"', '"', not '"'))
                {
                    // $"...
                    // $""
                    // not $"""
                    kind = InterpolatedStringKind.Normal;
                    startingDollarSignCount = 1;
                    startingQuoteCount = 1;
                    window.AdvanceChar(2);
                    return true;
                }

                // From this point we have either a complete error case that we cannot process further, or a raw literal
                // of some sort.
                var prefixAtCount = _lexer.ConsumeAtSignSequence();
                startingDollarSignCount = _lexer.ConsumeDollarSignSequence();
                Debug.Assert(startingDollarSignCount > 0);

                var suffixAtCount = _lexer.ConsumeAtSignSequence();
                startingQuoteCount = _lexer.ConsumeQuoteSequence();

                var totalAtCount = prefixAtCount + suffixAtCount;

                // We should only have gotten here if we had at least two characters that made us think we had an
                // interpolated string. Note that we may enter here on just `@@` or `$$` (without seeing anything else),
                // so we can't put a stricter bound on this here.
                Debug.Assert(totalAtCount + startingDollarSignCount + startingQuoteCount >= 2);

                if (startingQuoteCount == 0)
                {
                    // We have no quotes at all.  We cannot continue on as we have no quotes, and thus can't even find
                    // where the string starts or ends.
                    TrySetError(_lexer.MakeError(start, window.Position - start, ErrorCode.ERR_StringMustStartWithQuoteCharacter));
                    kind = totalAtCount == 1 && startingDollarSignCount == 1
                        ? InterpolatedStringKind.Verbatim
                        : InterpolatedStringKind.SingleLineRaw;
                    return false;
                }

                // @-signs with interpolations are always illegal.  Detect these and give a reasonable error message.
                // Continue on if we can.
                if (totalAtCount > 0)
                {
                    TrySetError(_lexer.MakeError(start, window.Position - start, ErrorCode.ERR_IllegalAtSequence));
                }

                if (startingQuoteCount < 3)
                {
                    // 1-2 quotes present.  Not legal.  But we can give a good error message and still proceed.
                    TrySetError(_lexer.MakeError(window.Position - startingQuoteCount, startingQuoteCount, ErrorCode.ERR_NotEnoughQuotesForRawString));
                }

                // Now see if this was a single-line or multi-line raw literal.

                var afterQuotePosition = window.Position;
                _lexer.ConsumeWhitespace(builder: null);
                if (SyntaxFacts.IsNewLine(window.PeekChar()))
                {
                    // We had whitespace followed by a newline.  That section is considered the open-quote section of
                    // the literal.
                    window.AdvancePastNewLine();
                    kind = InterpolatedStringKind.MultiLineRaw;
                }
                else
                {
                    // wasn't multi-line, jump back to right after the quotes as what follows is content and not
                    // considered part of the open quote.
                    window.Reset(afterQuotePosition);
                    kind = InterpolatedStringKind.SingleLineRaw;
                }

                return true;
            }

            private void ScanInterpolatedStringLiteralEnd(InterpolatedStringKind kind, int startingQuoteCount, out Range closeQuoteRange)
            {
                // Handles reading the end of the interpolated string literal (after where the content ends)

                var closeQuotePosition = _lexer.TextWindow.Position;

                if (kind is InterpolatedStringKind.Normal or InterpolatedStringKind.Verbatim)
                {
                    ScanNormalOrVerbatimInterpolatedStringLiteralEnd(kind);
                }
                else
                {
                    Debug.Assert(kind is InterpolatedStringKind.SingleLineRaw or InterpolatedStringKind.MultiLineRaw);
                    ScanRawInterpolatedStringLiteralEnd(kind, startingQuoteCount);
                }

                // Note: this range may be empty.  For example, if we hit the end of a line for a single-line construct,
                // or we hit the end of a file for a multi-line construct.
                closeQuoteRange = closeQuotePosition.._lexer.TextWindow.Position;
            }

            private void ScanNormalOrVerbatimInterpolatedStringLiteralEnd(InterpolatedStringKind kind)
            {
                Debug.Assert(kind is InterpolatedStringKind.Normal or InterpolatedStringKind.Verbatim);

                if (_lexer.TextWindow.PeekChar() != '"')
                {
                    // Didn't find a closing quote.  We hit the end of a line (in the normal case) or the end of the
                    // file in the normal/verbatim case.
                    Debug.Assert(IsAtEnd(kind));

                    TrySetError(_lexer.MakeError(
                        IsAtEnd(allowNewline: true) ? _lexer.TextWindow.Position - 1 : _lexer.TextWindow.Position,
                        width: 1, ErrorCode.ERR_UnterminatedStringLit));
                }
                else
                {
                    // found the closing quote
                    _lexer.TextWindow.AdvanceChar(); // "
                }
            }

            private void ScanRawInterpolatedStringLiteralEnd(InterpolatedStringKind kind, int startingQuoteCount)
            {
                Debug.Assert(kind is InterpolatedStringKind.SingleLineRaw or InterpolatedStringKind.MultiLineRaw);

                if (kind is InterpolatedStringKind.SingleLineRaw)
                {
                    if (_lexer.TextWindow.PeekChar() != '"')
                    {
                        // Didn't find a closing quote.  We hit the end of a line (in the normal case) or the end of the
                        // file in the normal/verbatim case.
                        Debug.Assert(IsAtEnd(kind));

                        TrySetError(_lexer.MakeError(
                            IsAtEnd(allowNewline: true) ? _lexer.TextWindow.Position - 1 : _lexer.TextWindow.Position,
                            width: 1, ErrorCode.ERR_UnterminatedRawString));
                    }
                    else
                    {
                        var closeQuoteCount = _lexer.ConsumeQuoteSequence();

                        // We should only hit here if we had enough close quotes to end the string.  If we didn't have
                        // enough they should have just have been consumed as content, and we'd hit the 'true' case in
                        // this 'if' instead.
                        //
                        // If we have too many close quotes for this string, report an error on the excess quotes so the
                        // user knows how many they need to delete.
                        Debug.Assert(closeQuoteCount >= startingQuoteCount);
                        if (closeQuoteCount > startingQuoteCount)
                        {
                            var excessQuoteCount = closeQuoteCount - startingQuoteCount;
                            TrySetError(_lexer.MakeError(
                                position: _lexer.TextWindow.Position - excessQuoteCount,
                                width: excessQuoteCount,
                                ErrorCode.ERR_TooManyQuotesForRawString));
                        }
                    }
                }
                else
                {
                    // A multiline literal might end either because:
                    //
                    // 1. we hit the end of the file.
                    // 2. we hit quotes *after* content on a line.
                    // 3. we found the legitimate end to the literal.

                    if (IsAtEnd(kind))
                    {
                        TrySetError(_lexer.MakeError(
                            _lexer.TextWindow.Position - 1, width: 1, ErrorCode.ERR_UnterminatedRawString));
                    }
                    else if (_lexer.TextWindow.PeekChar() == '"')
                    {
                        // Don't allow a content line to contain a quote sequence that looks like a delimiter (or longer)
                        var closeQuoteCount = _lexer.ConsumeQuoteSequence();

                        // We must have too many close quotes.  If we had less, they would have just been consumed as content.
                        Debug.Assert(closeQuoteCount >= startingQuoteCount);

                        TrySetError(_lexer.MakeError(
                            position: _lexer.TextWindow.Position - closeQuoteCount,
                            width: closeQuoteCount,
                            ErrorCode.ERR_RawStringDelimiterOnOwnLine));
                    }
                    else
                    {
                        _lexer.TextWindow.AdvancePastNewLine();
                        _lexer.ConsumeWhitespace(builder: null);

                        var closeQuoteCount = _lexer.ConsumeQuoteSequence();

                        // We should only hit here if we had enough close quotes to end the string.  If we didn't have
                        // enough they should have just have been consumed as content, and we'd hit one of the above cases
                        // instead.
                        Debug.Assert(closeQuoteCount >= startingQuoteCount);
                        if (closeQuoteCount > startingQuoteCount)
                        {
                            var excessQuoteCount = closeQuoteCount - startingQuoteCount;
                            TrySetError(_lexer.MakeError(
                                position: _lexer.TextWindow.Position - excessQuoteCount,
                                width: excessQuoteCount,
                                ErrorCode.ERR_TooManyQuotesForRawString));
                        }
                    }
                }
            }

            private void ScanInterpolatedStringLiteralContents(
                InterpolatedStringKind kind, int startingDollarSignCount, int startingQuoteCount, ArrayBuilder<Interpolation>? interpolations)
            {
                // Check for the trivial multi-line raw string literal of the form:
                //
                // $"""
                //  """
                //
                // And give the special message that a content line is required in the literal.
                if (CheckForIllegalEmptyMultiLineRawStringLiteral(kind, startingQuoteCount))
                    return;

                while (true)
                {
                    if (IsAtEnd(kind))
                    {
                        // error: end of line/file before end of string pop out. Error will be reported in
                        // ScanInterpolatedStringLiteralEnd
                        return;
                    }

                    if (IsAtEndOfMultiLineRawLiteral(kind, startingQuoteCount))
                        return;

                    switch (_lexer.TextWindow.PeekChar())
                    {
                        case '"':
                            // Depending on the type of string or the escapes involved, this may be the end of the
                            // string literal, or it may just be content.
                            if (IsEndDelimiterOtherwiseConsume(kind, startingQuoteCount))
                                return;

                            continue;
                        case '}':
                            HandleCloseBraceInContent(kind, startingDollarSignCount);
                            continue;
                        case '{':
                            HandleOpenBraceInContent(kind, startingDollarSignCount, interpolations);
                            continue;
                        case '\\':
                            // In a normal interpolated string a backslash starts an escape. In all other interpolated
                            // strings it's just a backslash.
                            if (kind == InterpolatedStringKind.Normal)
                            {
                                var escapeStart = _lexer.TextWindow.Position;
                                char ch = _lexer.ScanEscapeSequence(surrogateCharacter: out _);
                                if (ch is '{' or '}')
                                {
                                    TrySetError(_lexer.MakeError(escapeStart, _lexer.TextWindow.Position - escapeStart, ErrorCode.ERR_EscapedCurly, ch));
                                }
                            }
                            else
                            {
                                _lexer.TextWindow.AdvanceChar();
                            }

                            continue;

                        default:
                            // found some other character in the string portion.  Just consume it as content and continue.
                            _lexer.TextWindow.AdvanceChar();
                            continue;
                    }
                }
            }

            private bool CheckForIllegalEmptyMultiLineRawStringLiteral(InterpolatedStringKind kind, int startingQuoteCount)
            {
                if (kind == InterpolatedStringKind.MultiLineRaw)
                {
                    _lexer.ConsumeWhitespace(builder: null);
                    var beforeQuotesPosition = _lexer.TextWindow.Position;
                    var closeQuoteCount = _lexer.ConsumeQuoteSequence();

                    if (closeQuoteCount >= startingQuoteCount)
                    {
                        // Found the end of the string.  reset our position so that ScanInterpolatedStringLiteralEnd
                        // can consume it.
                        this.TrySetError(_lexer.MakeError(
                            _lexer.TextWindow.Position - closeQuoteCount, closeQuoteCount, ErrorCode.ERR_RawStringMustContainContent));
                        _lexer.TextWindow.Reset(beforeQuotesPosition);
                        return true;
                    }
                }

                return false;
            }

            private bool IsAtEndOfMultiLineRawLiteral(InterpolatedStringKind kind, int startingQuoteCount)
            {
                if (kind == InterpolatedStringKind.MultiLineRaw)
                {
                    // A multiline string ends with a newline, whitespace and at least as many quotes as we started with.

                    var startPosition = _lexer.TextWindow.Position;
                    if (SyntaxFacts.IsNewLine(_lexer.TextWindow.PeekChar()))
                    {
                        _lexer.TextWindow.AdvancePastNewLine();
                        _lexer.ConsumeWhitespace(builder: null);
                        var closeQuoteCount = _lexer.ConsumeQuoteSequence();

                        _lexer.TextWindow.Reset(startPosition);

                        if (closeQuoteCount >= startingQuoteCount)
                            return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Returns <see langword="true"/> if the quote was an end delimiter and lexing of the contents of the
            /// interpolated string literal should stop.  If it was an end delimiter it will not be consumed.  If it is
            /// content and should not terminate the string then it will be consumed by this method.
            /// </summary>
            private bool IsEndDelimiterOtherwiseConsume(InterpolatedStringKind kind, int startingQuoteCount)
            {
                if (kind is InterpolatedStringKind.Normal or InterpolatedStringKind.Verbatim)
                {
                    // When recovering from mismatched delimiters, we consume the next sequence of quote
                    // characters as the close quote for the interpolated string. In practice this gets us
                    // out of trouble in scenarios we've encountered. See, for example,
                    // https://github.com/dotnet/roslyn/issues/44789
                    if (this.RecoveringFromRunawayLexing())
                    {
                        return true;
                    }

                    if (kind == InterpolatedStringKind.Normal)
                    {
                        // Was in a normal $"  string, the next " closes us.
                        return true;
                    }

                    Debug.Assert(kind == InterpolatedStringKind.Verbatim);
                    // In a verbatim string a "" sequence is an escape. Otherwise this terminates us.
                    if (_lexer.TextWindow.PeekChar(1) != '"')
                    {
                        return true;
                    }

                    // Was just escaped content.  Consume it.
                    _lexer.TextWindow.AdvanceChar(2); // ""
                }
                else
                {
                    Debug.Assert(kind is InterpolatedStringKind.SingleLineRaw or InterpolatedStringKind.MultiLineRaw);

                    var beforeQuotePosition = _lexer.TextWindow.Position;
                    var currentQuoteCount = _lexer.ConsumeQuoteSequence();
                    if (currentQuoteCount >= startingQuoteCount)
                    {
                        // we saw a long enough sequence of close quotes to finish us.  Move back to before the close quotes
                        // and let the caller handle this (including error-ing if there are too many close quotes, or if the
                        // close quotes are in the wrong location).
                        _lexer.TextWindow.Reset(beforeQuotePosition);
                        return true;
                    }
                }

                // otherwise, these were just quotes that we should treat as raw content.
                return false;
            }

            private void HandleCloseBraceInContent(InterpolatedStringKind kind, int startingDollarSignCount)
            {
                if (kind is InterpolatedStringKind.Normal or InterpolatedStringKind.Verbatim)
                {
                    var pos = _lexer.TextWindow.Position;
                    _lexer.TextWindow.AdvanceChar(); // }

                    // ensure any } characters are doubled up
                    if (_lexer.TextWindow.PeekChar() == '}')
                    {
                        _lexer.TextWindow.AdvanceChar(); // }
                    }
                    else
                    {
                        TrySetError(_lexer.MakeError(pos, 1, ErrorCode.ERR_UnescapedCurly, "}"));
                    }
                }
                else
                {
                    Debug.Assert(kind is InterpolatedStringKind.MultiLineRaw or InterpolatedStringKind.SingleLineRaw);

                    // A close quote is normally fine as content in a raw interpolated string literal. However, similar
                    // to the rules around quotes, we do not allow a subsequence of braces to be longer than the number
                    // of `$`s the literal starts with.  Note: this restriction is only on *content*.  It acceptable to
                    // have a sequence of braces be longer, as long as it is part content and also part of an
                    // interpolation.  In that case, the content portion must abide by this rule.
                    var closeBraceCount = _lexer.ConsumeCloseBraceSequence();
                    if (closeBraceCount >= startingDollarSignCount)
                    {
                        TrySetError(_lexer.MakeError(
                            position: _lexer.TextWindow.Position - closeBraceCount,
                            width: closeBraceCount,
                            ErrorCode.ERR_TooManyCloseBracesForRawString));
                    }
                }
            }

            private void HandleOpenBraceInContent(InterpolatedStringKind kind, int startingDollarSignCount, ArrayBuilder<Interpolation>? interpolations)
            {
                if (kind is InterpolatedStringKind.Normal or InterpolatedStringKind.Verbatim)
                {
                    HandleOpenBraceInNormalOrVerbatimContent(kind, interpolations);
                }
                else
                {
                    HandleOpenBraceInRawContent(kind, startingDollarSignCount, interpolations);
                }
            }

            private void HandleOpenBraceInNormalOrVerbatimContent(InterpolatedStringKind kind, ArrayBuilder<Interpolation>? interpolations)
            {
                Debug.Assert(kind is InterpolatedStringKind.Normal or InterpolatedStringKind.Verbatim);
                if (_lexer.TextWindow.PeekChar(1) == '{')
                {
                    _lexer.TextWindow.AdvanceChar(2); // {{
                }
                else
                {
                    int openBracePosition = _lexer.TextWindow.Position;
                    _lexer.TextWindow.AdvanceChar();
                    ScanInterpolatedStringLiteralHoleBalancedText(kind, '}', isHole: true, out var colonRange);
                    int closeBracePosition = _lexer.TextWindow.Position;
                    if (_lexer.TextWindow.PeekChar() == '}')
                    {
                        _lexer.TextWindow.AdvanceChar();
                    }
                    else
                    {
                        TrySetError(_lexer.MakeError(openBracePosition - 1, 2, ErrorCode.ERR_UnclosedExpressionHole));
                    }

                    interpolations?.Add(new Interpolation(
                        new Range(openBracePosition, openBracePosition + 1),
                        colonRange,
                        new Range(closeBracePosition, _lexer.TextWindow.Position)));
                }
            }

            private void HandleOpenBraceInRawContent(InterpolatedStringKind kind, int startingDollarSignCount, ArrayBuilder<Interpolation>? interpolations)
            {
                Debug.Assert(kind is InterpolatedStringKind.SingleLineRaw or InterpolatedStringKind.MultiLineRaw);

                // In raw content we are allowed to see up to 2*N-1 open (or close) braces.  For example, if the string
                // literal starts with `$$$"""` then we can see up to `2*3-1 = 5` braces like so `$$$""" {{{{{`.  The
                // inner three braces start the interpolation.  The outer two braces are just content.  This ensures the
                // rule that the content cannot contain a sequence of open or close braces equal to (or longer than) the
                // dollar sequence.
                var beforeOpenBracesPosition = _lexer.TextWindow.Position;
                var openBraceCount = _lexer.ConsumeOpenBraceSequence();
                if (openBraceCount < startingDollarSignCount)
                {
                    // not enough open braces to matter.  Just treat as content.
                    return;
                }

                var afterOpenBracePosition = _lexer.TextWindow.Position;
                if (openBraceCount >= 2 * startingDollarSignCount)
                {
                    // Too many open braces.  Report an error on the portion up before the section that counts as the
                    // start of the interpolation.
                    TrySetError(_lexer.MakeError(
                        beforeOpenBracesPosition,
                        width: openBraceCount - startingDollarSignCount,
                        ErrorCode.ERR_TooManyOpenBracesForRawString));
                }

                // Now, try to scan the contents of the interpolation.  Ending when we hit a close brace.
                ScanInterpolatedStringLiteralHoleBalancedText(kind, '}', isHole: true, out var colonRange);

                var beforeCloseBracePosition = _lexer.TextWindow.Position;
                var closeBraceCount = _lexer.ConsumeCloseBraceSequence();

                if (closeBraceCount == 0)
                {
                    // Didn't find any close braces.  Report a particular error on the open braces that they are unclosed.
                    TrySetError(_lexer.MakeError(
                        position: afterOpenBracePosition - startingDollarSignCount,
                        width: startingDollarSignCount,
                        ErrorCode.ERR_UnclosedExpressionHole));
                }
                else if (closeBraceCount < startingDollarSignCount)
                {
                    // not enough close braces to end the interpolation.  Report here.
                    TrySetError(_lexer.MakeError(
                        beforeOpenBracesPosition,
                        width: openBraceCount - startingDollarSignCount,
                        ErrorCode.ERR_NotEnoughCloseBracesForRawString));
                }
                else
                {
                    // Only consume up to the minimum number of close braces we need to end the interpolation. Any
                    // excess will be consumed in the content consumption pass in ScanInterpolatedStringLiteralContents.
                    _lexer.TextWindow.Reset(beforeCloseBracePosition + startingDollarSignCount);
                }

                interpolations?.Add(new Interpolation(
                    (afterOpenBracePosition - startingDollarSignCount)..afterOpenBracePosition,
                    colonRange,
                    beforeCloseBracePosition.._lexer.TextWindow.Position));
            }

            private void ScanFormatSpecifier(InterpolatedStringKind kind)
            {
                /*
                ## Grammar from spec:
                
                interpolation_format
                    : ':' interpolation_format_character+
                ; 
                interpolation_format_character
                    : '<Any character except \" (U+0022), : (U+003A), { (U+007B) and } (U+007D)>'
                ;
                 */

                Debug.Assert(_lexer.TextWindow.PeekChar() == ':');
                _lexer.TextWindow.AdvanceChar();
                while (true)
                {
                    char ch = _lexer.TextWindow.PeekChar();
                    if (ch == '\\' && kind is InterpolatedStringKind.Normal)
                    {
                        // normal string & char constants can have escapes
                        var pos = _lexer.TextWindow.Position;
                        ch = _lexer.ScanEscapeSequence(surrogateCharacter: out _);
                        if (ch is '{' or '}')
                        {
                            TrySetError(_lexer.MakeError(pos, 1, ErrorCode.ERR_EscapedCurly, ch));
                        }
                    }
                    else if (ch == '"')
                    {
                        if (kind is InterpolatedStringKind.Verbatim && _lexer.TextWindow.PeekChar(1) == '"')
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
                        TrySetError(_lexer.MakeError(
                            _lexer.TextWindow.Position, 1, ErrorCode.ERR_UnexpectedCharacter, ch));
                        _lexer.TextWindow.AdvanceChar();
                    }
                    else if (ch == '}')
                    {
                        return; // end of interpolation
                    }
                    else if (IsAtEnd(allowNewline: true))
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
            private void ScanInterpolatedStringLiteralHoleBalancedText(InterpolatedStringKind kind, char endingChar, bool isHole, out Range colonRange)
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
                            TrySetError(_lexer.MakeError(_lexer.TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString()));
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
                                ScanFormatSpecifier(kind);
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

                            TrySetError(_lexer.MakeError(_lexer.TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString()));
                            goto default;
                        case '"':
                            if (RecoveringFromRunawayLexing())
                            {
                                // When recovering from mismatched delimiters, we consume the next
                                // quote character as the close quote for the interpolated string. In
                                // practice this gets us out of trouble in scenarios we've encountered.
                                // See, for example, https://github.com/dotnet/roslyn/issues/44789
                                return;
                            }

                            // handle string literal inside an expression hole.
                            ScanInterpolatedStringLiteralNestedString();
                            continue;

                        case '\'':
                            // handle character literal inside an expression hole.
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
                            ScanInterpolatedStringLiteralHoleBracketed(kind, '{', '}');
                            continue;
                        case '(':
                            // TODO: after the colon this has no special meaning.
                            ScanInterpolatedStringLiteralHoleBracketed(kind, '(', ')');
                            continue;
                        case '[':
                            // TODO: after the colon this has no special meaning.
                            ScanInterpolatedStringLiteralHoleBracketed(kind, '[', ']');
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
            private bool RecoveringFromRunawayLexing() => Error != null;

            private void ScanInterpolatedStringLiteralNestedString()
            {
                var info = default(TokenInfo);
                _lexer.ScanStringLiteral(ref info, inDirective: false);
            }

            private void ScanInterpolatedStringLiteralHoleBracketed(InterpolatedStringKind kind, char start, char end)
            {
                Debug.Assert(start == _lexer.TextWindow.PeekChar());
                _lexer.TextWindow.AdvanceChar();
                ScanInterpolatedStringLiteralHoleBalancedText(kind, end, isHole: false, colonRange: out _);
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
