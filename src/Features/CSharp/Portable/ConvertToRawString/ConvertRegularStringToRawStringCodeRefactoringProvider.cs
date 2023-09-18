// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString
{
    using static ConvertToRawStringHelpers;
    using static SyntaxFactory;

    internal partial class ConvertRegularStringToRawStringProvider : IConvertStringProvider
    {
        public static readonly IConvertStringProvider Instance = new ConvertRegularStringToRawStringProvider();

        private ConvertRegularStringToRawStringProvider()
        {
        }

        public bool CheckSyntax(ExpressionSyntax expression)
            => expression is LiteralExpressionSyntax(kind: SyntaxKind.StringLiteralExpression);

        public bool CanConvert(
            ParsedDocument document,
            ExpressionSyntax expression,
            SyntaxFormattingOptions formattingOptions,
            out CanConvertParams convertParams,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(CheckSyntax(expression));
            var token = ((LiteralExpressionSyntax)expression).Token;
            return CanConvertStringLiteral(token, out convertParams);
        }

        private static bool CanConvertStringLiteral(SyntaxToken token, out CanConvertParams convertParams)
        {
            Debug.Assert(token.Kind() == SyntaxKind.StringLiteralToken);

            convertParams = default;

            // Can't convert a string literal in a directive to a raw string.
            if (IsInDirective(token.Parent))
                return false;

            if (token.Parent is not LiteralExpressionSyntax)
                return false;

            var characters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);

            if (!ConvertToRawStringHelpers.CanConvert(characters))
                return false;

            // TODO(cyrusn): Should we offer this on empty strings... seems undesirable as you'd end with a gigantic 
            // three line alternative over just ""
            if (characters.IsEmpty)
                return false;

            var canBeSingleLine = CanBeSingleLine(characters);
            var canBeMultiLineWithoutLeadingWhiteSpaces = false;
            if (!canBeSingleLine)
            {
                // Users sometimes write verbatim string literals with a extra starting newline (or indentation) purely
                // for aesthetic reasons.  For example:
                //
                //      var v = @"
                //          SELECT column1, column2, ...
                //          FROM table_name";
                //
                // Converting this directly to a raw string will produce:
                //
                //      var v = """
                //
                //                  SELECT column1, column2, ...
                //                  FROM table_name";
                //          """
                //
                // Check for this and offer instead to generate:
                //
                //      var v = """
                //          SELECT column1, column2, ...
                //          FROM table_name";
                //          """
                //
                // This changes the contents of the literal, but that can be fine for the domain the user is working in.
                // Offer this, but let the user know that this will change runtime semantics.
                canBeMultiLineWithoutLeadingWhiteSpaces = token.IsVerbatimStringLiteral() &&
                    (HasLeadingWhitespace(characters) || HasTrailingWhitespace(characters)) &&
                    CleanupWhitespace(characters).Length > 0;
            }

            // If we have escaped quotes in the string, then this is a good option to bubble up as something to convert
            // to a raw string.  Otherwise, still offer this refactoring, but at low priority as the user may be
            // invoking this on lots of strings that they have no interest in converting.
            var priority = AllEscapesAreQuotes(characters) ? CodeActionPriority.Default : CodeActionPriority.Low;

            convertParams = new CanConvertParams(priority, canBeSingleLine, canBeMultiLineWithoutLeadingWhiteSpaces);
            return true;
        }

        private static bool CanBeSingleLine(VirtualCharSequence characters)
        {
            // Single line raw strings cannot start/end with quote.
            if (characters.First().Rune.Value == '"' ||
                characters.Last().Rune.Value == '"')
            {
                return false;
            }

            // a single line raw string cannot contain a newline.
            if (characters.Any(static ch => IsCSharpNewLine(ch)))
                return false;

            return true;
        }

        public ExpressionSyntax Convert(
            ParsedDocument document,
            ExpressionSyntax expression,
            ConvertToRawKind kind,
            SyntaxFormattingOptions options,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(CheckSyntax(expression));
            var stringExpression = (LiteralExpressionSyntax)expression;
            var newToken = GetReplacementToken(
                document, stringExpression.Token, kind, options, cancellationToken);
            return stringExpression.WithToken(newToken);
        }

        private static SyntaxToken GetReplacementToken(
            ParsedDocument parsedDocument,
            SyntaxToken token,
            ConvertToRawKind kind,
            SyntaxFormattingOptions formattingOptions,
            CancellationToken cancellationToken)
        {
            var characters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            Contract.ThrowIfTrue(characters.IsDefaultOrEmpty);

            if (kind == ConvertToRawKind.SingleLine)
                return ConvertToSingleLineRawString();

            var indentationOptions = new IndentationOptions(formattingOptions);

            var tokenLine = parsedDocument.Text.Lines.GetLineFromPosition(token.SpanStart);
            if (token.SpanStart == tokenLine.Start)
            {
                // Special case.  string token starting at the start of the line.  This is a common pattern used for
                // multi-line strings that don't want any indentation and have the start/end of the string at the same
                // level (like unit tests).
                //
                // In this case, figure out what indentation we're normally like to put this string.  Update *both* the
                // contents *and* the starting quotes of the raw string.
                var indenter = parsedDocument.LanguageServices.GetRequiredService<IIndentationService>();
                var indentationVal = indenter.GetIndentation(parsedDocument, tokenLine.LineNumber, indentationOptions, cancellationToken);

                var indentation = indentationVal.GetIndentationString(parsedDocument.Text, indentationOptions);
                var newToken = ConvertToMultiLineRawIndentedString(indentation);

                newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.Add(Whitespace(indentation)));
                return newToken;
            }
            else
            {
                // otherwise this was a string literal on a line that already contains contents.  Or it's a string
                // literal on its own line, but indented some amount.  Figure out the indentation of the contents from
                // this, but leave the string literal starting at whatever position it's at.
                var indentation = token.GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);
                return ConvertToMultiLineRawIndentedString(indentation);
            }

            SyntaxToken ConvertToSingleLineRawString()
            {
                // Have to make sure we have a delimiter longer than any quote sequence in the string.
                var longestQuoteSequence = GetLongestQuoteSequence(characters);
                var quoteDelimiterCount = Math.Max(3, longestQuoteSequence + 1);

                using var _ = PooledStringBuilder.GetInstance(out var builder);

                builder.Append('"', quoteDelimiterCount);

                foreach (var ch in characters)
                    ch.AppendTo(builder);

                builder.Append('"', quoteDelimiterCount);

                return Token(
                    token.LeadingTrivia,
                    SyntaxKind.SingleLineRawStringLiteralToken,
                    builder.ToString(),
                    characters.CreateString(),
                    token.TrailingTrivia);
            }

            SyntaxToken ConvertToMultiLineRawIndentedString(string indentation)
            {
                // If the user asked to remove whitespace then do so now.
                if (kind == ConvertToRawKind.MultiLineWithoutLeadingWhitespace)
                    characters = CleanupWhitespace(characters);

                // Have to make sure we have a delimiter longer than any quote sequence in the string.
                var longestQuoteSequence = GetLongestQuoteSequence(characters);
                var quoteDelimiterCount = Math.Max(3, longestQuoteSequence + 1);

                using var _ = PooledStringBuilder.GetInstance(out var builder);

                builder.Append('"', quoteDelimiterCount);
                builder.Append(formattingOptions.NewLine);

                var atStartOfLine = true;
                AppendCharacters(builder, characters, indentation, ref atStartOfLine);

                builder.Append(formattingOptions.NewLine);
                builder.Append(indentation);
                builder.Append('"', quoteDelimiterCount);

                return Token(
                    token.LeadingTrivia,
                    SyntaxKind.MultiLineRawStringLiteralToken,
                    builder.ToString(),
                    characters.CreateString(),
                    token.TrailingTrivia);
            }
        }

        private static VirtualCharSequence CleanupWhitespace(VirtualCharSequence characters)
        {
            using var _ = ArrayBuilder<VirtualCharSequence>.GetInstance(out var lines);

            // First, determine all the lines in the content.
            BreakIntoLines(characters, lines);

            // Remove the leading and trailing line if they are all whitespace.
            while (lines.Count > 0 && AllWhitespace(lines.First()))
                lines.RemoveAt(0);

            while (lines.Count > 0 && AllWhitespace(lines.Last()))
                lines.RemoveAt(lines.Count - 1);

            if (lines.Count == 0)
                return VirtualCharSequence.Empty;

            // Use the remaining lines to figure out what common whitespace we have.
            var commonWhitespacePrefix = ComputeCommonWhitespacePrefix(lines);

            var result = ImmutableSegmentedList.CreateBuilder<VirtualChar>();

            foreach (var line in lines)
            {
                if (AllWhitespace(line))
                {
                    // For an all-whitespace line, just add the trailing newlines on the line (if present).
                    AddRange(result, line.SkipWhile(IsCSharpWhitespace));
                }
                else
                {
                    // Normal line.  Skip the common whitespace.
                    AddRange(result, line.Skip(commonWhitespacePrefix));
                }
            }

            // Remove all trailing whitespace and newlines from the final string.
            while (result.Count > 0 && (IsCSharpNewLine(result[^1]) || IsCSharpWhitespace(result[^1])))
                result.RemoveAt(result.Count - 1);

            return VirtualCharSequence.Create(result.ToImmutable());
        }

        private static void BreakIntoLines(VirtualCharSequence characters, ArrayBuilder<VirtualCharSequence> lines)
        {
            var index = 0;

            while (index < characters.Length)
                lines.Add(GetNextLine(characters, ref index));
        }

        private static VirtualCharSequence GetNextLine(
            VirtualCharSequence characters,
            ref int index)
        {
            var end = index;
            while (end < characters.Length && !IsCSharpNewLine(characters[end]))
                end++;

            if (end != characters.Length)
                end += IsCarriageReturnNewLine(characters, end) ? 2 : 1;

            var result = characters.GetSubSequence(TextSpan.FromBounds(index, end));
            index = end;
            return result;
        }
    }
}
