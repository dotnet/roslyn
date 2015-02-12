// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
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

        public override async Task AddDocumentFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, FixAllContext fixAllContext)
        {
            var changedDocument = await AddSimplifierAnnotationsAsync(document, diagnostics, fixAllContext).ConfigureAwait(false);
            var title = GetFixAllTitle(fixAllContext);
            var codeAction = new MyCodeAction(title, (c) => Task.FromResult(changedDocument));
            addFix(codeAction);
        }

        /// <summary>
        /// Get node on which to add simplifier and formatter annotation for fixing the given diagnostic.
        /// </summary>
        protected virtual SyntaxNode GetNodeToSimplify(SyntaxNode root, SemanticModel model, Diagnostic diagnostic, Workspace workspace, out string codeActionEquivalenceKey, CancellationToken cancellationToken)
        {
            codeActionEquivalenceKey = null;
            var span = diagnostic.Location.SourceSpan;
            return root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true);
        }

        protected virtual Task<Document> AddSimplifyAnnotationsAsync(Document document, SyntaxNode nodeToSimplify, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual bool NeedsParentFixup { get { return false; } }

        private async Task<Document> AddSimplifierAnnotationsAsync(Document document, ImmutableArray<Diagnostic> diagnostics, FixAllContext fixAllContext)
        {
            var cancellationToken = fixAllContext.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Find all nodes to simplify corresponding to diagnostic spans.
            var nodesToSimplify = new List<SyntaxNode>();
            foreach (var diagnostic in diagnostics)
            {
                string codeActionEquivalenceKey;
                var node = GetNodeToSimplify(root, model, diagnostic, fixAllContext.Solution.Workspace, out codeActionEquivalenceKey, cancellationToken);
                if (node != null && fixAllContext.CodeActionEquivalenceKey == codeActionEquivalenceKey)
                {
                    nodesToSimplify.Add(node);
                }
            }

            // Add simplifier and formatter annotations to all nodes to simplify.
            if (!NeedsParentFixup)
            {
                root = root.ReplaceNodes(nodesToSimplify, (o, n) =>
                    n.WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation));
            }
            else
            {
                var annotation = new SyntaxAnnotation();
                root = root.ReplaceNodes(nodesToSimplify, (o, n) =>
                    n.WithAdditionalAnnotations(annotation));
                document = document.WithSyntaxRoot(root);

                while (true)
                {
                    var annotatedNode = root.GetAnnotatedNodes(annotation).FirstOrDefault(n => !n.HasAnnotation(Simplifier.Annotation));
                    if (annotatedNode == null)
                    {
                        break;
                    }

                    document = await AddSimplifyAnnotationsAsync(document, annotatedNode, cancellationToken).ConfigureAwait(false);
                    root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                }

                root = root.WithoutAnnotations(annotation);
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
