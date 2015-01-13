// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        protected virtual SyntaxNode GetNodeToSimplify(SyntaxNode root, SemanticModel model, Diagnostic diagnostic, Workspace workspace, out string codeActionId, CancellationToken cancellationToken)
        {
            codeActionId = null;
            var span = diagnostic.Location.SourceSpan;
            return root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true);
        }

        private async Task<Document> AddSimplifierAnnotationsAsync(Document document, ImmutableArray<Diagnostic> diagnostics, FixAllContext fixAllContext)
        {
            var cancellationToken = fixAllContext.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Find all nodes to simplify corresponding to diagnostic spans.
            var nodesToSimplify = new List<SyntaxNode>();
            foreach (var diagnostic in diagnostics)
            {
                string codeActionId;
                var node = GetNodeToSimplify(root, model, diagnostic, fixAllContext.Solution.Workspace, out codeActionId, cancellationToken);
                if (node != null && fixAllContext.CodeActionId == codeActionId)
                {
                    nodesToSimplify.Add(node);
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
