// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AvoidUnusedMembers
{
    internal abstract class AbstractAvoidUnusedMembersCodeFixProvider<TFieldDeclarationSyntax> : SyntaxEditorBasedCodeFixProvider
        where TFieldDeclarationSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.AvoidUnusedMembersDiagnosticId);

        // Adjust declarators to remove based on whether or not all variable declarators within a field declaration should be removed.
        protected abstract void AdjustDeclarators(HashSet<TFieldDeclarationSyntax> fieldDeclarators, HashSet<SyntaxNode> declarators);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            return FixWithEditorAsync(document, editor, diagnostics, cancellationToken);
        }

        private async Task FixWithEditorAsync(
            Document document, SyntaxEditor editor, ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var declarators = new HashSet<SyntaxNode>();
            var fieldDeclarators = new HashSet<TFieldDeclarationSyntax>();

            // Compute declarators to remove, and also track common field declarators.
            foreach (var diagnostic in diagnostics)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var symbolName = diagnostic.Properties[AvoidUnusedMembersDiagnosticAnalyzer.UnunsedMemberNameProperty];
                var symbolKind = diagnostic.Properties[AvoidUnusedMembersDiagnosticAnalyzer.UnunsedMemberKindProperty];

                var node = GetTopmostSyntaxNodeForSymbolDeclaration(root.FindNode(diagnosticSpan),
                    isSymbolDeclarationNode: n => n != null && semanticModel.GetDeclaredSymbol(n, cancellationToken)?.Name == symbolName);

                declarators.Add(node);
                if (symbolKind == nameof(SymbolKind.Field))
                {
                    var fieldDeclarator = node.FirstAncestorOrSelf<TFieldDeclarationSyntax>();
                    fieldDeclarators.Add(fieldDeclarator);
                }
            }

            if (fieldDeclarators.Count > 0)
            {
                AdjustDeclarators(fieldDeclarators, declarators);
            }

            foreach (var declarator in declarators)
            {
                editor.RemoveNode(declarator);
            }
        }

        protected virtual SyntaxNode GetTopmostSyntaxNodeForSymbolDeclaration(SyntaxNode syntaxNode, Func<SyntaxNode, bool> isSymbolDeclarationNode)
        {
            return syntaxNode.FirstAncestorOrSelf(isSymbolDeclarationNode);
        }

        protected void AdjustChildDeclarators(SyntaxNode parentDeclaration, IEnumerable<SyntaxNode> childDeclarators, HashSet<SyntaxNode> declarators)
        {
            Debug.Assert(!declarators.Contains(parentDeclaration));

            var declaratorsContainsAllChildren = true;
            foreach (var childDeclarator in childDeclarators)
            {
                if (!declarators.Contains(childDeclarator))
                {
                    declaratorsContainsAllChildren = false;
                    break;
                }
            }

            if (declaratorsContainsAllChildren)
            {
                // Remove the entire parent declaration instead of individual child declarators within it.
                declarators.Add(parentDeclaration);
                declarators.RemoveAll(childDeclarators);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Remove_unused_member, createChangedDocument)
            {
            }
        }
    }
}
