// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractSnippetProvider : ISnippetProvider
    {
        public abstract string SnippetIdentifier { get; }
        public abstract string SnippetDisplayName { get; }

        protected readonly SyntaxAnnotation _cursorAnnotation = new();
        protected readonly SyntaxAnnotation _findSnippetAnnotation = new();

        /// <summary>
        /// Implemented by each SnippetProvider to determine if that particular position is a valid
        /// location for the snippet to be inserted.
        /// </summary>
        protected abstract Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Generates the new snippet's TextChanges that are being inserted into the document
        /// </summary>
        protected abstract Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Method for each snippet to locate the inserted SyntaxNode to reformat
        /// </summary>
        protected abstract Task<SyntaxNode> AnnotateNodesToReformatAsync(Document document, SyntaxAnnotation reformatAnnotation, SyntaxAnnotation cursorAnnotation, int position, CancellationToken cancellationToken);
        protected abstract int? GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget);

        /// <summary>
        /// Determines if the location is valid for a snippet,
        /// if so, then it creates a SnippetData.
        /// </summary>
        public async Task<SnippetData?> GetSnippetDataAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            {
                return null;
            }

            if (!await IsValidSnippetLocationAsync(document, position, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return new SnippetData(SnippetDisplayName, SnippetIdentifier);
        }

        /// <summary>
        /// Handles all the work to generate the Snippet.
        /// Reformats the document with the snippet TextChange and annotates 
        /// appropriately for the cursor to get the target cursor position.
        /// </summary>
        public async Task<SnippetChange> GetSnippetAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var textChanges = await GenerateSnippetTextChangesAsync(document, position, cancellationToken).ConfigureAwait(false);
            var snippetDocument = await GetDocumentWithSnippetAsync(document, textChanges, cancellationToken).ConfigureAwait(false);

            var formatAnnotatedSnippetDocument = await AddFormatAnnotationAsync(snippetDocument, position, cancellationToken).ConfigureAwait(false);
            var reformattedDocument = await CleanupDocumentAsync(formatAnnotatedSnippetDocument, cancellationToken).ConfigureAwait(false);
            var reformattedRoot = await reformattedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var caretTarget = reformattedRoot.GetAnnotatedNodes(_cursorAnnotation).SingleOrDefault();
            var changes = await reformattedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var mainChange = changes.Where(x => x.Span.Start == position).FirstOrDefault();
            return new SnippetChange(
                mainTextChange: mainChange,
                textChanges: changes.ToImmutableArray(),
                cursorPosition: GetTargetCaretPosition(syntaxFacts, caretTarget),
                );
        }

        private async Task<Document> CleanupDocumentAsync(
            Document document, CancellationToken cancellationToken)
        {
            if (document.SupportsSyntaxTree)
            {
                var addImportOptions = await AddImportPlacementOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);

                document = await ImportAdder.AddImportsFromSymbolAnnotationAsync(
                    document, _findSnippetAnnotation, addImportOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

                document = await Simplifier.ReduceAsync(document, _findSnippetAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);

                // format any node with explicit formatter annotation
                document = await Formatter.FormatAsync(document, _findSnippetAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);

                // format any elastic whitespace
                document = await Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        private static async Task<Document> GetDocumentWithSnippetAsync(Document document, ImmutableArray<TextChange> snippets, CancellationToken cancellationToken)
        {
            var originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            originalText = originalText.WithChanges(snippets);
            var snippetDocument = document.WithText(originalText);

            return snippetDocument;
        }

        private async Task<Document> AddFormatAnnotationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var annotatedSnippetRoot = await AnnotateNodesToReformatAsync(document, _findSnippetAnnotation, _cursorAnnotation, position, cancellationToken).ConfigureAwait(false);
            document = document.WithSyntaxRoot(annotatedSnippetRoot);
            return document;
        }
    }
}
