// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString
{
    using static ConvertToRawStringHelpers;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRawString), Shared]
    internal class ConvertRegularStringToRawStringCodeRefactoringProvider : CodeRefactoringProvider
    {
        private enum ConvertToRawKind
        {
            SingleLine,
            MultiLineIndented,
            MultiLineWithoutLeadingWhitespace,
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertRegularStringToRawStringCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);
            if (!context.Span.IntersectsWith(token.Span))
                return;

            if (token.Kind() != SyntaxKind.StringLiteralToken)
                return;

            // Can't convert a string literal in a directive to a raw string.
            if (IsInDirective(token.Parent))
                return;

            var characters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);

            // TODO(cyrusn): Should we offer this on empty strings... seems undesirable as you'd end with a gigantic 
            // three line alternative over just ""
            if (characters.IsDefaultOrEmpty)
                return;

            // Ensure that all characters in the string are those we can convert.
            if (!characters.All(static ch => CanConvert(ch)))
                return;

            // If we have escaped quotes in the string, then this is a good option to bubble up as something to convert
            // to a raw string.  Otherwise, still offer this refactoring, but at low priority as the user may be
            // invoking this on lots of strings that they have no interest in converting.
            var priority = AllEscapesAreQuotes(characters) ? CodeActionPriority.Medium : CodeActionPriority.Low;

            var canBeSingleLine = CanBeSingleLine(characters);

            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(context.Options, cancellationToken).ConfigureAwait(false);

            if (canBeSingleLine)
            {
                context.RegisterRefactoring(
                    CodeAction.CreateWithPriority(
                        priority,
                        CSharpFeaturesResources.Convert_to_raw_string,
                        c => UpdateDocumentAsync(document, span, ConvertToRawKind.SingleLine, formattingOptions, c),
                        nameof(CSharpFeaturesResources.Convert_to_raw_string) + "-" + ConvertToRawKind.SingleLine),
                    token.Span);
            }
            else
            {
                context.RegisterRefactoring(
                    CodeAction.CreateWithPriority(
                        priority,
                        CSharpFeaturesResources.Convert_to_raw_string,
                        c => UpdateDocumentAsync(document, span, ConvertToRawKind.MultiLineIndented, formattingOptions, c),
                        nameof(CSharpFeaturesResources.Convert_to_raw_string)),
                    token.Span);

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
                if (token.IsVerbatimStringLiteral() &&
                    HasLeadingWhitespace(characters) &&
                    CleanupWhitespace(characters).Length > 0)
                {
                    context.RegisterRefactoring(
                        CodeAction.CreateWithPriority(
                            priority,
                            CSharpFeaturesResources.without_leading_whitespace_may_change_semantics,
                            c => UpdateDocumentAsync(document, span, ConvertToRawKind.MultiLineWithoutLeadingWhitespace, formattingOptions, c),
                            nameof(CSharpFeaturesResources.without_leading_whitespace_may_change_semantics)),
                        token.Span);
                }
            }
        }

        private static bool HasLeadingWhitespace(VirtualCharSequence characters)
        {
            var index = 0;
            while (index < characters.Length && IsCSharpWhitespace(characters[index]))
                index++;

            return index < characters.Length && IsCSharpNewLine(characters[index]);
        }

        private static async Task<Document> UpdateDocumentAsync(
            Document document, TextSpan span, ConvertToRawKind kind, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);
            Contract.ThrowIfFalse(span.IntersectsWith(token.Span));
            Contract.ThrowIfFalse(token.Kind() == SyntaxKind.StringLiteralToken);

            var replacement = GetReplacementToken(document, token, kind, options, cancellationToken);
            return document.WithSyntaxRoot(root.ReplaceToken(token, replacement));
        }

        private static SyntaxToken GetReplacementToken(
            Document document,
            SyntaxToken token,
            ConvertToRawKind kind,
            SyntaxFormattingOptions options,
            CancellationToken cancellationToken)
        {
            var characters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            Contract.ThrowIfTrue(characters.IsDefaultOrEmpty);

            // If the user asked to remove whitespace then do so now.
            if (kind == ConvertToRawKind.MultiLineWithoutLeadingWhitespace)
                characters = CleanupWhitespace(characters);

            return kind == ConvertToRawKind.SingleLine
                ? ConvertToSingleLineRawString(token, characters)
                : ConvertToMultiLineRawIndentedString(document, token, options, characters, cancellationToken);
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

        private static void AddRange(ImmutableSegmentedList<VirtualChar>.Builder result, VirtualCharSequence sequence)
        {
            foreach (var c in sequence)
                result.Add(c);
        }

        private static int ComputeCommonWhitespacePrefix(ArrayBuilder<VirtualCharSequence> lines)
        {
            var commonLeadingWhitespace = GetLeadingWhitespace(lines.First());

            for (var i = 1; i < lines.Count; i++)
            {
                if (commonLeadingWhitespace.IsEmpty)
                    return 0;

                var currentLine = lines[i];
                if (AllWhitespace(currentLine))
                    continue;

                var currentLineLeadingWhitespace = GetLeadingWhitespace(currentLine);
                commonLeadingWhitespace = ComputeCommonWhitespacePrefix(commonLeadingWhitespace, currentLineLeadingWhitespace);
            }

            return commonLeadingWhitespace.Length;
        }

        private static VirtualCharSequence ComputeCommonWhitespacePrefix(
            VirtualCharSequence leadingWhitespace1, VirtualCharSequence leadingWhitespace2)
        {
            var length = Math.Min(leadingWhitespace1.Length, leadingWhitespace2.Length);

            var current = 0;
            while (current < length && IsCSharpWhitespace(leadingWhitespace1[current]) && leadingWhitespace1[current].Rune == leadingWhitespace2[current].Rune)
                current++;

            return leadingWhitespace1.GetSubSequence(TextSpan.FromBounds(0, current));
        }

        private static VirtualCharSequence GetLeadingWhitespace(VirtualCharSequence line)
        {
            var current = 0;
            while (current < line.Length && IsCSharpWhitespace(line[current]))
                current++;

            return line.GetSubSequence(TextSpan.FromBounds(0, current));
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

        private static bool AllWhitespace(VirtualCharSequence line)
        {
            var index = 0;
            while (index < line.Length && IsCSharpWhitespace(line[index]))
                index++;

            return index == line.Length || IsCSharpNewLine(line[index]);
        }

        private static SyntaxToken ConvertToMultiLineRawIndentedString(
            Document document,
            SyntaxToken token,
            SyntaxFormattingOptions formattingOptions,
            VirtualCharSequence characters,
            CancellationToken cancellationToken)
        {
            // Have to make sure we have a delimiter longer than any quote sequence in the string.
            var longestQuoteSequence = GetLongestQuoteSequence(characters);
            var quoteDelimeterCount = Math.Max(3, longestQuoteSequence + 1);

            // Auto-formatting options are not relevant since they only control behavior on typing.
            var indentationOptions = new IndentationOptions(formattingOptions, AutoFormattingOptions.Default);
            var indentation = token.GetPreferredIndentation(document, indentationOptions, cancellationToken);

            using var _ = PooledStringBuilder.GetInstance(out var builder);

            builder.Append('"', quoteDelimeterCount);
            builder.Append(formattingOptions.NewLine);

            var atStartOfLine = true;
            for (int i = 0, n = characters.Length; i < n; i++)
            {
                var ch = characters[i];
                if (IsCSharpNewLine(ch))
                {
                    ch.AppendTo(builder);
                    atStartOfLine = true;
                    continue;
                }

                if (atStartOfLine)
                {
                    builder.Append(indentation);
                    atStartOfLine = false;
                }

                ch.AppendTo(builder);
            }

            builder.Append(formattingOptions.NewLine);
            builder.Append(indentation);
            builder.Append('"', quoteDelimeterCount);

            return SyntaxFactory.Token(
                token.LeadingTrivia,
                SyntaxKind.MultiLineRawStringLiteralToken,
                builder.ToString(),
                characters.CreateString(),
                token.TrailingTrivia);
        }

        private static SyntaxToken ConvertToSingleLineRawString(
            SyntaxToken token, VirtualCharSequence characters)
        {
            // Have to make sure we have a delimiter longer than any quote sequence in the string.
            var longestQuoteSequence = GetLongestQuoteSequence(characters);
            var quoteDelimeterCount = Math.Max(3, longestQuoteSequence + 1);

            using var _ = PooledStringBuilder.GetInstance(out var builder);

            builder.Append('"', quoteDelimeterCount);

            foreach (var ch in characters)
                ch.AppendTo(builder);

            builder.Append('"', quoteDelimeterCount);

            return SyntaxFactory.Token(
                token.LeadingTrivia,
                SyntaxKind.SingleLineRawStringLiteralToken,
                builder.ToString(),
                characters.CreateString(),
                token.TrailingTrivia);
        }
    }
}
