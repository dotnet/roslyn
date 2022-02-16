// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal abstract class AbstractSnippetProvider : ISnippetProvider
    {
        protected SyntaxAnnotation _cursorAnnotation = new();
        protected SyntaxAnnotation _reformatAnnotation = new();

        protected abstract Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract string GetSnippetDisplayName();
        protected abstract Task<TextChange> GenerateSnippetTextChangeAsync(Document document, TextSpan span, int tokenSpanStart, int tokenSpanEnd, CancellationToken cancellationToken);
        protected abstract Task<SyntaxNode> AnnotateRootForCursorAsync(Document document, TextSpan span, SyntaxAnnotation cursorAnnotation, CancellationToken cancellationToken);
        protected abstract Task<SyntaxNode> AnnotateRootForReformattingAsync(Document document, TextSpan span, SyntaxAnnotation reformatAnnotation, CancellationToken cancellationToken);
        protected abstract int GetTargetCaretPosition(SyntaxNode caretTarget);
        protected abstract Task<ImmutableArray<TextSpan>> GetRenameLocationsAsync(Document document, TextSpan span, CancellationToken cancellationToken);

        public async Task<SnippetData?> GetSnippetDataAsync(Document document, int position, CancellationToken cancellationToken)
        {
            if (await IsValidSnippetLocationAsync(document, position, cancellationToken).ConfigureAwait(false))
            {
                return new SnippetData(GetSnippetDisplayName());
            }

            return null;
        }

        public async Task<Snippet> GetSnippetAsync(Document document, TextSpan span, int tokenSpanStart, int tokenSpanEnd, CancellationToken cancellationToken)
        {
            var textChange = await GenerateSnippetTextChangeAsync(document, span, tokenSpanStart, tokenSpanEnd, cancellationToken).ConfigureAwait(false);
            var snippetDocument = await GetDocumentWithSnippetAsync(document, span, textChange, tokenSpanStart, tokenSpanEnd, cancellationToken).ConfigureAwait(false);
            var reformattedDocument = await ReformatDocumentAsync(snippetDocument, span, cancellationToken).ConfigureAwait(false);
            var changes = await reformattedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var changesArray = changes.ToImmutableArray();
            var newText = await reformattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var change = Utilities.Collapse(newText, changesArray);
            var annotatedRootForCursor = await AnnotateRootForCursorAsync(reformattedDocument, span, _cursorAnnotation, cancellationToken).ConfigureAwait(false);
            reformattedDocument = reformattedDocument.WithSyntaxRoot(annotatedRootForCursor);
            var caretTarget = annotatedRootForCursor.GetAnnotatedNodes(_cursorAnnotation).SingleOrDefault();
            return new Snippet(
                snippetType: GetSnippetDisplayName(),
                textChange: change,
                cursorPosition: GetTargetCaretPosition(caretTarget),
                renameLocations: await GetRenameLocationsAsync(reformattedDocument, span, cancellationToken).ConfigureAwait(false));
        }

        private static async Task<Document> GetDocumentWithSnippetAsync(Document document, TextSpan span, TextChange snippet, int tokenSpanStart, int tokenSpanEnd, CancellationToken cancellationToken)
        {
            var originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            Document? snippetContainingDocument;

            // Need to special case if the span is empty because otherwise it takes out preceding
            // characters when we just want to remove any characters to invoke the completion.
            if (span.IsEmpty)
            {
                originalText = originalText.WithChanges(snippet);
                snippetContainingDocument = document.WithText(originalText);
            }
            else
            {
                var textChange = new TextChange(TextSpan.FromBounds(tokenSpanStart, tokenSpanEnd), string.Empty);
                originalText = originalText.WithChanges(textChange);
                var document1 = document.WithText(originalText);
                var textWithoutInvocation = await document1.GetTextAsync(cancellationToken).ConfigureAwait(false);
                textWithoutInvocation = textWithoutInvocation.WithChanges(snippet);
                snippetContainingDocument = document1.WithText(textWithoutInvocation);
            }

            return snippetContainingDocument;
        }

        private async Task<Document> ReformatDocumentAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var annotatedSnippetRoot = await AnnotateRootForReformattingAsync(document, span, _reformatAnnotation, cancellationToken).ConfigureAwait(false);
            document = document.WithSyntaxRoot(annotatedSnippetRoot);
            return await Formatter.FormatAsync(document, _reformatAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public string GetSnippetText()
        {
            return GetSnippetDisplayName();
        }
    }
}
