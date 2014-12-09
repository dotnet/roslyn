// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class LanguageParser
    {
        /// <summary>
        /// "Safe" substring using start and end positions rather than start and length.
        /// If things are out of bounds just returns the whole string. That should only
        /// be used by clients to assist in error recovery.
        /// <param name="s">original string</param>
        /// <param name="first">index of first character to be included</param>
        /// <param name="last">index of last character to be included</param>
        /// </summary>
        private string Substring(string s, int first, int last)
        {
            int len = last - first + 1;
            return (last > s.Length || len < 0) ? s : s.Substring(first, len);
        }

        private ExpressionSyntax ParseInterpolatedStringToken()
        {
            // We don't want to make the scanner stateful (between tokens) if we can possibly avoid it.
            // The approach implemented here is
            //
            // (1) Scan the whole interpolated string literal as a single token. Now the statefulness of
            // the scanner (to match { }'s) is limited to its behavior while scanning a single token.
            //
            // (2) When the parser gets such a token, here, it spins up another scanner / parser on each of
            // the holes and builds a tree for the whole thing (resulting in an interpolated string nonterminal node).
            //
            // (3) The parser discards the original token and replaces it with this tree. (In other words,
            // it replaces one token with a different set of tokens that have already been parsed)
            //
            // (4) On an incremental change, we widen the invalidated region to include any enclosing interpolated
            // string nonterminal so that we never reuse tokens inside a changed interpolated string.
            //
            // This has the secondary advantage that it can reasonably be specified.
            // 
            // The substitution will end up being invisible to external APIs and clients such as the IDE, as
            // they have no way to ask for the stream of tokens before parsing.
            //
            // Take special not of the handling of trivia. With one exception, what happens in a hole stays
            // in the hole. That means that the first token in a hole can have leading trivia, even though
            // it is not the first thing on a line. The single exception occurs when the hole is completely
            // empty of tokens, but contains some trivia. In that case we move the trivia (from the fake EOF
            // token inside the hole) to be leading trivia of the following literal part so that it doesn't
            // get dropped on the floor.
            //

            var originalToken = this.EatToken();
            var originalText = originalToken.ValueText; // this is actually the source text
            Debug.Assert(originalText[0] == '$');
            var isVerbatim = originalText.Length > 2 && originalText[1] == '@';
            Debug.Assert(originalToken.Kind == SyntaxKind.InterpolatedStringToken);
            var interpolations = ArrayBuilder<Lexer.Interpolation>.GetInstance();
            SyntaxDiagnosticInfo error = null;
            using (var tempLexer = new Lexer(Text.SourceText.From(originalText), this.Options, allowPreprocessorDirectives: false))
            {
                // compute the positions of the interpolations in the original string literal, and also compute/preserve
                // lexical errors
                var info = default(Lexer.TokenInfo);
                tempLexer.ScanInterpolatedStringLiteralTop(interpolations, isVerbatim, ref info, ref error);
            }

            var builder = this.pool.AllocateSeparated<InterpolatedStringInsertSyntax>();
            try
            {
                SyntaxToken stringStart = null;
                SyntaxToken stringEnd = null;
                if (interpolations.Count == 0)
                {
                    // In the special case when there are no interpolations, we just construct a format string
                    // with no inserts. We must still use String.Format to get its handling of escapes such as {{
                    var startText = Substring(originalText, isVerbatim ? 3 : 2, originalText.Length - 1);
                    stringStart = MakeStringToken(originalText, startText, isVerbatim, SyntaxKind.InterpolatedStringStartToken).WithLeadingTrivia(originalToken.GetLeadingTrivia());
                    stringEnd = SyntaxFactory.Literal(null, string.Empty, SyntaxKind.InterpolatedStringEndToken, string.Empty, null).WithTrailingTrivia(originalToken.GetTrailingTrivia());
                }
                else
                {
                    for (int i = 0; i < interpolations.Count; i++)
                    {
                        var interpolation = interpolations[i];
                        var first = i == 0;
                        var last = i == (interpolations.Count - 1);

                        if (first)
                        {
                            // compute stringStart
                            var startText1 = Substring(originalText, 0, interpolation.Start);
                            var startText2 = Substring(originalText, isVerbatim ? 3 : 2, interpolation.Start - 1);
                            stringStart = MakeStringToken(startText1, startText2, isVerbatim, SyntaxKind.InterpolatedStringStartToken).WithLeadingTrivia(originalToken.GetLeadingTrivia());
                            Debug.Assert(stringStart.Kind == SyntaxKind.InterpolatedStringStartToken);
                        }

                        CSharpSyntaxNode additionalTrivia;
                        var hasFormatSpecifier = interpolation.Colon != 0;
                        var end = hasFormatSpecifier ? interpolation.Colon : interpolation.End;
                        var interpText = Substring(originalText, interpolation.Start + 1, end - 1);
                        using (var tempLexer = new Lexer(Text.SourceText.From(interpText), this.Options, allowPreprocessorDirectives: false))
                        {
                            using (var tempParser = new LanguageParser(tempLexer, null, null))
                            {
                                ExpressionSyntax expr;
                                SyntaxToken commaToken;
                                ExpressionSyntax alignmentExpression;
                                tempParser.ParseInterpolationStart(out expr, out commaToken, out alignmentExpression);
                                // In case the insert is empty, move the leading trivia from its EOF token to the following token.
                                additionalTrivia = tempParser.CurrentToken.GetLeadingTrivia();

                                var formatToken = default(SyntaxToken);
                                if (hasFormatSpecifier)
                                {
                                    additionalTrivia = null;
                                    var formatString1 = Substring(originalText, interpolation.Colon, interpolation.End - 1);
                                    var formatString2 = Substring(originalText, interpolation.Colon + 1, interpolation.End - 1);
                                    formatToken = MakeStringToken(formatString1, formatString2, isVerbatim, SyntaxKind.StringLiteralToken);
                                    var text = formatToken.ValueText;
                                    if (text.Length == 0)
                                    {
                                        formatToken = AddError(formatToken, ErrorCode.ERR_EmptyFormatSpecifier);
                                    }
                                    else if (SyntaxFacts.IsWhitespace(text[text.Length - 1]) || SyntaxFacts.IsNewLine(text[text.Length - 1]))
                                    {
                                        formatToken = AddError(formatToken, ErrorCode.ERR_TrailingWhitespaceInFormatSpecifier);
                                    }
                                }

                                var insert = SyntaxFactory.InterpolatedStringInsert(expr, commaToken, alignmentExpression, formatToken);
                                builder.Add(insert);
                            }
                        }

                        if (last)
                        {
                            // compute stringEnd
                            var endText1 = originalText.Substring(interpolation.End);
                            var endText2 = Substring(originalText, interpolation.End + 1, originalText.Length - 2);
                            stringEnd = MakeStringToken(endText1, endText2, isVerbatim, SyntaxKind.InterpolatedStringEndToken).WithLeadingTrivia(additionalTrivia).WithTrailingTrivia(originalToken.GetTrailingTrivia());
                            Debug.Assert(stringEnd.Kind == SyntaxKind.InterpolatedStringEndToken);
                        }
                        else
                        {
                            // add an interpolated string mid token for the following }...{ part
                            var midText1 = Substring(originalText, interpolation.End, interpolations[i + 1].Start);
                            var midText2 = Substring(originalText, interpolation.End + 1, interpolations[i + 1].Start - 1);
                            var stringMid = MakeStringToken(midText1, midText2, isVerbatim, SyntaxKind.InterpolatedStringMidToken).WithLeadingTrivia(additionalTrivia);
                            Debug.Assert(stringMid.Kind == SyntaxKind.InterpolatedStringMidToken);
                            builder.AddSeparator(stringMid);
                        }

                    }
                }

                interpolations.Free();
                var result = SyntaxFactory.InterpolatedString(stringStart, builder.ToList(), stringEnd);
                if (error != null) result = result.WithDiagnosticsGreen(new[] { error });
                return result;
            }
            finally
            {
                this.pool.Free(builder);
            }
        }

        /// <summary>
        /// Take the given text and treat it as the contents of a string literal, returning a token for that.
        /// </summary>
        /// <param name="text">The text for the full string literal, including the quotes and contents</param>
        /// <param name="bodyText">The text for the string literal's contents, excluding surrounding quotes</param>
        /// <param name="isVerbatim">True if the string contents should be scanned using the rules for verbatim strings</param>
        /// <param name="kind">The token kind to be assigned to the resulting token</param>
        SyntaxToken MakeStringToken(string text, string bodyText, bool isVerbatim, SyntaxKind kind)
        {
            var fakeString = (isVerbatim ? "@\"" : "\"") + bodyText + "\"";
            using (var tempLexer = new Lexer(Text.SourceText.From(fakeString), this.Options, allowPreprocessorDirectives: false))
            {
                var info = default(Lexer.TokenInfo);
                if (isVerbatim) tempLexer.ScanVerbatimStringLiteral(ref info); else tempLexer.ScanStringLiteral(ref info);
                Debug.Assert(info.Kind == SyntaxKind.StringLiteralToken);
                return SyntaxFactory.Literal(null, text, kind, info.StringValue, null);
            }
        }

        private void ParseInterpolationStart(out ExpressionSyntax expr, out SyntaxToken commaToken, out ExpressionSyntax alignmentExpression)
        {
            expr = this.ParseExpression();
            if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                commaToken = this.EatToken(SyntaxKind.CommaToken);
                // require the width to be a literal (one token) or the negative of a literal (two tokens)
                var minusToken = (this.CurrentToken.Kind == SyntaxKind.MinusToken) ? this.EatToken(SyntaxKind.MinusToken) : null;
                var widthToken = this.EatToken(SyntaxKind.NumericLiteralToken);
                var widthExpression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, widthToken);
                alignmentExpression = (minusToken != null) ? (ExpressionSyntax)SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, minusToken, widthExpression) : widthExpression;
                alignmentExpression = ConsumeUnexpectedTokens(alignmentExpression);
            }
            else
            {
                commaToken = default(SyntaxToken);
                alignmentExpression = null;
                expr = ConsumeUnexpectedTokens(expr);
            }
        }
    }
}
