// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class LanguageParser
    {
        private LiteralExpressionSyntax ParseRawStringToken()
        {
            var originalToken = this.EatToken();

            var expressionKind = SyntaxFacts.GetLiteralExpression(originalToken.Kind);
            Debug.Assert(expressionKind != SyntaxKind.None);

            // We want to share as much code as possible with raw-interpolated-strings.  Especially the code for dealing
            // with indentation removal and determining the 'value' of the string.  As such, we will reinterpret this
            // raw string as an interpolated string with no $'s and no holes, and then extract out the content token
            // from that.

            var originalText = originalToken.Text;
            Debug.Assert(originalText is ['"', '"', '"', ..]);

            var interpolatedString = ParseInterpolatedOrRawStringToken(
                originalToken, originalText, originalText.AsSpan(), isInterpolatedString: false);

            // Because there are no actual interpolations, we expect to only see a single text content node containing
            // the interpreted value of the raw string.
            Debug.Assert(interpolatedString.StringStartToken.Kind is SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken);
            Debug.Assert(interpolatedString.Contents is [InterpolatedStringTextSyntax]);

            var interpolatedText = (InterpolatedStringTextSyntax)interpolatedString.Contents[0]!;
            var textToken = interpolatedText.TextToken;

            // Based on how ParseInterpolatedOrRawStringToken works we should never get a diagnostic on the actual
            // interpolated string text token (since we create it, and immediately add it to the InterpolatedStringText
            // node).
            Debug.Assert(!textToken.ContainsDiagnostics);

            var diagnosticsBuilder = ArrayBuilder<DiagnosticInfo>.GetInstance();
            // Move any diagnostics on the original token to the new token.
            // diagnosticsBuilder.AddRange(token.GetDiagnostics());
            // And any diagnostics from the interpolated string as a whole.
            diagnosticsBuilder.AddRange(interpolatedString.GetDiagnostics());
            // If there are any diagnostics on the interpolated text node, move those over too.  However, move them as
            // they are relative to the text token, and now need to be relative to the start of the token as a whole.
            var textTokenDiagnostics = MoveDiagnostics(interpolatedText.GetDiagnostics(), interpolatedString.StringStartToken.Width);
            if (textTokenDiagnostics != null)
                diagnosticsBuilder.AddRange(textTokenDiagnostics);

            // if the original token had diagnostics, then we absolutely must have produced some diagnostics creating
            // the interpolated version.  Note: the converse does not hold.  Producing the interpolation may produce
            // indentation diagnostics, which are not something the lexer would have produced.
            if (originalToken.ContainsDiagnostics)
                Debug.Assert(diagnosticsBuilder.Count > 0);

            // We preserve everything from the original raw token.  Except we use the computed value text from the
            // interpolated text token instead as long as we got no diagnostics for this raw string.
            var finalToken = SyntaxFactory
                .Literal(originalToken.GetLeadingTrivia(), originalToken.Text, originalToken.Kind, getTokenValue(), originalToken.GetTrailingTrivia())
                .WithDiagnosticsGreen(diagnosticsBuilder.ToArrayAndFree());

            return _syntaxFactory.LiteralExpression(expressionKind, finalToken);

            string getTokenValue()
            {
                if (diagnosticsBuilder.Count == 0)
                    return textToken.GetValueText();

                // Preserve what the lexer used to do here.  In the presence of any diagnostics, the text of the raw
                // string minus the starting quotes is used as the value.
                var startIndex = 0;
                while (startIndex < originalText.Length && originalText[startIndex] is '"')
                    startIndex++;

                return originalText[startIndex..];
            }
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

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.InterpolatedStringToken);
            var originalToken = this.EatToken();

            var originalText = originalToken.ValueText; // this is actually the source text
            Debug.Assert(originalText[0] == '$' || originalText[0] == '@');

            return ParseInterpolatedOrRawStringToken(
                originalToken, originalText, originalText.AsSpan(), isInterpolatedString: true);
        }

        private InterpolatedStringExpressionSyntax ParseInterpolatedOrRawStringToken(
            SyntaxToken originalToken,
            string originalText,
            ReadOnlySpan<char> originalTextSpan,
            bool isInterpolatedString)
        {
            // compute the positions of the interpolations in the original string literal, if there was an error or not,
            // and where the open and close quotes can be found.
            var interpolations = ArrayBuilder<Lexer.Interpolation>.GetInstance();

            rescanInterpolation(out var kind, out var error, out var openQuoteRange, interpolations, out var closeQuoteRange);

            // Only bother trying to do dedentation if we have a multiline literal without errors.  There's no point
            // trying in the presence of errors as we may not even be able to determine what the dedentation should be.
            var needsDedentation = kind == Lexer.InterpolatedStringKind.MultiLineRaw && error == null;

            var result = SyntaxFactory.InterpolatedStringExpression(getOpenQuote(), getContent(originalTextSpan), getCloseQuote());

            interpolations.Free();
            if (error != null)
                result = result.WithDiagnosticsGreen([error]);

            Debug.Assert(originalToken.ToFullString() == result.ToFullString()); // yield from text equals yield from node
            return result;

            void rescanInterpolation(out Lexer.InterpolatedStringKind kind, out SyntaxDiagnosticInfo? error, out Range openQuoteRange, ArrayBuilder<Lexer.Interpolation> interpolations, out Range closeQuoteRange)
            {
                using var tempLexer = new Lexer(SourceText.From(originalText), this.Options, allowPreprocessorDirectives: false);
                var info = default(Lexer.TokenInfo);
                tempLexer.ScanInterpolatedOrRawStringLiteralTop(
                    ref info, isInterpolatedString, out error, out kind, out openQuoteRange, interpolations, out closeQuoteRange);

                Debug.Assert(isInterpolatedString || interpolations.Count == 0, "Non-interpolated parsing should never produce interpolations");
            }

            SyntaxToken getOpenQuote()
            {
                return SyntaxFactory.Token(
                    originalToken.GetLeadingTrivia(),
                    kind switch
                    {
                        Lexer.InterpolatedStringKind.Normal => SyntaxKind.InterpolatedStringStartToken,
                        Lexer.InterpolatedStringKind.Verbatim => SyntaxKind.InterpolatedVerbatimStringStartToken,
                        Lexer.InterpolatedStringKind.SingleLineRaw => SyntaxKind.InterpolatedSingleLineRawStringStartToken,
                        Lexer.InterpolatedStringKind.MultiLineRaw => SyntaxKind.InterpolatedMultiLineRawStringStartToken,
                        _ => throw ExceptionUtilities.UnexpectedValue(kind),
                    },
                    originalText[openQuoteRange],
                    trailing: null);
            }

            CodeAnalysis.Syntax.InternalSyntax.SyntaxList<InterpolatedStringContentSyntax> getContent(ReadOnlySpan<char> originalTextSpan)
            {
                var content = PooledStringBuilder.GetInstance();
                var builder = _pool.Allocate<InterpolatedStringContentSyntax>();

                var indentationWhitespace = needsDedentation ? getIndentationWhitespace(originalTextSpan) : default;

                var currentContentStart = openQuoteRange.End;
                for (var i = 0; i < interpolations.Count; i++)
                {
                    var interpolation = interpolations[i];

                    // Add a token for text preceding the interpolation
                    builder.Add(makeContent(
                        indentationWhitespace, content, isFirst: i == 0, isLast: false,
                        originalTextSpan[currentContentStart..interpolation.OpenBraceRange.Start]));

                    // Now parse the interpolation itself.
                    var interpolationNode = ParseInterpolation(this.Options, originalText, interpolation, kind, IsInFieldKeywordContext);

                    // Make sure the interpolation starts at the right location.
                    var indentationError = getInterpolationIndentationError(indentationWhitespace, interpolation);
                    if (indentationError != null)
                        interpolationNode = interpolationNode.WithDiagnosticsGreen(new[] { indentationError });

                    builder.Add(interpolationNode);
                    currentContentStart = interpolation.CloseBraceRange.End;
                }

                // Add a token for text following the last interpolation
                builder.Add(makeContent(
                    indentationWhitespace, content, isFirst: interpolations.Count == 0, isLast: true,
                    originalTextSpan[currentContentStart..closeQuoteRange.Start]));

                CodeAnalysis.Syntax.InternalSyntax.SyntaxList<InterpolatedStringContentSyntax> result = builder;
                _pool.Free(builder);
                content.Free();
                return result;
            }

            // Gets the indentation whitespace from the last line of a multi-line raw literal.
            ReadOnlySpan<char> getIndentationWhitespace(ReadOnlySpan<char> originalTextSpan)
            {
                // The content we want to create text token out of.  Effectively, what is in the text sections
                // minus leading whitespace.
                var closeQuoteText = originalTextSpan[closeQuoteRange];

                // A multi-line raw interpolation without errors always ends with a new-line, some number of spaces, and
                // the quotes. So it's safe to just pull off the first two characters here to find where the
                // newline-ends.
                var afterNewLine = SlidingTextWindow.GetNewLineWidth(closeQuoteText[0], closeQuoteText[1]);
                var afterWhitespace = SkipWhitespace(closeQuoteText, afterNewLine);

                Debug.Assert(closeQuoteText[afterWhitespace] == '"');
                return closeQuoteText[afterNewLine..afterWhitespace];
            }

            InterpolatedStringContentSyntax? makeContent(
                ReadOnlySpan<char> indentationWhitespace, StringBuilder content, bool isFirst, bool isLast, ReadOnlySpan<char> text)
            {
                if (text.IsEmpty)
                {
                    // For the raw string case, always include an InterpolatedStringText token, even if empty. This
                    // allows the caller to uniformly assume there is always at least one text token that it can 
                    // extract data from.
                    return isInterpolatedString
                        ? null
                        : SyntaxFactory.InterpolatedStringText(
                            SyntaxFactory.Literal(leading: null, "", SyntaxKind.InterpolatedStringTextToken, "", trailing: null));

                }

                // If we're not dedenting then just make a standard interpolated text token.  Also, we can short-circuit
                // if the indentation whitespace is empty (nothing to dedent in that case).
                if (!needsDedentation || indentationWhitespace.IsEmpty)
                    return SyntaxFactory.InterpolatedStringText(MakeInterpolatedStringTextToken(kind, text.ToString()));

                content.Clear();
                var currentIndex = 0;

                // If we're not processing the first content chunk, then we must be processing a chunk that came after
                // an interpolation.  In that case, we need to consume up through the next newline of that chunk as
                // content that is not subject to dedentation.
                if (!isFirst)
                    currentIndex = ConsumeRemainingContentThroughNewLine(content, text, currentIndex);

                // We're either the first item, or we consumed up through a newline from the previous line. We're
                // definitely at the start of a new line (or at the end).  Regardless, we want to consume each
                // successive line, making sure its indentation is correct.

                // Consume one line at a time.
                SyntaxDiagnosticInfo? indentationError = null;
                while (currentIndex < text.Length)
                {
                    var lineStartPosition = currentIndex;

                    // Only bother reporting a single indentation error on a text chunk.
                    if (indentationError == null)
                    {
                        currentIndex = SkipWhitespace(text, currentIndex);
                        var currentLineWhitespace = text[lineStartPosition..currentIndex];

                        if (!currentLineWhitespace.StartsWith(indentationWhitespace))
                        {
                            // We have a line where the indentation of that line isn't a prefix of indentation
                            // whitespace.
                            //
                            // If we're not on a blank line then this is bad.  That's a content line that doesn't start
                            // with the indentation whitespace.  If we are on a blank line then it's ok if the whitespace
                            // we do have is a prefix of the indentation whitespace.
                            var isBlankLine = (currentIndex == text.Length && isLast) || (currentIndex < text.Length && SyntaxFacts.IsNewLine(text[currentIndex]));
                            var isLegalBlankLine = isBlankLine && indentationWhitespace.StartsWith(currentLineWhitespace);
                            if (!isLegalBlankLine)
                            {
                                // Specialized error message if this is a spacing difference.
                                if (CheckForSpaceDifference(
                                        currentLineWhitespace, indentationWhitespace,
                                        out var currentLineWhitespaceChar, out var indentationWhitespaceChar))
                                {
                                    indentationError ??= MakeError(
                                        lineStartPosition,
                                        width: currentIndex - lineStartPosition,
                                        ErrorCode.ERR_LineContainsDifferentWhitespace,
                                        currentLineWhitespaceChar, indentationWhitespaceChar);
                                }
                                else
                                {
                                    indentationError ??= MakeError(
                                        lineStartPosition,
                                        width: currentIndex - lineStartPosition,
                                        ErrorCode.ERR_LineDoesNotStartWithSameWhitespace);
                                }
                            }
                        }
                    }

                    // Skip the leading whitespace that matches the terminator line and add any text after that to our content.
                    currentIndex = Math.Min(currentIndex, lineStartPosition + indentationWhitespace.Length);
                    currentIndex = ConsumeRemainingContentThroughNewLine(content, text, currentIndex);
                }

                // if we ran into any errors, don't give this item any special value.  It just has the value of our actual text.
                var textString = text.ToString();
                var valueString = indentationError != null ? textString : content.ToString();

                var node = SyntaxFactory.InterpolatedStringText(
                    SyntaxFactory.Literal(leading: null, textString, SyntaxKind.InterpolatedStringTextToken, valueString, trailing: null));

                return indentationError != null
                    ? node.WithDiagnosticsGreen(new[] { indentationError })
                    : node;
            }

            SyntaxToken getCloseQuote()
            {
                // Make a token for the close quote " (even if it was missing)
                return TokenOrMissingToken(
                    leading: null,
                    kind switch
                    {
                        Lexer.InterpolatedStringKind.Normal => SyntaxKind.InterpolatedStringEndToken,
                        Lexer.InterpolatedStringKind.Verbatim => SyntaxKind.InterpolatedStringEndToken,
                        Lexer.InterpolatedStringKind.SingleLineRaw => SyntaxKind.InterpolatedRawStringEndToken,
                        Lexer.InterpolatedStringKind.MultiLineRaw => SyntaxKind.InterpolatedRawStringEndToken,
                        _ => throw ExceptionUtilities.UnexpectedValue(kind),
                    },
                    originalText[closeQuoteRange],
                    originalToken.GetTrailingTrivia());
            }

            // if the interpolation starts on its own line, then it has to have correct indentation whitespace
            // before it.  e.g.:
            //
            //      var x = """
            //          {1 + 1}
            //          """
            //
            // Not:
            //
            //      var x = """
            // {1 + 1}
            //          """
            //
            // Note: We don't need to check
            //
            //      var x = """
            // <space>{1 + 1}
            //          """
            //
            // as initial whitespace in text will already be checked in makeContent.  This is only for the case where
            // the interpolation is at the start of a line.

            SyntaxDiagnosticInfo? getInterpolationIndentationError(
                ReadOnlySpan<char> indentationWhitespace,
                Lexer.Interpolation interpolation)
            {
                if (needsDedentation && !indentationWhitespace.IsEmpty)
                {
                    var openBracePosition = interpolation.OpenBraceRange.Start.Value;
                    if (openBracePosition > 0 && SyntaxFacts.IsNewLine(originalText[openBracePosition - 1]))
                        // Pass 0 as the offset to give the error on the interpolation brace.
                        return MakeError(offset: 0, width: 1, ErrorCode.ERR_LineDoesNotStartWithSameWhitespace);
                }

                return null;
            }
        }

        /// <summary>
        /// Converts a whitespace character to its string representation for error messages.
        /// </summary>
        private static string CharToString(char ch)
        {
            return ch switch
            {
                '\t' => @"\t",
                '\v' => @"\v",
                '\f' => @"\f",
                _ => @$"\u{(int)ch:x4}",
            };
        }

        /// <summary>
        /// Checks if two whitespace sequences differ at a specific character position where both
        /// characters are whitespace but different types (e.g., tab vs space).
        /// </summary>
        private static bool CheckForSpaceDifference(
            ReadOnlySpan<char> currentLineWhitespace,
            ReadOnlySpan<char> indentationLineWhitespace,
            [NotNullWhen(true)] out string? currentLineMessage,
            [NotNullWhen(true)] out string? indentationLineMessage)
        {
            for (int i = 0, n = Math.Min(currentLineWhitespace.Length, indentationLineWhitespace.Length); i < n; i++)
            {
                var currentLineChar = currentLineWhitespace[i];
                var indentationLineChar = indentationLineWhitespace[i];

                if (currentLineChar != indentationLineChar &&
                    SyntaxFacts.IsWhitespace(currentLineChar) &&
                    SyntaxFacts.IsWhitespace(indentationLineChar))
                {
                    currentLineMessage = CharToString(currentLineChar);
                    indentationLineMessage = CharToString(indentationLineChar);
                    return true;
                }
            }

            currentLineMessage = null;
            indentationLineMessage = null;
            return false;
        }

        private static SyntaxToken TokenOrMissingToken(GreenNode? leading, SyntaxKind kind, string text, GreenNode? trailing)
            => text == ""
                ? SyntaxFactory.MissingToken(leading, kind, trailing)
                : SyntaxFactory.Token(leading, kind, text, trailing);

        private static int SkipWhitespace(ReadOnlySpan<char> text, int currentIndex)
        {
            while (currentIndex < text.Length && SyntaxFacts.IsWhitespace(text[currentIndex]))
                currentIndex++;
            return currentIndex;
        }

        private static int ConsumeRemainingContentThroughNewLine(StringBuilder content, ReadOnlySpan<char> text, int currentIndex)
        {
            var start = currentIndex;
            while (currentIndex < text.Length)
            {
                var ch = text[currentIndex];
                if (!SyntaxFacts.IsNewLine(ch))
                {
                    currentIndex++;
                    continue;
                }

                currentIndex += SlidingTextWindow.GetNewLineWidth(ch, currentIndex + 1 < text.Length ? text[currentIndex + 1] : '\0');
                break;
            }

            var slice = text[start..currentIndex];
#if NET
            content.Append(slice);
#else
            unsafe
            {
                fixed (char* pointer = slice)
                    content.Append(pointer, slice.Length);
            }
#endif
            return currentIndex;
        }

        private static InterpolationSyntax ParseInterpolation(
            CSharpParseOptions options,
            string text,
            Lexer.Interpolation interpolation,
            Lexer.InterpolatedStringKind kind,
            bool isInFieldKeywordContext)
        {
            // Grab the text from after the { all the way to the start of the } (or the start of the : if present). This
            // will be used to parse out the expression of the interpolation.
            //
            // The parsing of the open brace, close brace and colon is specially handled in ParseInterpolation below.
            var followingRange = interpolation.HasColon ? interpolation.ColonRange : interpolation.CloseBraceRange;
            var expressionText = text[interpolation.OpenBraceRange.End..followingRange.Start];

            using var tempLexer = new Lexer(SourceText.From(expressionText), options, allowPreprocessorDirectives: false, interpolationFollowedByColon: interpolation.HasColon);

            // First grab any trivia right after the {, it will be trailing trivia for the { token.
            var openTokenTrailingTrivia = tempLexer.LexSyntaxTrailingTrivia().Node;

            // Now create a parser to actually handle the expression portion of the interpolation
            using var tempParser = new LanguageParser(tempLexer, oldTree: null, changes: null);
            using var _ = new FieldKeywordContext(tempParser, isInFieldKeywordContext);

            var result = tempParser.ParseInterpolation(
                text, interpolation, kind,
                SyntaxFactory.Token(leading: null, SyntaxKind.OpenBraceToken, text[interpolation.OpenBraceRange], openTokenTrailingTrivia));

            Debug.Assert(text[interpolation.OpenBraceRange.Start..interpolation.CloseBraceRange.End] == result.ToFullString()); // yield from text equals yield from node
            return result;
        }

        private InterpolationSyntax ParseInterpolation(
            string text,
            Lexer.Interpolation interpolation,
            Lexer.InterpolatedStringKind kind,
            SyntaxToken openBraceToken)
        {
            var (expression, alignment) = getExpressionAndAlignment();
            var (format, closeBraceToken) = getFormatAndCloseBrace();

            var result = SyntaxFactory.Interpolation(openBraceToken, expression, alignment, format, closeBraceToken);
#if DEBUG
            Debug.Assert(text[interpolation.OpenBraceRange.Start..interpolation.CloseBraceRange.End] == result.ToFullString()); // yield from text equals yield from node
#endif
            return result;

            (ExpressionSyntax expression, InterpolationAlignmentClauseSyntax? alignment) getExpressionAndAlignment()
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

            (InterpolationFormatClauseSyntax? format, SyntaxToken closeBraceToken) getFormatAndCloseBrace()
            {
                var leading = this.CurrentToken.GetLeadingTrivia();
                if (interpolation.HasColon)
                {
                    var format = SyntaxFactory.InterpolationFormatClause(
                        SyntaxFactory.Token(leading, SyntaxKind.ColonToken, text[interpolation.ColonRange], trailing: null),
                        MakeInterpolatedStringTextToken(kind, text[interpolation.ColonRange.End..interpolation.CloseBraceRange.Start]));
                    return (format, getInterpolationCloseToken(leading: null));
                }
                else
                {
                    return (format: null, getInterpolationCloseToken(leading));
                }
            }

            SyntaxToken getInterpolationCloseToken(GreenNode? leading)
            {
                return TokenOrMissingToken(
                    leading,
                    SyntaxKind.CloseBraceToken,
                    text[interpolation.CloseBraceRange],
                    trailing: null);
            }
        }

        /// <summary>
        /// Interpret the given raw text from source as an InterpolatedStringTextToken.
        /// </summary>
        /// <param name="text">The text for the full string literal, including the quotes and contents</param>
        /// <param name="kind">The kind of the interpolated string we were processing</param>
        private SyntaxToken MakeInterpolatedStringTextToken(Lexer.InterpolatedStringKind kind, string text)
        {
            // with a raw string, we don't do any interpretation of the content.  Note: removal of indentation is
            // handled already in splitContent
            if (kind is Lexer.InterpolatedStringKind.SingleLineRaw or Lexer.InterpolatedStringKind.MultiLineRaw)
                return SyntaxFactory.Literal(leading: null, text, SyntaxKind.InterpolatedStringTextToken, text, trailing: null);

            Debug.Assert(kind is Lexer.InterpolatedStringKind.Normal or Lexer.InterpolatedStringKind.Verbatim);

            // For a normal/verbatim piece of content, process the inner content as if it was in a corresponding
            // *non*-interpolated string to get the correct meaning of all the escapes/diagnostics within.
            var prefix = kind is Lexer.InterpolatedStringKind.Verbatim ? "@\"" : "\"";
            var fakeString = prefix + text + "\"";
            using var tempLexer = new Lexer(SourceText.From(fakeString), this.Options, allowPreprocessorDirectives: false);
            var mode = LexerMode.Syntax;
            var token = tempLexer.Lex(ref mode);
            Debug.Assert(token.Kind == SyntaxKind.StringLiteralToken);
            var result = SyntaxFactory.Literal(leading: null, text, SyntaxKind.InterpolatedStringTextToken, token.ValueText, trailing: null);
            if (token.ContainsDiagnostics)
                result = result.WithDiagnosticsGreen(MoveDiagnostics(token.GetDiagnostics(), -prefix.Length));

            return result;
        }

        private static DiagnosticInfo[]? MoveDiagnostics(DiagnosticInfo[]? infos, int offset)
        {
            if (infos is null || infos.Length == 0)
                return null;

            var builder = ArrayBuilder<DiagnosticInfo>.GetInstance(infos.Length);
            foreach (var info in infos)
            {
                // This cast should always be safe.  We are only moving diagnostics produced on syntax nodes and tokens.
                var sd = (SyntaxDiagnosticInfo)info;
                builder.Add(sd.WithOffset(sd.Offset + offset));
            }

            return builder.ToArrayAndFree();
        }
    }
}
