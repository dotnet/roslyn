// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class LanguageParser
    {
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

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.InterpolatedStringToken);
            var originalToken = this.EatToken();

            var originalText = originalToken.ValueText; // this is actually the source text
            Debug.Assert(originalText[0] == '$' || originalText[0] == '@');

            var isVerbatim = (originalText[0] == '$' && originalText[1] == '@') ||
                             (originalText[0] == '@' && originalText[1] == '$');

            // compute the positions of the interpolations in the original string literal, if there was an error or not,
            // and where the close quote can be found.
            var interpolations = ArrayBuilder<Lexer.Interpolation>.GetInstance();

            rescanInterpolation(out var openQuoteRange, out var error, out var closeQuoteRange);

            var result = SyntaxFactory.InterpolatedStringExpression(
                getOpenQuote(openQuoteRange), getContent(interpolations), getCloseQuote(closeQuoteRange));

            interpolations.Free();
            if (error != null)
            {
                result = result.WithDiagnosticsGreen(new[] { error });
            }

            Debug.Assert(originalToken.ToFullString() == result.ToFullString()); // yield from text equals yield from node
            return CheckFeatureAvailability(result, MessageID.IDS_FeatureInterpolatedStrings);

            void rescanInterpolation(out Range openQuoteRange, out SyntaxDiagnosticInfo error, out Range closeQuoteRange)
            {
                using var tempLexer = new Lexer(SourceText.From(originalText), this.Options, allowPreprocessorDirectives: false);
                var info = default(Lexer.TokenInfo);
                tempLexer.ScanInterpolatedStringLiteralTop(ref info, out error, out openQuoteRange, interpolations, out closeQuoteRange);
            }

            SyntaxToken getOpenQuote(Range openQuoteRange)
            {
                var openQuoteText = originalText[openQuoteRange];
                return SyntaxFactory.Token(
                    originalToken.GetLeadingTrivia(),
                    isVerbatim ? SyntaxKind.InterpolatedVerbatimStringStartToken : SyntaxKind.InterpolatedStringStartToken,
                    openQuoteText, openQuoteText, trailing: null);
            }

            CodeAnalysis.Syntax.InternalSyntax.SyntaxList<InterpolatedStringContentSyntax> getContent(ArrayBuilder<Lexer.Interpolation> interpolations)
            {
                var builder = _pool.Allocate<InterpolatedStringContentSyntax>();

                if (interpolations.Count == 0)
                {
                    // In the special case when there are no interpolations, we just construct a format string
                    // with no inserts. We must still use String.Format to get its handling of escapes such as {{,
                    // so we still treat it as a composite format string.
                    var text = originalText[new Range(openQuoteRange.End, closeQuoteRange.Start)];
                    if (text.Length > 0)
                    {
                        builder.Add(SyntaxFactory.InterpolatedStringText(MakeStringToken(text, text, isVerbatim, SyntaxKind.InterpolatedStringTextToken)));
                    }
                }
                else
                {
                    for (int i = 0; i < interpolations.Count; i++)
                    {
                        var interpolation = interpolations[i];

                        // Add a token for text preceding the interpolation
                        var text = originalText[new Range(
                            i == 0 ? openQuoteRange.End : interpolations[i - 1].CloseBraceRange.End,
                            interpolation.OpenBraceRange.Start)];
                        if (text.Length > 0)
                        {
                            builder.Add(SyntaxFactory.InterpolatedStringText(MakeStringToken(text, text, isVerbatim, SyntaxKind.InterpolatedStringTextToken)));
                        }

                        builder.Add(ParseInterpolation(this.Options, originalText, interpolation, isVerbatim));
                    }

                    // Add a token for text following the last interpolation
                    var lastText = originalText[new Range(interpolations[^1].CloseBraceRange.End, closeQuoteRange.Start)];
                    if (lastText.Length > 0)
                    {
                        var token = MakeStringToken(lastText, lastText, isVerbatim, SyntaxKind.InterpolatedStringTextToken);
                        builder.Add(SyntaxFactory.InterpolatedStringText(token));
                    }
                }

                CodeAnalysis.Syntax.InternalSyntax.SyntaxList<InterpolatedStringContentSyntax> result = builder;
                _pool.Free(builder);
                return result;
            }

            SyntaxToken getCloseQuote(Range openQuoteRange)
            {
                // Make a token for the close quote " (even if it was missing)
                var closeQuoteText = originalText[closeQuoteRange];
                return closeQuoteText == ""
                    ? SyntaxFactory.MissingToken(SyntaxKind.InterpolatedStringEndToken).TokenWithTrailingTrivia(originalToken.GetTrailingTrivia())
                    : SyntaxFactory.Token(null, SyntaxKind.InterpolatedStringEndToken, closeQuoteText, closeQuoteText, originalToken.GetTrailingTrivia());
            }
        }

        private static InterpolationSyntax ParseInterpolation(CSharpParseOptions options, string text, Lexer.Interpolation interpolation, bool isVerbatim)
        {
            // Grab from before the { all the way to the start of the } (or the start of the : if present).  The parsing
            // of the colon and/or close curly is specially handled in ParseInterpolation below.
            var parsedText = text[new Range(
                interpolation.OpenBraceRange.Start,
                interpolation.HasColon ? interpolation.ColonRange.Start : interpolation.CloseBraceRange.Start)];

            // TODO: some of the trivia in the interpolation maybe should be trailing trivia of the openBraceToken
            using var tempLexer = new Lexer(SourceText.From(parsedText), options, allowPreprocessorDirectives: false, interpolationFollowedByColon: interpolation.HasColon);
            using var tempParser = new LanguageParser(tempLexer, oldTree: null, changes: null);

            var result = tempParser.ParseInterpolation(text, interpolation, isVerbatim);

            Debug.Assert(text[new Range(interpolation.OpenBraceRange.Start, interpolation.CloseBraceRange.End)] == result.ToFullString()); // yield from text equals yield from node
            return result;
        }

        private InterpolationSyntax ParseInterpolation(string text, Lexer.Interpolation interpolation, bool isVerbatim)
        {
            var openBraceToken = this.EatToken(SyntaxKind.OpenBraceToken);
            var (expression, alignment) = getExpressionAndAlignment();
            var (format, closeBraceToken) = getFormatAndCloseBrace();

            return SyntaxFactory.Interpolation(openBraceToken, expression, alignment, format, closeBraceToken);

            (ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignment) getExpressionAndAlignment()
            {
                var expression = this.ParseExpressionCore();

                if (this.CurrentToken.Kind != SyntaxKind.CommaToken)
                {
                    return (this.ConsumeUnexpectedTokens(expression), alignment: null);
                }

                var alignment = SyntaxFactory.InterpolationAlignmentClause(
                    this.EatToken(SyntaxKind.CommaToken),
                    this.ConsumeUnexpectedTokens(this.ParseExpressionCore()));
                return (expression, alignment);
            }

            (InterpolationFormatClauseSyntax format, SyntaxToken closeBraceToken) getFormatAndCloseBrace()
            {
                var leading = this.CurrentToken.GetLeadingTrivia();
                if (interpolation.HasColon)
                {
                    var colonText = text[interpolation.ColonRange];
                    var colonToken = SyntaxFactory.Token(leading, SyntaxKind.ColonToken, colonText, colonText, trailing: null);
                    var formatText = text[new Range(interpolation.ColonRange.End, interpolation.CloseBraceRange.Start)];
                    var formatString = MakeStringToken(formatText, formatText, isVerbatim, SyntaxKind.InterpolatedStringTextToken);
                    var format = SyntaxFactory.InterpolationFormatClause(colonToken, formatString);
                    var closeBraceToken = getInterpolationCloseBraceToken(leading: null);
                    return (format, closeBraceToken);
                }
                else
                {
                    var closeBraceToken = getInterpolationCloseBraceToken(leading);
                    return (format: null, closeBraceToken);
                }
            }

            SyntaxToken getInterpolationCloseBraceToken(GreenNode leading)
            {
                var tokenText = text[interpolation.CloseBraceRange];
                if (tokenText == "")
                    return SyntaxFactory.MissingToken(leading, SyntaxKind.CloseBraceToken, trailing: null);

                return SyntaxFactory.Token(leading, SyntaxKind.CloseBraceToken, tokenText, tokenText, trailing: null);
            }
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
    }
}
