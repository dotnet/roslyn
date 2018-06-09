// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Helper class for "Fix all occurrences" code fix providers for Simplification code fix providers that just add simplifier/formatter annotations as part of code fixes.
    /// This provider batches all the simplifier annotation actions within a document into a single code action,
    /// instead of creating separate code actions for each added annotation.
    /// </summary>
    internal abstract class BatchSimplificationFixAllProvider : BatchFixAllProvider
    {
        protected BatchSimplificationFixAllProvider() { }

        protected override async Task AddDocumentFixesAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> fixes,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            // quick bail out
            if (diagnostics.IsEmpty)
            {
                return;
            }

            var changedDocument = await AddSimplifierAnnotationsAsync(
                document, diagnostics, fixAllState, cancellationToken).ConfigureAwait(false);
            var title = GetFixAllTitle(fixAllState);
            var codeAction = new MyCodeAction(title, c => Task.FromResult(changedDocument));
            fixes.Add((diagnostics.First(), codeAction));
        }

        /// <summary>
        /// Get node on which to add simplifier and formatter annotation for fixing the given diagnostic.
        /// </summary>
        protected abstract SyntaxNode GetNodeToSimplify(
            SyntaxNode root, SemanticModel model, Diagnostic diagnostic, 
            DocumentOptionSet options, CancellationToken cancellationToken);

        private async Task<Document> AddSimplifierAnnotationsAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // Find all nodes to simplify corresponding to diagnostic spans.
            var nodesToSimplify = new List<SyntaxNode>();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == fixAllState.CodeActionEquivalenceKey)
                {
                    var node = GetNodeToSimplify(root, model, diagnostic, options, cancellationToken);

                    if (node != null)
                    {
                        nodesToSimplify.Add(node);
                    }
                }
            }

            // Add simplifier and formatter annotations to all nodes to simplify.
            root = root.ReplaceNodes(nodesToSimplify, (o, n) =>
                n.WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation));

            return document.WithSyntaxRoot(root);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
