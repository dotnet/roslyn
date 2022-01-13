// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets
{
    internal abstract class AbstractSnippetCompletionProvider : CommonCompletionProvider
    {
        private readonly SyntaxAnnotation _annotation = new();

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
            var line = text.Lines[SnippetCompletionProvider.GetLine(completionItem)];

            // Annotate the line we care about so we can find it after adding usings
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = GetToken(completionItem, tree, cancellationToken);
            var annotatedRoot = tree.GetRoot(cancellationToken).ReplaceToken(token, token.WithAdditionalAnnotations(_otherAnnotation));
            document = document.WithSyntaxRoot(annotatedRoot);

            var memberContainingDocument = await GenerateMemberAndUsingsAsync(document, completionItem, line, cancellationToken).ConfigureAwait(false);
            if (memberContainingDocument == null)
            {
                // Generating the new document failed because we somehow couldn't resolve
                // the underlying symbol's SymbolKey. At this point, we won't be able to 
                // make any changes, so just return the document we started with.
                return document;
            }

            var insertionRoot = await GetTreeWithAddedSyntaxNodeRemovedAsync(memberContainingDocument, cancellationToken).ConfigureAwait(false);
            var insertionText = await GenerateInsertionTextAsync(memberContainingDocument, cancellationToken).ConfigureAwait(false);

            var destinationSpan = ComputeDestinationSpan(insertionRoot);

            var finalText = insertionRoot.GetText(text.Encoding)
                .Replace(destinationSpan, insertionText.Trim());

            document = document.WithText(finalText);
            var newRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = GetSyntax(newRoot.FindToken(destinationSpan.End));

            document = document.WithSyntaxRoot(newRoot.ReplaceNode(declaration, declaration.WithAdditionalAnnotations(_annotation)));
            return await Formatter.FormatAsync(document, _annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
