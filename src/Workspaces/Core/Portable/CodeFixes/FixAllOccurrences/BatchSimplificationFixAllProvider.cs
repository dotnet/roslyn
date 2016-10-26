// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal class BatchSimplificationFixAllProvider : BatchFixAllProvider
    {
        public static new readonly FixAllProvider Instance = new BatchSimplificationFixAllProvider();

        protected BatchSimplificationFixAllProvider() { }

        public override async Task AddDocumentFixesAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, 
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            var changedDocument = await AddSimplifierAnnotationsAsync(
                document, diagnostics, fixAllState, cancellationToken).ConfigureAwait(false);
            var title = GetFixAllTitle(fixAllState);
            var codeAction = new MyCodeAction(title, (c) => Task.FromResult(changedDocument));
            addFix(codeAction);
        }

        /// <summary>
        /// Get node on which to add simplifier and formatter annotation for fixing the given diagnostic.
        /// </summary>
        protected virtual SyntaxNode GetNodeToSimplify(SyntaxNode root, SemanticModel model, Diagnostic diagnostic, DocumentOptionSet options, out string codeActionEquivalenceKey, CancellationToken cancellationToken)
        {
            codeActionEquivalenceKey = null;
            var span = diagnostic.Location.SourceSpan;
            return root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true);
        }

        /// <summary>
        /// Override this method to add simplify annotations/fixup any parent nodes of original nodes to simplify.
        /// Additionally, this method should also add simplifier annotation to the given nodeToSimplify.
        /// See doc comments on <see cref="NeedsParentFixup"/>.
        /// </summary>
        protected virtual Task<Document> AddSimplifyAnnotationsAsync(Document document, SyntaxNode nodeToSimplify, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// By default, this property returns false and <see cref="AddSimplifierAnnotationsAsync(Document, ImmutableArray{Diagnostic}, FixAllState, CancellationToken)"/> will just add <see cref="Simplifier.Annotation"/> to each node to simplify
        /// returned by <see cref="GetNodeToSimplify(SyntaxNode, SemanticModel, Diagnostic, DocumentOptionSet, out string, CancellationToken)"/>.
        /// 
        /// Override this property to return true if the fix all provider needs to add simplify annotations/fixup any of the parent nodes of the nodes to simplify.
        /// This could be the case if simplifying certain nodes can enable cascaded simplifications, such as parentheses removal on parenting node.
        /// <see cref="AddSimplifierAnnotationsAsync(Document, ImmutableArray{Diagnostic}, FixAllState, CancellationToken)"/> will end up invoking <see cref="AddSimplifyAnnotationsAsync(Document, SyntaxNode, CancellationToken)"/> for each node to simplify.
        /// Ensure that you override <see cref="AddSimplifyAnnotationsAsync(Document, SyntaxNode, CancellationToken)"/> method when this property returns true.
        /// </summary>
        protected virtual bool NeedsParentFixup { get { return false; } }

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
                string codeActionEquivalenceKey;
                var node = GetNodeToSimplify(root, model, diagnostic, options, out codeActionEquivalenceKey, cancellationToken);
                if (node != null && fixAllState.CodeActionEquivalenceKey == codeActionEquivalenceKey)
                {
                    nodesToSimplify.Add(node);
                }
            }

            // Add simplifier and formatter annotations to all nodes to simplify.
            // If the fix all provider needs to fixup any of the parent nodes, then we iterate through each of the nodesToSimplify
            // and fixup any parenting node, computing a new document with required simplifier annotations in each iteration.
            // Otherwise, if the fix all provider doesn't need parent fixup, we just add simplifier annotation to all nodesToSimplify.
            if (!NeedsParentFixup)
            {
                root = root.ReplaceNodes(nodesToSimplify, (o, n) =>
                    n.WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation));
            }
            else
            {
                // Add a custom annotation to nodesToSimplify so we can get back to them later.
                var annotation = new SyntaxAnnotation();
                root = root.ReplaceNodes(nodesToSimplify, (o, n) =>
                    o.WithAdditionalAnnotations(annotation));
                document = document.WithSyntaxRoot(root);

                while (true)
                {
                    root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var annotatedNodes = root.GetAnnotatedNodes(annotation);

                    // Get the next un-processed node to simplify, processed nodes should have simplifier annotation.
                    var annotatedNode = annotatedNodes.FirstOrDefault(n => !n.HasAnnotation(Simplifier.Annotation));
                    if (annotatedNode == null)
                    {
                        // All nodesToSimplify have been processed.
                        // Remove all the custom annotations added for tracking nodesToSimplify.
                        root = root.ReplaceNodes(annotatedNodes, (o, n) => o.WithoutAnnotations(annotation));
                        break;
                    }

                    document = await AddSimplifyAnnotationsAsync(document, annotatedNode, cancellationToken).ConfigureAwait(false);
                }
            }

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
