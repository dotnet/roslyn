// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal abstract class AbstractSnippetCompletionProvider : CommonCompletionProvider
    {
        private readonly SyntaxAnnotation _annotation = new();
        private readonly SyntaxAnnotation _otherAnnotation = new();

        protected abstract int GetTargetCaretPosition(SyntaxNode caretTarget);
        protected abstract SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken);
        protected abstract SyntaxNode GetSyntax(SyntaxToken commonSyntaxToken);
        protected abstract Task<Document> GenerateDocumentWithSnippetAsync(Document document, CompletionItem completionItem, TextLine line, CancellationToken cancellationToken);

        public AbstractSnippetCompletionProvider()
        {

        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            var newDocument = await DetermineNewDocumentAsync(document, item, cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            int? newPosition = null;

            // Attempt to find the inserted node and move the caret appropriately
            if (newRoot != null)
            {
                var caretTarget = newRoot.GetAnnotatedNodes(_annotation).FirstOrDefault();
                if (caretTarget != null)
                {
                    var targetPosition = GetTargetCaretPosition(caretTarget);

                    // Something weird happened and we failed to get a valid position.
                    // Bail on moving the caret.
                    if (targetPosition > 0 && targetPosition <= newText.Length)
                    {
                        newPosition = targetPosition;
                    }
                }
            }

            var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var changesArray = changes.ToImmutableArray();
            var change = Utilities.Collapse(newText, changesArray);

            return CompletionChange.Create(change, changesArray, newPosition, includesCommitCharacter: true);
        }

        private async Task<Document> DetermineNewDocumentAsync(Document document, CompletionItem completionItem, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // The span we're going to replace
            var line = text.Lines[SnippetCompletionItem.GetLine(completionItem)];

            // Annotate the line we care about so we can find it after adding usings
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = GetToken(completionItem, tree, cancellationToken);
            var annotatedRoot = tree.GetRoot(cancellationToken).ReplaceToken(token, token.WithAdditionalAnnotations(_otherAnnotation));
            document = document.WithSyntaxRoot(annotatedRoot);

            var snippetContainingDocument = await GenerateDocumentWithSnippetAsync(document, completionItem, line, cancellationToken).ConfigureAwait(false);

            if (snippetContainingDocument is null)
            {
                return document;
            }

            var insertionRoot = await GetTreeWithAddedSyntaxNodeRemovedAsync(snippetContainingDocument, cancellationToken).ConfigureAwait(false);
            var insertionText = await GenerateInsertionTextAsync(snippetContainingDocument, cancellationToken).ConfigureAwait(false);
            var destinationSpan = ComputeDestinationSpan(insertionRoot);

            var finalText = insertionRoot.GetText(text.Encoding)
                .Replace(destinationSpan, insertionText.Trim());

            document = document.WithText(finalText);
            var newRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = GetSyntax(newRoot.FindToken(destinationSpan.End));

            document = document.WithSyntaxRoot(newRoot.ReplaceNode(declaration, declaration.WithAdditionalAnnotations(_annotation)));
            return await Formatter.FormatAsync(document, _annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<SyntaxNode> GetTreeWithAddedSyntaxNodeRemovedAsync(
            Document document, CancellationToken cancellationToken)
        {
            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, optionSet: null, cancellationToken).ConfigureAwait(false);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var members = root.GetAnnotatedNodes(_annotation).AsImmutable();

            root = root.RemoveNodes(members, SyntaxRemoveOptions.KeepLeadingTrivia);
            Contract.ThrowIfNull(root);

            var dismemberedDocument = document.WithSyntaxRoot(root);

            dismemberedDocument = await Formatter.FormatAsync(dismemberedDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await dismemberedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> GenerateInsertionTextAsync(
            Document document, CancellationToken cancellationToken)
        {
            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, optionSet: null, cancellationToken).ConfigureAwait(false);
            document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root.GetAnnotatedNodes(_annotation).Single().ToString().Trim();
        }

        private TextSpan ComputeDestinationSpan(SyntaxNode insertionRoot)
        {
            var targetToken = insertionRoot.GetAnnotatedTokens(_otherAnnotation).FirstOrNull();
            Contract.ThrowIfNull(targetToken);

            var text = insertionRoot.GetText();
            var line = text.Lines.GetLineFromPosition(targetToken.Value.Span.End);
            var position = line.GetFirstNonWhitespacePosition();
            Contract.ThrowIfNull(position);

            var firstToken = insertionRoot.FindToken(position.Value);
            return TextSpan.FromBounds(firstToken.SpanStart, line.End);
        }
    }
}
