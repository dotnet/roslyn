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
            Debug.Assert(originalToken.Kind == SyntaxKind.InterpolatedStringToken);
            var originalText = originalToken.ValueText;
            var interpolations = ArrayBuilder<Lexer.Interpolation>.GetInstance();
            SyntaxDiagnosticInfo error = null;
            using (var tempLexer = new Lexer(Text.SourceText.From(originalText), this.Options))
            {
                // compute the positions of the interpolations in the original string literal
                var info = default(Lexer.TokenInfo);
                tempLexer.ScanISLTop(interpolations, ref info, ref error);
            }

            Debug.Assert(interpolations.Count != 0);
            var builder = this.pool.AllocateSeparated<InterpolatedStringInsertSyntax>();
            try
            {
                SyntaxToken stringStart = null;
                SyntaxToken stringEnd = null;

                for (int i = 0; i < interpolations.Count; i++)
                {
                    var interpolation = interpolations[i];
                    var first = i == 0;
                    var last = i == (interpolations.Count - 1);

                    if (first)
                    {
                        // compute stringStart
                        var startText = originalText.Substring(0, interpolation.Start + 1);
                        stringStart = MakeStringToken(startText, SyntaxKind.InterpolatedStringStartToken).WithLeadingTrivia(originalToken.GetLeadingTrivia());
                        Debug.Assert(stringStart.Kind == SyntaxKind.InterpolatedStringStartToken);
                    }

                    CSharpSyntaxNode additionalTrivia;
                    var interpText = originalText.Substring(interpolations[0].Start + 1, interpolations[0].End - interpolations[0].Start - 1);
                    using (var tempLexer = new Lexer(Text.SourceText.From(interpText), this.Options))
                    {
                        using (var tempParser = new LanguageParser(tempLexer, null, null))
                        {
                            var insert = tempParser.ParseInterpolatedStringInsert();
                            insert = tempParser.ConsumeUnexpectedTokens(insert);
                            // In case the insert is empty, move the leading trivia from its EOF token to the following literal part.
                            additionalTrivia = tempParser.CurrentToken.GetLeadingTrivia();
                            builder.Add(insert);
                        }
                    }

                    if (last)
                    {
                        // compute stringEnd
                        var endText = originalText.Substring(interpolation.End);
                        stringEnd = MakeStringToken(endText, SyntaxKind.InterpolatedStringEndToken).WithLeadingTrivia(additionalTrivia).WithTrailingTrivia(originalToken.GetTrailingTrivia());
                        Debug.Assert(stringEnd.Kind == SyntaxKind.InterpolatedStringEndToken);
                    } else {
                        // add an interpolated string mid token for the following }...{ part
                        var midText = originalText.Substring(interpolation.End, interpolations[i + 1].Start - interpolation.End + 1);
                        var stringMid = MakeStringToken(midText, SyntaxKind.InterpolatedStringMidToken).WithLeadingTrivia(additionalTrivia);
                        Debug.Assert(stringMid.Kind == SyntaxKind.InterpolatedStringMidToken);
                        builder.AddSeparator(stringMid);
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
        /// <param name="kind">The token kind to be assigned to the resulting token</param>
        SyntaxToken MakeStringToken(string text, SyntaxKind kind)
        {
            int length = text.Length;
            int startingChars = (length > 2 && text[0] == '"' || length > 1 && text[0] == '}') ? 1 : 0;
            int endingChars = (length > 1 && text[length - 1] == '"') ? 1 : (length > 2 && text[length - 1] == '{' && text[length - 2] == '\\') ? 2 : 0;
            using (var tempLexer = new Lexer(Text.SourceText.From("\"" + text.Substring(startingChars, length - startingChars - endingChars) + "\""), this.Options))
            {
                var info = default(Lexer.TokenInfo);
                tempLexer.ScanStringLiteral(ref info);
                Debug.Assert(info.Kind == SyntaxKind.StringLiteralToken);
                return SyntaxFactory.Literal(null, text, kind, info.StringValue, null);
            }
        }

        private ExpressionSyntax ParseInterpolatedString()
        {
            // The following *should* be dead code, as we always construct the tokens and parse them at the same time, above.
            throw new NotImplementedException();

            ////var stringStart = this.EatToken();
            ////Debug.Assert(stringStart.Kind == SyntaxKind.InterpolatedStringStart);
            ////SyntaxToken stringEnd = default(SyntaxToken);
            ////var builder = this.pool.AllocateSeparated<InterpolatedStringInsertSyntax>();
            ////try
            ////{
            ////    while (true)
            ////    {
            ////        var insert = ParseInterpolatedStringInsert();
            ////        builder.Add(insert);
            ////        if (this.CurrentToken.Kind == SyntaxKind.InterpolatedStringMid)
            ////        {
            ////            builder.AddSeparator(this.EatToken());
            ////        }
            ////        else if (this.CurrentToken.Kind == SyntaxKind.InterpolatedStringEnd)
            ////        {
            ////            stringEnd = this.EatToken();
            ////            break;
            ////        }
            ////        else
            ////        {
            ////            throw new NotImplementedException();
            ////        }
            ////    }

            ////    return SyntaxFactory.InterpolatedString(stringStart, builder.ToList(), stringEnd);
            ////}
            ////finally
            ////{
            ////    this.pool.Free(builder);
            ////}
        }

        private InterpolatedStringInsertSyntax ParseInterpolatedStringInsert()
        {
            ExpressionSyntax expr;
            var commaToken = default(SyntaxToken);
            ExpressionSyntax alignmentExpression = null;
            var colonToken = default(SyntaxToken);
            var formatToken = default(SyntaxToken);

            expr = this.ParseExpression();

            if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                commaToken = this.EatToken(SyntaxKind.CommaToken);
                // require the width to be a literal (one token) of the negative of a literal (two tokens)
                var minusToken = (this.CurrentToken.Kind == SyntaxKind.MinusToken) ? this.EatToken(SyntaxKind.MinusToken) : null;
                var widthToken = this.EatToken(SyntaxKind.NumericLiteralToken);
                var widthExpression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, widthToken);
                alignmentExpression = (minusToken != null) ? (ExpressionSyntax)SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, minusToken, widthExpression) : widthExpression;
            }

            if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                colonToken = this.EatToken(SyntaxKind.ColonToken);
                formatToken = this.EatToken(this.CurrentToken.Kind == SyntaxKind.IdentifierToken ? SyntaxKind.IdentifierToken : SyntaxKind.StringLiteralToken);
            }

            return SyntaxFactory.InterpolatedStringInsert(
                expr,                // expression
                commaToken,          // ,
                alignmentExpression, // -5
                colonToken,          // :
                formatToken);        // N2

        }

    }
}
