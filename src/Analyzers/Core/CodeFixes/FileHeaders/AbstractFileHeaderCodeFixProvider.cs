// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Formatting;
#endif

namespace Microsoft.CodeAnalysis.FileHeaders
{
    internal abstract class AbstractFileHeaderCodeFixProvider : CodeFixProvider
    {
        protected abstract ISyntaxFacts SyntaxFacts { get; }
        protected abstract ISyntaxKinds SyntaxKinds { get; }

        protected abstract SyntaxTrivia EndOfLine(string text);
        protected abstract string CommentPrefix { get; }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.FileHeaderMismatch);

        public override FixAllProvider GetFixAllProvider()
            => new FixAll(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(cancellationToken => GetTransformedDocumentAsync(context.Document, cancellationToken)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> GetTransformedDocumentAsync(Document document, CancellationToken cancellationToken)
            => document.WithSyntaxRoot(await GetTransformedSyntaxRootAsync(document, cancellationToken).ConfigureAwait(false));

        private Task<SyntaxNode> GetTransformedSyntaxRootAsync(Document document, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var newLineText = Environment.NewLine;
#else
            var newLineText = document.Project.Solution.Options.GetOption(FormattingOptions.NewLine, document.Project.Language);
#endif
            var newLineTrivia = EndOfLine(newLineText);

            return GetTransformedSyntaxRootAsync(SyntaxFacts, newLineTrivia, document, cancellationToken);
        }

        internal static async Task<SyntaxNode> GetTransformedSyntaxRootAsync(ISyntaxFacts syntaxFacts,
            SyntaxTrivia newLineTrivia, Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            if (!document.Project.AnalyzerOptions.TryGetEditorConfigOption<string>(CodeStyleOptions2.FileHeaderTemplate, tree, out var fileHeaderTemplate)
                || string.IsNullOrEmpty(fileHeaderTemplate))
            {
                // This exception would show up as a gold bar, but as indicated we do not believe this is reachable.
                throw ExceptionUtilities.Unreachable;
            }

            var expectedFileHeader = fileHeaderTemplate.Replace("{fileName}", Path.GetFileName(document.FilePath));

            var fileHeader = syntaxFacts.GetFileBanner(root);

            return fileHeader.Length == 0
                ? AddHeader(syntaxFacts, newLineTrivia, root, expectedFileHeader)
                : ReplaceHeader(syntaxFacts, newLineTrivia, root, expectedFileHeader);
        }

        private static SyntaxNode ReplaceHeader(ISyntaxFacts syntaxFacts, SyntaxTrivia newLineTrivia, SyntaxNode root, string expectedFileHeader)
        {
            var newHeaderTrivia = CreateNewHeader(syntaxFacts, expectedFileHeader, newLineTrivia.ToFullString());

            var existingBanner = syntaxFacts.GetFileBanner(root);
            var allRootTrivia = root.GetLeadingTrivia();
            var (triviaToKeep, bannerInsertationIndex) = RemoveBannerFromRootTrivia(allRootTrivia, existingBanner);

            var trailingWhiteSpaceOrEndOfLineFromExisitingHeader = GetTrailingWhiteSpaceOrEndOfLineFromExisitingHeader(syntaxFacts, existingBanner);
            if (trailingWhiteSpaceOrEndOfLineFromExisitingHeader.IsEmpty)
            {
                trailingWhiteSpaceOrEndOfLineFromExisitingHeader = trailingWhiteSpaceOrEndOfLineFromExisitingHeader
                    .Add(newLineTrivia)
                    .Add(newLineTrivia);
            }
            // Append the whitespace and new lines as it was before
            newHeaderTrivia = newHeaderTrivia.AddRange(trailingWhiteSpaceOrEndOfLineFromExisitingHeader);
            // Insert the new header at the right position in the existing trivia around the header
            newHeaderTrivia = triviaToKeep.InsertRange(bannerInsertationIndex, newHeaderTrivia);

            return root.WithLeadingTrivia(newHeaderTrivia);
        }

        private static ImmutableArray<SyntaxTrivia> GetTrailingWhiteSpaceOrEndOfLineFromExisitingHeader(ISyntaxFacts syntaxFacts,
            ImmutableArray<SyntaxTrivia> existingBanner)
        {
            // Search from the end to find the first non-whitespace or end of line trivia
            var i = existingBanner.Length - 1;
            while (syntaxFacts.IsWhitespaceOrEndOfLineTrivia(existingBanner[i]))
            {
                i--;
            }

            return existingBanner.RemoveRange(0, i + 1);
        }

        private static (SyntaxTriviaList triviaToKeep, int bannerInsertationIndex) RemoveBannerFromRootTrivia(SyntaxTriviaList allRootTrivia,
            ImmutableArray<SyntaxTrivia> banner)
        {
            if (banner.Length == 0)
            {
                return (allRootTrivia, 0);
            }

            // Create a stack of indices of banner trivia within all rootTrivia.
            var existingBannerIndices = new Stack<int>(capacity: allRootTrivia.Count);
            for (var i = 0; i < allRootTrivia.Count; i++)
            {
                if (banner.Contains(allRootTrivia[i]))
                {
                    existingBannerIndices.Push(i);
                }
            }

            // Remove all header trivia from the end. Keep track of "index". It is the start position of the old header 
            // and will become the insert position of the new header later.
            var index = 0;
            while (existingBannerIndices.Count > 0)
            {
                index = existingBannerIndices.Pop();
                allRootTrivia = allRootTrivia.RemoveAt(index);
            }

            return (triviaToKeep: allRootTrivia, bannerInsertationIndex: index);
        }

        private static SyntaxNode AddHeader(ISyntaxFacts syntaxFacts, SyntaxTrivia newLineTrivia, SyntaxNode root, string expectedFileHeader)
        {
            var newTrivia = CreateNewHeader(syntaxFacts, expectedFileHeader, newLineTrivia.ToFullString())
                .Add(newLineTrivia)
                .Add(newLineTrivia);

            // Skip blank lines already at the beginning of the document, since we add our own
            var leadingTrivia = root.GetLeadingTrivia();
            var skipCount = 0;
            for (var i = 0; i < leadingTrivia.Count; i++)
            {
                if (leadingTrivia[i].RawKind == syntaxFacts.SyntaxKinds.EndOfLineTrivia)
                {
                    skipCount = i + 1;
                }
                else if (leadingTrivia[i].RawKind != syntaxFacts.SyntaxKinds.WhitespaceTrivia)
                {
                    break;
                }
            }

            newTrivia = newTrivia.AddRange(leadingTrivia.Skip(skipCount));

            return root.WithLeadingTrivia(newTrivia);
        }

        private static SyntaxTriviaList CreateNewHeader(ISyntaxFacts syntaxFacts, string expectedFileHeader, string newLineText)
        {
            if (TryParseTemplateAsComment(syntaxFacts, expectedFileHeader, newLineText, out var parsedTemplate))
            {
                // The editorconfig file header can be parsed as a comment in the target language. We insert it as is.
                return parsedTemplate;
            }
            var singleLineComment = syntaxFacts.GetSingleLineCommentPrefix();
            var copyrightText = GetCopyrightText(singleLineComment, expectedFileHeader, newLineText);
            var newHeader = copyrightText;
            return syntaxFacts.ParseLeadingTrivia(newHeader);
        }

        private static bool TryParseTemplateAsComment(ISyntaxFacts syntaxFacts, string expectedFileHeader,
            string newLineText, out SyntaxTriviaList result)
        {
            var normalizedHeaderTemplate = GetNormalizedHeaderTemplate(expectedFileHeader, newLineText);
            var tryParseTemplate = syntaxFacts.ParseLeadingTrivia(normalizedHeaderTemplate);

            foreach (var trivia in tryParseTemplate)
            {
                // Skip over leading whitespace
                if (syntaxFacts.IsWhitespaceOrEndOfLineTrivia(trivia))
                {
                    continue;
                }

                if (syntaxFacts.IsRegularComment(trivia))
                {
                    // trivia starts with optional leading whitespace followed by a comment.
                    // Lets check if there are any parsing errors in the trivia (e.g. missing closing block comment token)
                    if (tryParseTemplate.All(t => !t.ContainsDiagnostics))
                    {
                        // One final check: Let's see if the parsed comment round-trips. This fails in cases like this:
                        // file_header_template = // Some text\nMore text
                        // "More text" is not part of tryParseTemplate
                        var parsedComment = GetNormalizedHeaderTemplate(tryParseTemplate.ToFullString(), newLineText);
                        if (string.Equals(parsedComment, normalizedHeaderTemplate, StringComparison.Ordinal))
                        {
                            result = tryParseTemplate;
                            return true;
                        }
                    }
                }

                // The first found (non whitespace) token is not a comment or a comment with diagnostics
                // There is no need to look any further.
                break;
            }

            result = SyntaxTriviaList.Empty;
            return false;
        }

        /// <summary>
        /// Normalize the new line characters for comparison.
        /// </summary>
        /// <param name="original">The text with the new line characters to normalize.</param>
        /// <param name="newLineText">The new line sequence.</param>
        /// <returns>A normalized copy of <paramref name="original"/>.</returns>
        private static string GetNormalizedHeaderTemplate(string original, string newLineText)
        {
            // Source: https://stackoverflow.com/a/141069
            return Regex.Replace(original, @"\r\n|\n\r|\n|\r", newLineText);
        }

        private static string GetCopyrightText(string prefixWithLeadingSpaces, string copyrightText, string newLineText)
        {
            copyrightText = copyrightText.Replace("\r\n", "\n");
            var lines = copyrightText.Split('\n');
            return string.Join(newLineText, lines.Select(line =>
            {
                // Rewrite the lines of the header as comments without trailing whitespace.
                if (string.IsNullOrEmpty(line))
                {
                    // This is a blank line of the header. We want the prefix indicating the line is a comment, but no
                    // additional trailing whitespace.
                    return prefixWithLeadingSpaces;
                }
                else
                {
                    // This is a normal line of the header. We want the prefix, followed by a single space, and then the
                    // text of the header line.
                    return prefixWithLeadingSpaces + " " + line;
                }
            }));
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CodeFixesResources.Add_file_header, createChangedDocument, nameof(AbstractFileHeaderCodeFixProvider))
            {
            }
        }

        private class FixAll : DocumentBasedFixAllProvider
        {
            private readonly AbstractFileHeaderCodeFixProvider _codeFixProvider;

            public FixAll(AbstractFileHeaderCodeFixProvider codeFixProvider)
                => _codeFixProvider = codeFixProvider;

            protected override string CodeActionTitle => CodeFixesResources.Add_file_header;

            protected override Task<SyntaxNode?> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                if (diagnostics.IsEmpty)
                {
                    return SpecializedTasks.Null<SyntaxNode>();
                }

                return _codeFixProvider.GetTransformedSyntaxRootAsync(document, fixAllContext.CancellationToken).AsNullable();
            }
        }
    }
}
