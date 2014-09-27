using Microsoft.CodeAnalysis.Collections;
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
        private void ScanInterpolatedStringLiteral(ref TokenInfo info)
        {
            // We have a string of the form
            //                " ... "
            // Where the contents contains one or more sequences
            //                \{ STUFF }
            // where these curly braces delimit STUFF in expression "holes".
            // In order to properly find the closing quote of the whole string,
            // we need to locate the closing brace of each hole, as strings
            // may appear in expressions in the holes. So we
            // need to match up any braces that appear between them.
            // But in order to do that, we also need to match up any
            // /**/ comments, ' characters quotes, () parens
            // [] brackets, and "" strings, including interpolated holes in the latter.

            SyntaxDiagnosticInfo error = null;
            ScanISLTop(null, ref info, ref error);
            this.AddError(error);
            info.Text = TextWindow.GetText(false);
        }

        internal struct Interpolation
        {
            public readonly int Start;
            public readonly int End;
            public Interpolation(int start, int end)
            {
                this.Start = start;
                this.End = end;
            }
        }

        internal void ScanISLTop(ArrayBuilder<Interpolation> interpolations, ref TokenInfo info, ref SyntaxDiagnosticInfo error)
        {
            Debug.Assert(TextWindow.PeekChar() == '\"');
            TextWindow.AdvanceChar(); // "
            ScanISLContents(interpolations, ref error);
            if (IsAtEnd() || TextWindow.PeekChar() != '\"')
            {
                if (error == null) error = MakeError(TextWindow.Position, 1, ErrorCode.ERR_NewlineInConst);
            }
            else
            {
                // found the closing quote
                TextWindow.AdvanceChar(); // "
            }

            info.Kind = SyntaxKind.InterpolatedStringToken;
        }

        /// <summary>
        /// Turn a (parsed) interpolated string nonterminal into an interpolated string token.
        /// </summary>
        /// <param name="interpolatedString"></param>
        /// <returns></returns>
        static internal SyntaxToken RescanInterpolatedString(InterpolatedStringSyntax interpolatedString)
        {
            var text = interpolatedString.ToString();
            // TODO: scan the contents to reconstruct any lexical errors such as // inside an expression hole
            return SyntaxFactory.Literal(interpolatedString.GetLeadingTrivia(), text, SyntaxKind.InterpolatedStringToken, text, interpolatedString.GetTrailingTrivia());
        }

        private void ScanISLContents(ArrayBuilder<Interpolation> interpolations, ref SyntaxDiagnosticInfo error)
        {
            while (true)
            {
                if (IsAtEnd())
                {
                    // error: end of line before end of string
                    return;
                }
                switch (TextWindow.PeekChar())
                {
                    case '\"':
                        // found the end of the string
                        return;
                    case '\\':
                        TextWindow.AdvanceChar();
                        if (IsAtEnd())
                        {
                            // the caller will complain about unclosed quote
                            return;
                        }
                        else if (TextWindow.PeekChar() == '{')
                        {
                            int interpolationStart = TextWindow.Position;
                            TextWindow.AdvanceChar();
                            ScanISLHoleBalancedText('}', true, ref error);
                            int end = TextWindow.Position;
                            if (TextWindow.PeekChar() == '}')
                            {
                                TextWindow.AdvanceChar();
                            }
                            else
                            {
                                if (error == null) error = MakeError(interpolationStart-1, 2, ErrorCode.ERR_UnclosedExpressionHole);
                            }
                            if (interpolations != null) interpolations.Add(new Interpolation(interpolationStart, end));
                        }
                        else
                        {
                            TextWindow.AdvanceChar(); // skip past a single escaped character
                        }
                        continue;
                    default:
                        // found some other character in the string portion
                        TextWindow.AdvanceChar();
                        continue;
                }
            }
        }

        private bool IsAtEnd()
        {
            char ch = TextWindow.PeekChar();
            return SyntaxFacts.IsNewLine(ch) || (ch == SlidingTextWindow.InvalidCharacter && TextWindow.IsReallyAtEnd());
        }

        /// <summary>
        /// Scan past the hole inside an interpolated string literal, leaving the current character on the '}' (if any)
        /// </summary>
        private void ScanISLHoleBalancedText(char endingChar, bool isHole, ref SyntaxDiagnosticInfo error)
        {
            while (true)
            {
                if (IsAtEnd())
                {
                    // the caller will complain
                    return;
                }
                char ch = TextWindow.PeekChar();
                switch (ch)
                {
                    case '}':
                    case ')':
                    case ']':
                        if (ch == endingChar) return;
                        if (error == null) error = MakeError(TextWindow.Position, 1, ErrorCode.ERR_SyntaxError, endingChar.ToString());
                        goto default;
                    case '\"':
                    case '\'':
                        // handle string or character literal inside an expression hole.
                        ScanISLNestedString(ch, ref error);
                        continue;
                    case '@':
                        if (TextWindow.PeekChar(1) == '\"')
                        {
                            // check for verbatim string inside an expression hole.
                            ScanISLNestedVerbatimString(ref error);
                        }
                        goto default;
                    case '/':
                        switch (TextWindow.PeekChar(1))
                        {
                            case '/':
                                // error: single-line comment not allowed in an interpolated string
                                if (error == null) error = MakeError(TextWindow.Position, 2, ErrorCode.ERR_SingleLineCommentInExpressionHole);
                                TextWindow.AdvanceChar();
                                TextWindow.AdvanceChar();
                                continue;
                            case '*':
                                // check for and scan /* comment */
                                ScanISLNestedComment();
                                continue;
                            default:
                                TextWindow.AdvanceChar();
                                continue;
                        }
                    case '{':
                        ScanISLHoleBracketed('{', '}', ref error);
                        continue;
                    case '(':
                        ScanISLHoleBracketed('(', ')', ref error);
                        continue;
                    case '[':
                        ScanISLHoleBracketed('[', ']', ref error);
                        continue;
                    default:
                        // part of code in the expression hole
                        TextWindow.AdvanceChar();
                        continue;
                }
            }

        }

        private void ScanISLNestedComment()
        {
            Debug.Assert(TextWindow.PeekChar() == '/');
            TextWindow.AdvanceChar();
            Debug.Assert(TextWindow.PeekChar() == '*');
            TextWindow.AdvanceChar();
            while (true)
            {
                if (IsAtEnd())
                {
                    return; // let the caller complain about the unterminated quote
                }
                var ch = TextWindow.PeekChar();
                TextWindow.AdvanceChar();
                if (ch == '*' && TextWindow.PeekChar() == '/')
                {
                    TextWindow.AdvanceChar(); // skip */
                    return;
                }
            }
        }

        private void ScanISLNestedString(char quote, ref SyntaxDiagnosticInfo error)
        {
            Debug.Assert(TextWindow.PeekChar() == quote);
            TextWindow.AdvanceChar(); // move past quote
            while (true)
            {
                if (IsAtEnd())
                {
                    // we'll get an error in the enclosing construct
                    return;
                }
                char ch = TextWindow.PeekChar();
                TextWindow.AdvanceChar();
                switch (ch)
                {
                    case '\"':
                    case '\'':
                        if (ch == quote)
                        {
                            return;
                        }
                        break;
                    case '\\':
                        ch = TextWindow.PeekChar();
                        if (IsAtEnd())
                        {
                            return;
                        }
                        else if (ch == '{' && quote == '"')
                        {
                            TextWindow.AdvanceChar(); // move past {
                            ScanISLHoleBalancedText('}', true, ref error);
                            if (TextWindow.PeekChar() == '}')
                            {
                                TextWindow.AdvanceChar();
                            }
                        }
                        else
                        {
                            TextWindow.AdvanceChar(); // move past one escaped character
                        }
                        break;
                }
            }
        }

        private void ScanISLNestedVerbatimString(ref SyntaxDiagnosticInfo error)
        {
            Debug.Assert(TextWindow.PeekChar() == '@');
            TextWindow.AdvanceChar();
            Debug.Assert(TextWindow.PeekChar() == '\"');
            TextWindow.AdvanceChar(); // move past quote
            while (true)
            {
                if (IsAtEnd())
                {
                    // we'll get an error in the enclosing construct
                    return;
                }
                char ch = TextWindow.PeekChar();
                TextWindow.AdvanceChar();
                if (ch == '\"')
                {
                    if (TextWindow.PeekChar(1) == '\"')
                    {
                        TextWindow.AdvanceChar(); // move past escaped quote
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        void ScanISLHoleBracketed(char start, char end, ref SyntaxDiagnosticInfo error)
        {
            Debug.Assert(start == TextWindow.PeekChar());
            TextWindow.AdvanceChar();
            ScanISLHoleBalancedText(end, false, ref error);
            if (TextWindow.PeekChar() == end)
            {
                TextWindow.AdvanceChar();
            }
            else
            {
                // an error was given by the caller
            }
        }
    }
}
