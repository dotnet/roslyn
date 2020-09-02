// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.RemoveRedundantEqualityWithTrue
{
    internal abstract class AbstractRemoveRedundantEqualityWithTrueCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.RemoveRedundantEqualityWithTrueDiagnosticId);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(new MyCodeAction(
                    AnalyzersResources.Remove_redundant_equality_with_true,
                    (ct) => FixAsync(context.Document, diagnostic, ct)),
                    diagnostic);
            }
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                if (TryGetReplacementNode(node, out var replacement))
                {
                    editor.ReplaceNode(node, replacement);
                }
            }
        }

        protected abstract bool TryGetReplacementNode(SyntaxNode node, out SyntaxNode replacement);

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
