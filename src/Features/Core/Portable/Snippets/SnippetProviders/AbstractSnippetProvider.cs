// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis.Snippets;

internal abstract class AbstractSnippetProvider<TSnippetSyntax> : ISnippetProvider
    where TSnippetSyntax : SyntaxNode
{
    public abstract string Identifier { get; }
    public abstract string Description { get; }

    public virtual ImmutableArray<string> AdditionalFilterTexts => [];

    protected readonly SyntaxAnnotation FindSnippetAnnotation = new();

    /// <summary>
    /// Implemented by each SnippetProvider to determine if that particular position is a valid
    /// location for the snippet to be inserted.
    /// </summary>
    protected abstract bool IsValidSnippetLocation(in SnippetContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Generates the new snippet's TextChanges that are being inserted into the document.
    /// </summary>
    protected abstract Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the position that we want the caret to be at after all of the indentation/formatting has been done.
    /// </summary>
    protected abstract int GetTargetCaretPosition(TSnippetSyntax caretTarget, SourceText sourceText);

    /// <summary>
    /// Method to find the locations that must be renamed and where tab stops must be inserted into the snippet.
    /// </summary>
    protected abstract ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(TSnippetSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken);

    public ValueTask<bool> IsValidSnippetLocationAsync(in SnippetContext context, CancellationToken cancellationToken)
    {
        var syntaxFacts = context.Document.GetRequiredLanguageService<ISyntaxFactsService>();
        var syntaxTree = context.SyntaxContext.SyntaxTree;
        if (syntaxFacts.IsInNonUserCode(syntaxTree, context.Position, cancellationToken))
        {
            return ValueTaskFactory.FromResult(false);
        }

        return ValueTaskFactory.FromResult(IsValidSnippetLocation(in context, cancellationToken));
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
        var documentWithIndentation = await AddIndentationToDocumentAsync(reformattedDocument, cancellationToken).ConfigureAwait(false);

        var reformattedRoot = await documentWithIndentation.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var mainChangeNode = (TSnippetSyntax)reformattedRoot.GetAnnotatedNodes(FindSnippetAnnotation).First();

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
            cursorPosition: GetTargetCaretPosition(mainChangeNode, sourceText),
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
            var addImportPlacementOptions = await document.GetAddImportPlacementOptionsAsync(cancellationToken).ConfigureAwait(false);
            var simplifierOptions = await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

            document = await ImportAdder.AddImportsFromSymbolAnnotationAsync(
                document, FindSnippetAnnotation, addImportPlacementOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            document = await Simplifier.ReduceAsync(document, FindSnippetAnnotation, simplifierOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            // format any node with explicit formatter annotation
            document = await Formatter.FormatAsync(document, FindSnippetAnnotation, syntaxFormattingOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

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
        var nearestStatement = FindAddedSnippetSyntaxNode(root, position);

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
        var annotatedSnippetRoot = await AnnotateNodesToReformatAsync(document, position, cancellationToken).ConfigureAwait(false);
        document = document.WithSyntaxRoot(annotatedSnippetRoot);
        return document;
    }

    /// <summary>
    /// Method to added formatting annotations to the created snippet.
    /// </summary>
    protected virtual async Task<SyntaxNode> AnnotateNodesToReformatAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var snippetExpressionNode = FindAddedSnippetSyntaxNode(root, position);
        Contract.ThrowIfNull(snippetExpressionNode);

        var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(FindSnippetAnnotation, Simplifier.Annotation, Formatter.Annotation);
        return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
    }

    protected virtual TSnippetSyntax? FindAddedSnippetSyntaxNode(SyntaxNode root, int position)
        => root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true) as TSnippetSyntax;

    /// <summary>
    /// Certain snippets require more indentation - snippets with blocks.
    /// The SyntaxGenerator does not insert this space for us nor does the LSP Snippet Expander.
    /// We need to manually add that spacing to snippets containing blocks.
    /// </summary>
    private async Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var snippetNode = root.GetAnnotatedNodes(FindSnippetAnnotation).FirstOrDefault();

        if (snippetNode is not TSnippetSyntax snippet)
            return document;

        return await AddIndentationToDocumentAsync(document, snippet, cancellationToken).ConfigureAwait(false);
    }

    protected virtual Task<Document> AddIndentationToDocumentAsync(Document document, TSnippetSyntax snippet, CancellationToken cancellationToken)
        => Task.FromResult(document);
}
