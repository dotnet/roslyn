// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal abstract class AbstractSnippetCompletionProvider : CommonCompletionProvider
    {
        private readonly SyntaxAnnotation _cursorAnnotation = new();
        private readonly SyntaxAnnotation _reformatSnippetAnnotation = new();

        protected abstract int GetTargetCaretPosition(SyntaxNode caretTarget);
        protected abstract Task<Document> GenerateDocumentWithSnippetAsync(Document document, CompletionItem completionItem, CancellationToken cancellationToken);
        protected abstract Task<SyntaxNode> GetAnnotatedSnippetRootAsync(Document document, CompletionItem completionItem,
            SyntaxAnnotation reformatAnnotation, CancellationToken cancellationToken);
        protected abstract Task<SyntaxNode> GetAnnotationForCursorAsync(Document document, CompletionItem completionItem,
            SyntaxAnnotation cursorAnnotation, CancellationToken cancellationToken);

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
                var caretTarget = newRoot.GetAnnotatedNodes(_cursorAnnotation).FirstOrDefault();
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
            var originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            Document? snippetContainingDocument;

            // Need to special case if the span is empty because otherwise it takes out preceding
            // characters when we just want to remove any characters to invoke the completion.
            if (completionItem.Span.IsEmpty)
            {
                snippetContainingDocument = await GenerateDocumentWithSnippetAsync(document, completionItem, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var textChange = new TextChange(TextSpan.FromBounds(SnippetCompletionItem.GetTokenSpanStart(completionItem), SnippetCompletionItem.GetTokenSpanEnd(completionItem)),
                    string.Empty);
                originalText = originalText.WithChanges(textChange);
                var documentWithoutInvocationText = document.WithText(originalText);
                snippetContainingDocument = await GenerateDocumentWithSnippetAsync(documentWithoutInvocationText, completionItem, cancellationToken).ConfigureAwait(false);
            }

            if (snippetContainingDocument is null)
            {
                return document;
            }

            var annotatedSnippetRoot = await GetAnnotatedSnippetRootAsync(snippetContainingDocument, completionItem, _reformatSnippetAnnotation, cancellationToken).ConfigureAwait(false);
            snippetContainingDocument = snippetContainingDocument.WithSyntaxRoot(annotatedSnippetRoot);
            var reformattedDocument = await Formatter.FormatAsync(snippetContainingDocument, _reformatSnippetAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            var cursorAnnotatedRoot = await GetAnnotationForCursorAsync(reformattedDocument, completionItem, _cursorAnnotation, cancellationToken).ConfigureAwait(false);
            reformattedDocument = reformattedDocument.WithSyntaxRoot(cursorAnnotatedRoot);
            return await Formatter.FormatAsync(reformattedDocument, _cursorAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
