// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractSnippetProvider : ISnippetProvider
    {
        public abstract string Identifier { get; }
        public abstract string Description { get; }

        public virtual ImmutableArray<string> AdditionalFilterTexts => ImmutableArray<string>.Empty;

        protected readonly SyntaxAnnotation _cursorAnnotation = new();
        protected readonly SyntaxAnnotation _findSnippetAnnotation = new();

        /// <summary>
        /// Implemented by each SnippetProvider to determine if that particular position is a valid
        /// location for the snippet to be inserted.
        /// </summary>
        protected abstract Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Generates the new snippet's TextChanges that are being inserted into the document.
        /// </summary>
        protected abstract Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the position that we want the caret to be at after all of the indentation/formatting has been done.
        /// </summary>
        protected abstract int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText);

        /// <summary>
        /// Helper function to retrieve the specific type of snippet syntax when it needs to be searched for again.
        /// </summary>
        protected abstract Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts);

        /// <summary>
        /// Method to find the locations that must be renamed and where tab stops must be inserted into the snippet.
        /// </summary>
        protected abstract ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken);

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

            return new SnippetData(Description, Identifier, AdditionalFilterTexts);
        }

        /// <summary>
        /// Handles all the work to generate the Snippet.
        /// Reformats the document with the snippet TextChange and annotates 
        /// appropriately for the cursor to get the target cursor position.
        /// </summary>
        public async Task<SnippetChange> GetSnippetAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Generates the snippet as a list of text changes
            var textChanges = await GenerateSnippetTextChangesAsync(document, position, cancellationToken).ConfigureAwait(false);

            // Applies the snippet text changes to the document 
            var snippetDocument = await GetDocumentWithSnippetAsync(document, textChanges, cancellationToken).ConfigureAwait(false);

            // Finds the inserted snippet and replaces the node in the document with a node that has added trivia
            // since all trivia is removed when converted to a TextChange.
            var snippetWithTriviaDocument = await GetDocumentWithSnippetAndTriviaAsync(snippetDocument, position, syntaxFacts, cancellationToken).ConfigureAwait(false);

            // Adds annotations to inserted snippet to be formatted, simplified, add imports if needed, etc.
            var formatAnnotatedSnippetDocument = await AddFormatAnnotationAsync(snippetWithTriviaDocument, position, cancellationToken).ConfigureAwait(false);

            // Goes through and calls upon the formatting engines that the previous step annotated.
            var reformattedDocument = await CleanupDocumentAsync(formatAnnotatedSnippetDocument, cancellationToken).ConfigureAwait(false);

            // Finds the added snippet and adds indentation where necessary (braces).
            var documentWithIndentation = await AddIndentationToDocumentAsync(reformattedDocument, position, syntaxFacts, cancellationToken).ConfigureAwait(false);

            var reformattedRoot = await documentWithIndentation.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var caretTarget = reformattedRoot.GetAnnotatedNodes(_cursorAnnotation).FirstOrDefault();
            var mainChangeNode = reformattedRoot.GetAnnotatedNodes(_findSnippetAnnotation).FirstOrDefault();

            Contract.ThrowIfNull(caretTarget);
            Contract.ThrowIfNull(mainChangeNode);

            var annotatedReformattedDocument = documentWithIndentation.WithSyntaxRoot(reformattedRoot);

            // All the TextChanges from the original document. Will include any imports (if necessary) and all snippet associated
            // changes after having been formatted.
            var changes = await annotatedReformattedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);

            // Gets a listing of the identifiers that need to be found in the snippet TextChange
            // and their associated TextSpan so they can later be converted into an LSP snippet format.
            var placeholders = GetPlaceHolderLocationsList(mainChangeNode, syntaxFacts, cancellationToken);

            // All the changes from the original document to the most updated. Will later be
            // collapsed into one collapsed TextChange.
            var changesArray = changes.ToImmutableArray();
            var sourceText = await annotatedReformattedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            return new SnippetChange(
                textChanges: changesArray,
                cursorPosition: GetTargetCaretPosition(syntaxFacts, caretTarget, sourceText),
                placeholders: placeholders);
        }

        /// <summary>
        /// Descends into the inserted snippet to add back trivia on every token.
        /// </summary>
        private static SyntaxNode? GenerateElasticTriviaForSyntax(ISyntaxFacts syntaxFacts, SyntaxNode? node)
        {
            if (node is null)
            {
                return null;
            }

            var allTokens = node.DescendantTokens(descendIntoTrivia: true).ToList();

            // Skips the first and last token since
            // those do not need elastic trivia added to them.
            var nodeWithTrivia = node.ReplaceTokens(allTokens.Skip(1).Take(allTokens.Count - 2),
                (oldToken, _) => oldToken.WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation)
                .WithAppendedTrailingTrivia(syntaxFacts.ElasticMarker)
                .WithPrependedLeadingTrivia(syntaxFacts.ElasticMarker));

            return nodeWithTrivia;
        }

        private async Task<Document> CleanupDocumentAsync(
            Document document, CancellationToken cancellationToken)
        {
            if (document.SupportsSyntaxTree)
            {
                var addImportPlacementOptions = await document.GetAddImportPlacementOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
                var simplifierOptions = await document.GetSimplifierOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
                var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);

                document = await ImportAdder.AddImportsFromSymbolAnnotationAsync(
                    document, _findSnippetAnnotation, addImportPlacementOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

                document = await Simplifier.ReduceAsync(document, _findSnippetAnnotation, simplifierOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

                // format any node with explicit formatter annotation
                document = await Formatter.FormatAsync(document, _findSnippetAnnotation, syntaxFormattingOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

                // format any elastic whitespace
                document = await Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation, syntaxFormattingOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        /// <summary>
        /// Locates the snippet that was inserted. Generates trivia for every token in that SyntaxNode.
        /// Replaces the SyntaxNodes and gets back the new document.
        /// </summary>
        private async Task<Document> GetDocumentWithSnippetAndTriviaAsync(Document snippetDocument, int position, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            var root = await snippetDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nearestStatement = FindAddedSnippetSyntaxNode(root, position, GetSnippetContainerFunction(syntaxFacts));

            if (nearestStatement is null)
            {
                return snippetDocument;
            }

            var nearestStatementWithTrivia = GenerateElasticTriviaForSyntax(syntaxFacts, nearestStatement);

            if (nearestStatementWithTrivia is null)
            {
                return snippetDocument;
            }

            root = root.ReplaceNode(nearestStatement, nearestStatementWithTrivia);
            return snippetDocument.WithSyntaxRoot(root);
        }

        private static async Task<Document> GetDocumentWithSnippetAsync(Document document, ImmutableArray<TextChange> snippets, CancellationToken cancellationToken)
        {
            var originalText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

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

        /// <summary>
        /// Method to added formatting annotations to the created snippet.
        /// </summary>
        protected virtual async Task<SyntaxNode> AnnotateNodesToReformatAsync(Document document,
            SyntaxAnnotation findSnippetAnnotation, SyntaxAnnotation cursorAnnotation, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var snippetExpressionNode = FindAddedSnippetSyntaxNode(root, position, GetSnippetContainerFunction(syntaxFacts));
            Contract.ThrowIfNull(snippetExpressionNode);

            var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(findSnippetAnnotation, cursorAnnotation, Simplifier.Annotation, Formatter.Annotation);
            return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
        }

        protected virtual SyntaxNode? FindAddedSnippetSyntaxNode(SyntaxNode root, int position, Func<SyntaxNode?, bool> isCorrectContainer)
        {
            var closestNode = root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true);

            if (!isCorrectContainer(closestNode))
            {
                return null;
            }

            return closestNode;
        }

        /// <summary>
        /// Certain snippets require more indentation - snippets with blocks.
        /// The SyntaxGenerator does not insert this space for us nor does the LSP Snippet Expander.
        /// We need to manually add that spacing to snippets containing blocks.
        /// </summary>
        protected virtual async Task<Document> AddIndentationToDocumentAsync(Document document, int position, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            return await Task.FromResult(document).ConfigureAwait(false);
        }
    }
}
