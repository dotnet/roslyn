// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.RemoveUnusedVariable
{
    internal abstract class AbstractRemoveUnusedVariableCodeFixProvider<TLocalDeclarationStatement, TVariableDeclarator, TVariableDeclaration> : SyntaxEditorBasedCodeFixProvider
        where TLocalDeclarationStatement: SyntaxNode
        where TVariableDeclarator: SyntaxNode
        where TVariableDeclaration: SyntaxNode
    {
        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var ancestor = token.GetAncestor<TLocalDeclarationStatement>();

                if (ancestor == null)
                {
                    return;
                }
            }

            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var variableDeclarator = token.GetAncestor<TVariableDeclarator>();

                var variableDeclarators = token.GetAncestor<TVariableDeclaration>().ChildNodes().Where(x => x is TVariableDeclarator);

                if (variableDeclarators.Count() == 1)
                {
                    editor.RemoveNode(token.GetAncestor<TLocalDeclarationStatement>());
                }
                else if (variableDeclarators.Count() > 1)
                {
                    editor.RemoveNode(variableDeclarator);
                }
            }
            return SpecializedTasks.EmptyTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Remove_unused_variable, createChangedDocument, FeaturesResources.Remove_unused_variable)
            {
            }
        }
    }
}
