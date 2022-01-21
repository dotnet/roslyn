// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertToRawString
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRawString), Shared]
    internal class ConvertRegularStringToRawStringCodeRefactoringProvider : CodeRefactoringProvider
    {
        private enum ConvertToRawKind
        {
            SingleLine,
            MultiLine,
            MultiLineIndented,
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

            if (canBeSingleLine)
            {
                context.RegisterRefactoring(
                    new MyCodeAction(
                        CSharpFeaturesResources.Convert_to_raw_string,
                        c => UpdateDocumentAsync(document, span, ConvertToRawKind.SingleLine, c),
                        nameof(CSharpFeaturesResources.Convert_to_raw_string) + "-" + ConvertToRawKind.SingleLine,
                        priority),
                    token.Span);
            }
            else
            {
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(true);
                sourceText.GetLineAndOffset(token.SpanStart, out _, out var lineOffset);

                context.RegisterRefactoring(
                    new MyCodeAction(
                        CSharpFeaturesResources.Convert_to_raw_string,
                        c => UpdateDocumentAsync(document, span, ConvertToRawKind.MultiLine, c),
                        nameof(CSharpFeaturesResources.Convert_to_raw_string),
                        priority),
                    token.Span);

                if (lineOffset > 0)
                {
                    context.RegisterRefactoring(
                        new MyCodeAction(
                            CSharpFeaturesResources.Convert_to_raw_string_indented,
                            c => UpdateDocumentAsync(document, span, ConvertToRawKind.MultiLineIndented, c),
                            nameof(CSharpFeaturesResources.Convert_to_raw_string_indented),
                            priority),
                        token.Span);
                }
            }
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
            if (characters.Any(static ch => IsNewLine(ch)))
                return false;

            return true;
        }

        private static bool IsNewLine(VirtualChar ch)
            => ch.Rune.Utf16SequenceLength == 1 && SyntaxFacts.IsNewLine((char)ch.Value);

        private static bool AllEscapesAreQuotes(VirtualCharSequence sequence)
        {
            var hasEscape = false;

            foreach (var ch in sequence)
            {
                if (ch.Span.Length > 1)
                {
                    hasEscape = true;
                    if (ch.Value != '"')
                        return false;
                }
            }

            return hasEscape;
        }

        private static bool IsInDirective(SyntaxNode? node)
        {
            while (node != null)
            {
                if (node is DirectiveTriviaSyntax)
                    return true;

                node = node.GetParent(ascendOutOfTrivia: true);
            }

            return false;
        }

        private static bool CanConvert(VirtualChar ch)
        {
            // Don't bother with unpaired surrogates.  This is just a legacy language corner case that we don't care to
            // even try having support for.
            if (ch.SurrogateChar != 0)
                return false;

            // Can't ever encode a null value directly in a c# file as our lexer/parser/text apis will stop righ there.
            if (ch.Rune.Value == 0)
                return false;

            // Check if we have an escaped character in the original string.
            if (ch.Span.Length > 1)
            {
                // An escaped newline is fine to convert (to a multi-line raw string).
                if (IsNewLine(ch))
                    return true;

                // Control/formatting unicode escapes should stay as escapes.  The user code will just be enormously
                // difficult to read/reason about if we convert those to the actual corresponding non-escaped chars.
                var category = Rune.GetUnicodeCategory(ch.Rune);
                if (category is UnicodeCategory.Format or UnicodeCategory.Control)
                    return false;
            }

            return true;
        }

        private static async Task<Document> UpdateDocumentAsync(
            Document document, TextSpan span, ConvertToRawKind kind, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var newLine = options.GetOption(FormattingOptions.NewLine);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);
            Contract.ThrowIfFalse(span.IntersectsWith(token.Span));
            Contract.ThrowIfFalse(token.Kind() == SyntaxKind.StringLiteralToken);

            var replacement = await GetReplacementTokenAsync(document, token, kind, newLine, cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(root.ReplaceToken(token, replacement));
        }

        private static async Task<SyntaxToken> GetReplacementTokenAsync(
            Document document,
            SyntaxToken token,
            ConvertToRawKind kind,
            string newLine,
            CancellationToken cancellationToken)
        {
            return kind switch
            {
                ConvertToRawKind.SingleLine => ConvertToSingleLineRawString(token),
                ConvertToRawKind.MultiLine => ConvertToMultiLineRawString(token, newLine),
                ConvertToRawKind.MultiLineIndented => await ConvertToMultiLineRawIndentedStringAsync(document, token, newLine, cancellationToken)
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }

        private static Task ConvertToMultiLineRawIndentedStringAsync(Document document, SyntaxToken token, string newLine, CancellationToken cancellationToken)
        {
            var characters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            Contract.ThrowIfTrue(characters.IsDefaultOrEmpty);

            // Have to make sure we have a delimiter longer than any quote sequence in the string.
            var longestQuoteSequence = GetLongestQuoteSequence(characters);
            var quoteDelimeterCount = Math.Max(3, longestQuoteSequence + 1);
            var indentation = await DetermineIndentationAsync(document, token, cancellationToken).ConfigureAwait(false);

            using var _ = PooledStringBuilder.GetInstance(out var builder);

            builder.Append('"', quoteDelimeterCount);
            builder.Append(newLine);

            foreach (var ch in characters)
                ch.AppendTo(builder);

            builder.Append(newLine);
            builder.Append('"', quoteDelimeterCount);

            return SyntaxFactory.Token(
                token.LeadingTrivia,
                SyntaxKind.MultiLineRawStringLiteralToken,
                builder.ToString(),
                characters.CreateString(),
                token.TrailingTrivia);
        }

        private static SyntaxToken ConvertToMultiLineRawString(SyntaxToken token, string newLine)
        {
            var characters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            Contract.ThrowIfTrue(characters.IsDefaultOrEmpty);

            // Have to make sure we have a delimiter longer than any quote sequence in the string.
            var longestQuoteSequence = GetLongestQuoteSequence(characters);
            var quoteDelimeterCount = Math.Max(3, longestQuoteSequence + 1);

            using var _ = PooledStringBuilder.GetInstance(out var builder);

            builder.Append('"', quoteDelimeterCount);
            builder.Append(newLine);

            foreach (var ch in characters)
                ch.AppendTo(builder);

            builder.Append(newLine);
            builder.Append('"', quoteDelimeterCount);

            return SyntaxFactory.Token(
                token.LeadingTrivia,
                SyntaxKind.MultiLineRawStringLiteralToken,
                builder.ToString(),
                characters.CreateString(),
                token.TrailingTrivia);
        }

        private static SyntaxToken ConvertToSingleLineRawString(SyntaxToken token)
        {
            var characters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            Contract.ThrowIfTrue(characters.IsDefaultOrEmpty);

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

        private static int GetLongestQuoteSequence(VirtualCharSequence characters)
        {
            var longestQuoteSequence = 0;
            for (int i = 0, n = characters.Length; i < n;)
            {
                int j;
                for (j = i; j < n && characters[j] == '"'; j++)
                    ;

                longestQuoteSequence = Math.Max(longestQuoteSequence, j - i);
                i = j + 1;
            }

            return longestQuoteSequence;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            internal override CodeActionPriority Priority { get; }

            public MyCodeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument,
                string equivalenceKey,
                CodeActionPriority priority)
                : base(title, createChangedDocument, equivalenceKey)
            {
                Priority = priority;
            }
        }
    }
}
