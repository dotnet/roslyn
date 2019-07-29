// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class LanguageParser
    {
        /// <summary>
        /// "Safe" substring using start and end positions rather than start and length.
        /// If things are out of bounds just returns the empty string. That should only
        /// be used by clients to assist in error recovery.
        /// <param name="s">original string</param>
        /// <param name="first">index of first character to be included</param>
        /// <param name="last">index of last character to be included</param>
        /// </summary>
        private string Substring(string s, int first, int last)
        {
            if (last >= s.Length)
            {
                last = s.Length - 1;
            }

            int len = last - first + 1;
            return (last > s.Length || len <= 0) ? string.Empty : s.Substring(first, len);
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
            // the holes and builds a tree for the whole thing (resulting in an InterpolatedStringExpressionSyntax).
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

            var originalToken = this.EatToken();
            var originalText = originalToken.ValueText; // this is actually the source text
            Debug.Assert(originalText[0] == '$' || originalText[0] == '@');

            var isAltInterpolatedVerbatim = originalText.Length > 2 && originalText[0] == '@'; // @$
            var isVerbatim = isAltInterpolatedVerbatim || (originalText.Length > 2 && originalText[1] == '@');

            Debug.Assert(originalToken.Kind == SyntaxKind.InterpolatedStringToken);
            var interpolations = ArrayBuilder<Lexer.Interpolation>.GetInstance();
            SyntaxDiagnosticInfo error = null;
            bool closeQuoteMissing;
            using (var tempLexer = new Lexer(Text.SourceText.From(originalText), this.Options, allowPreprocessorDirectives: false))
            {
                // compute the positions of the interpolations in the original string literal, and also compute/preserve
                // lexical errors
                var info = default(Lexer.TokenInfo);
                tempLexer.ScanInterpolatedStringLiteralTop(interpolations, isVerbatim, ref info, ref error, out closeQuoteMissing);
            }

            // Make a token for the open quote $" or $@" or @$"
            var openQuoteIndex = isVerbatim ? 2 : 1;
            Debug.Assert(originalText[openQuoteIndex] == '"');

            var openQuoteKind = isVerbatim
                    ? SyntaxKind.InterpolatedVerbatimStringStartToken // $@ or @$
                    : SyntaxKind.InterpolatedStringStartToken; // $

            var openQuoteText = isAltInterpolatedVerbatim
                ? "@$\""
                : isVerbatim
                    ? "$@\""
                    : "$\"";
            var openQuote = SyntaxFactory.Token(originalToken.GetLeadingTrivia(), openQuoteKind, openQuoteText, openQuoteText, trailing: null);

            if (isAltInterpolatedVerbatim)
            {
                openQuote = CheckFeatureAvailability(openQuote, MessageID.IDS_FeatureAltInterpolatedVerbatimStrings);
            }

            // Make a token for the close quote " (even if it was missing)
            var closeQuoteIndex = closeQuoteMissing ? originalText.Length : originalText.Length - 1;
            Debug.Assert(closeQuoteMissing || originalText[closeQuoteIndex] == '"');
            var closeQuote = closeQuoteMissing
                ? SyntaxFactory.MissingToken(SyntaxKind.InterpolatedStringEndToken).TokenWithTrailingTrivia(originalToken.GetTrailingTrivia())
                : SyntaxFactory.Token(null, SyntaxKind.InterpolatedStringEndToken, originalToken.GetTrailingTrivia());
            var builder = _pool.Allocate<InterpolatedStringContentSyntax>();

            if (interpolations.Count == 0)
            {
                // In the special case when there are no interpolations, we just construct a format string
                // with no inserts. We must still use String.Format to get its handling of escapes such as {{,
                // so we still treat it as a composite format string.
                var text = Substring(originalText, openQuoteIndex + 1, closeQuoteIndex - 1);
                if (text.Length > 0)
                {
                    var token = MakeStringToken(text, text, isVerbatim, SyntaxKind.InterpolatedStringTextToken);
                    builder.Add(SyntaxFactory.InterpolatedStringText(token));
                }
            }
            else
            {
                for (int i = 0; i < interpolations.Count; i++)
                {
                    var interpolation = interpolations[i];

                    // Add a token for text preceding the interpolation
                    var text = Substring(originalText, (i == 0) ? (openQuoteIndex + 1) : (interpolations[i - 1].CloseBracePosition + 1), interpolation.OpenBracePosition - 1);
                    if (text.Length > 0)
                    {
                        var token = MakeStringToken(text, text, isVerbatim, SyntaxKind.InterpolatedStringTextToken);
                        builder.Add(SyntaxFactory.InterpolatedStringText(token));
                    }

                    // Add an interpolation
                    var interp = ParseInterpolation(originalText, interpolation, isVerbatim);
                    builder.Add(interp);
                }

                // Add a token for text following the last interpolation
                var lastText = Substring(originalText, interpolations[interpolations.Count - 1].CloseBracePosition + 1, closeQuoteIndex - 1);
                if (lastText.Length > 0)
                {
                    var token = MakeStringToken(lastText, lastText, isVerbatim, SyntaxKind.InterpolatedStringTextToken);
                    builder.Add(SyntaxFactory.InterpolatedStringText(token));
                }
            }

            interpolations.Free();
            var result = SyntaxFactory.InterpolatedStringExpression(openQuote, builder, closeQuote);
            _pool.Free(builder);
            if (error != null)
            {
                result = result.WithDiagnosticsGreen(new[] { error });
            }

            Debug.Assert(originalToken.ToFullString() == result.ToFullString()); // yield from text equals yield from node
            return CheckFeatureAvailability(result, MessageID.IDS_FeatureInterpolatedStrings);
        }

        private InterpolationSyntax ParseInterpolation(string text, Lexer.Interpolation interpolation, bool isVerbatim)
        {
            SyntaxToken openBraceToken;
            ExpressionSyntax expression;
            InterpolationAlignmentClauseSyntax alignment = null;
            InterpolationFormatClauseSyntax format = null;
            var closeBraceToken = interpolation.CloseBraceMissing
                ? SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken)
                : SyntaxFactory.Token(SyntaxKind.CloseBraceToken);

            var parsedText = Substring(text, interpolation.OpenBracePosition, interpolation.HasColon ? interpolation.ColonPosition - 1 : interpolation.CloseBracePosition - 1);
            using (var tempLexer = new Lexer(Text.SourceText.From(parsedText), this.Options, allowPreprocessorDirectives: false, interpolationFollowedByColon: interpolation.HasColon))
            {
                // TODO: some of the trivia in the interpolation maybe should be trailing trivia of the openBraceToken
                using (var tempParser = new LanguageParser(tempLexer, null, null))
                {
                    SyntaxToken commaToken = null;
                    ExpressionSyntax alignmentExpression = null;
                    tempParser.ParseInterpolationStart(out openBraceToken, out expression, out commaToken, out alignmentExpression);
                    if (alignmentExpression != null)
                    {
                        alignment = SyntaxFactory.InterpolationAlignmentClause(commaToken, alignmentExpression);
                    }

                    var extraTrivia = tempParser.CurrentToken.GetLeadingTrivia();
                    if (interpolation.HasColon)
                    {
                        var colonToken = SyntaxFactory.Token(SyntaxKind.ColonToken).TokenWithLeadingTrivia(extraTrivia);
                        var formatText = Substring(text, interpolation.ColonPosition + 1, interpolation.FormatEndPosition);
                        var formatString = MakeStringToken(formatText, formatText, isVerbatim, SyntaxKind.InterpolatedStringTextToken);
                        format = SyntaxFactory.InterpolationFormatClause(colonToken, formatString);
                    }
                    else
                    {
                        // Move the leading trivia from the insertion's EOF token to the following token.
                        closeBraceToken = closeBraceToken.TokenWithLeadingTrivia(extraTrivia);
                    }
                }
            }

            var result = SyntaxFactory.Interpolation(openBraceToken, expression, alignment, format, closeBraceToken);
            Debug.Assert(Substring(text, interpolation.OpenBracePosition, interpolation.LastPosition) == result.ToFullString()); // yield from text equals yield from node
            return result;
        }

        /// <summary>
        /// Take the given text and treat it as the contents of a string literal, returning a token for that.
        /// </summary>
        /// <param name="text">The text for the full string literal, including the quotes and contents</param>
        /// <param name="bodyText">The text for the string literal's contents, excluding surrounding quotes</param>
        /// <param name="isVerbatim">True if the string contents should be scanned using the rules for verbatim strings</param>
        /// <param name="kind">The token kind to be assigned to the resulting token</param>
        private SyntaxToken MakeStringToken(string text, string bodyText, bool isVerbatim, SyntaxKind kind)
        {
            var prefix = isVerbatim ? "@\"" : "\"";
            var fakeString = prefix + bodyText + "\"";
            using (var tempLexer = new Lexer(Text.SourceText.From(fakeString), this.Options, allowPreprocessorDirectives: false))
            {
                LexerMode mode = LexerMode.Syntax;
                SyntaxToken token = tempLexer.Lex(ref mode);
                Debug.Assert(token.Kind == SyntaxKind.StringLiteralToken);
                var result = SyntaxFactory.Literal(null, text, kind, token.ValueText, null);
                if (token.ContainsDiagnostics)
                {
                    result = result.WithDiagnosticsGreen(MoveDiagnostics(token.GetDiagnostics(), -prefix.Length));
                }

                return result;
            }
        }

        private static DiagnosticInfo[] MoveDiagnostics(DiagnosticInfo[] infos, int offset)
        {
            var builder = ArrayBuilder<DiagnosticInfo>.GetInstance();
            foreach (var info in infos)
            {
                var sd = info as SyntaxDiagnosticInfo;
                builder.Add(sd?.WithOffset(sd.Offset + offset) ?? info);
            }

            return builder.ToArrayAndFree();
        }

        private void ParseInterpolationStart(out SyntaxToken openBraceToken, out ExpressionSyntax expr, out SyntaxToken commaToken, out ExpressionSyntax alignmentExpression)
        {
            openBraceToken = this.EatToken(SyntaxKind.OpenBraceToken);
            expr = this.ParseExpressionCore();
            if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                commaToken = this.EatToken(SyntaxKind.CommaToken);
                alignmentExpression = ConsumeUnexpectedTokens(this.ParseExpressionCore());
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
