// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddFileBanner
{
    internal abstract class AbstractAddFileBannerCodeRefactoringProvider : SyntaxEditorBasedCodeRefactoringProvider
    {
        private const string BannerFileNamePlaceholder = "{filename}";

        protected abstract bool IsCommentStartCharacter(char ch);

        protected abstract SyntaxTrivia CreateTrivia(SyntaxTrivia trivia, string text);

        protected sealed override ImmutableArray<FixAllScope> SupportedFixAllScopes { get; }
            = ImmutableArray.Create(FixAllScope.Project, FixAllScope.Solution);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            if (!span.IsEmpty)
            {
                return;
            }

            var formattingOptions = await document.GetDocumentFormattingOptionsAsync(context.Options, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(formattingOptions.FileHeaderTemplate))
            {
                // If we have a defined file header template, allow the analyzer and code fix to handle it
                return;
            }

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var position = span.Start;
            var firstToken = root.GetFirstToken();
            if (!firstToken.FullSpan.IntersectsWith(position))
            {
                return;
            }

            if (HasExistingBanner(document, root))
            {
                // Already has a banner.
                return;
            }

            // Process the other documents in this document's project.  Look at the
            // ones that we can get a root from (without having to parse).  Then
            // look at the ones we'd need to parse.
            var siblingDocumentsAndRoots =
                document.Project.Documents
                        .Where(d => d != document)
                        .Select(d =>
                        {
                            d.TryGetSyntaxRoot(out var siblingRoot);
                            return (document: d, root: siblingRoot);
                        })
                        .OrderBy((t1, t2) => (t1.root != null) == (t2.root != null) ? 0 : t1.root != null ? -1 : 1);

            foreach (var (siblingDocument, siblingRoot) in siblingDocumentsAndRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var siblingBanner = await TryGetBannerAsync(siblingDocument, siblingRoot, cancellationToken).ConfigureAwait(false);
                if (siblingBanner.Length > 0 && !siblingDocument.IsGeneratedCode(cancellationToken))
                {
                    context.RegisterRefactoring(
                        CodeAction.Create(
                            CodeFixesResources.Add_file_header,
                            _ => AddBannerAsync(document, root, siblingDocument, siblingBanner),
                            equivalenceKey: GetEquivalenceKey(siblingDocument, siblingBanner)),
                        new Text.TextSpan(position, length: 0));
                    return;
                }
            }
        }

        private static bool HasExistingBanner(Document document, SyntaxNode root)
        {
            var bannerService = document.GetRequiredLanguageService<IFileBannerFactsService>();
            var banner = bannerService.GetFileBanner(root);
            return banner.Length > 0;
        }

        private static string GetEquivalenceKey(Document document, ImmutableArray<SyntaxTrivia> banner)
        {
            var bannerText = banner.Select(trivia => trivia.ToFullString()).Join(string.Empty);

            var fileName = IOUtilities.PerformIO(() => Path.GetFileName(document.FilePath));
            if (!string.IsNullOrEmpty(fileName))
                bannerText = bannerText.Replace(fileName, BannerFileNamePlaceholder);

            return bannerText;
        }

        private static ImmutableArray<SyntaxTrivia> GetBannerFromEquivalenceKey(string equivalenceKey, Document document)
        {
            var fileName = IOUtilities.PerformIO(() => Path.GetFileName(document.FilePath));
            if (!string.IsNullOrEmpty(fileName))
                equivalenceKey = equivalenceKey.Replace(BannerFileNamePlaceholder, fileName);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var token = syntaxFacts.ParseToken(equivalenceKey);

            var bannerService = document.GetRequiredLanguageService<IFileBannerFactsService>();
            return bannerService.GetFileBanner(token);
        }

        private Task<Document> AddBannerAsync(
            Document document, SyntaxNode root,
            Document siblingDocument, ImmutableArray<SyntaxTrivia> banner)
        {
            banner = UpdateEmbeddedFileNames(siblingDocument, document, banner);

            var newRoot = root.WithPrependedLeadingTrivia(new SyntaxTriviaList(banner));
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Looks at <paramref name="banner"/> to see if it contains the name of <paramref name="sourceDocument"/>
        /// in it.  If so, those names will be replaced with <paramref name="destinationDocument"/>'s name.
        /// </summary>
        private ImmutableArray<SyntaxTrivia> UpdateEmbeddedFileNames(
            Document sourceDocument, Document destinationDocument, ImmutableArray<SyntaxTrivia> banner)
        {
            var sourceName = IOUtilities.PerformIO(() => Path.GetFileName(sourceDocument.FilePath));
            var destinationName = IOUtilities.PerformIO(() => Path.GetFileName(destinationDocument.FilePath));
            if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(destinationName))
            {
                return banner;
            }

            using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var result);
            foreach (var trivia in banner)
            {
                var updated = CreateTrivia(trivia, trivia.ToFullString().Replace(sourceName, destinationName));
                result.Add(updated);
            }

            return result.ToImmutable();
        }

        private async Task<ImmutableArray<SyntaxTrivia>> TryGetBannerAsync(
            Document document, SyntaxNode? root, CancellationToken cancellationToken)
        {
            var bannerService = document.GetRequiredLanguageService<IFileBannerFactsService>();
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // If we have a tree already for this document, then just check to see
            // if it has a banner.
            if (root != null)
            {
                return bannerService.GetFileBanner(root);
            }

            // Didn't have a tree.  Don't want to parse the file if we can avoid it.
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            if (text.Length == 0 || !IsCommentStartCharacter(text[0]))
            {
                // Didn't start with a comment character, don't bother looking at 
                // this file.
                return ImmutableArray<SyntaxTrivia>.Empty;
            }

            var token = syntaxFacts.ParseToken(text.ToString());
            return bannerService.GetFileBanner(token);
        }

        protected sealed override async Task FixAllAsync(
            Document document,
            ImmutableArray<TextSpan> fixAllSpans,
            SyntaxEditor editor,
            CodeActionOptionsProvider optionsProvider,
            string? equivalenceKey,
            CancellationToken cancellationToken)
        {
            Debug.Assert(equivalenceKey != null);

            // Bail out if the document to fix already has an existing banner.
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (HasExistingBanner(document, root))
                return;

            // Get banner from the equivalence key.
            var banner = GetBannerFromEquivalenceKey(equivalenceKey, document);
            Debug.Assert(banner.Length > 0);

            // Finally add the banner to the document to be fixed.
            var newRoot = root.WithPrependedLeadingTrivia(new SyntaxTriviaList(banner));
            editor.ReplaceNode(editor.OriginalRoot, newRoot);
        }
    }
}
