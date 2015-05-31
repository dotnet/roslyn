// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    /// <summary>
    /// CA1821: Remove empty finalizers
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = RuleId), Shared]
    public sealed class RemoveEmptyFinalizersFixer : CodeFixProvider
    {
        public const string RuleId = "CA1821";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            if (node == null)
            {
                return;
            }

            // We cannot have multiple overlapping diagnostics of this id.
            var diagnostic = context.Diagnostics.Single();
            context.RegisterCodeFix(new MyCodeAction(FxCopFixersResources.RemoveEmptyFinalizers,
                             async ct => await RemoveFinalizer(context.Document, node, ct).ConfigureAwait(false)),
                        diagnostic);
            return;
        }

        private async Task<Document> RemoveFinalizer(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Get the declaration so that we step up to the methodblocksyntax and not the methodstatementsyntax for VB.
            node = editor.Generator.GetDeclaration(node);
            editor.RemoveNode(node);
            return editor.GetChangedDocument();
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
