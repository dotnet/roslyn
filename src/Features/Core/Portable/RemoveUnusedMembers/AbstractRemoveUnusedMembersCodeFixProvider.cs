// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedMembers
{
    internal abstract class AbstractRemoveUnusedMembersCodeFixProvider<TFieldDeclarationSyntax> : SyntaxEditorBasedCodeFixProvider
        where TFieldDeclarationSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId);

        // Adjust declarators to remove based on whether or not all variable declarators within a field declaration should be removed.
        protected abstract void AdjustDeclarators(HashSet<TFieldDeclarationSyntax> fieldDeclarators, HashSet<SyntaxNode> declarators);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            var declarators = new HashSet<SyntaxNode>();
            var fieldDeclarators = new HashSet<TFieldDeclarationSyntax>();

            // Compute declarators to remove, and also track common field declarators.
            foreach (var diagnostic in diagnostics)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // Get symbol to be removed.
                var diagnosticNode = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                var symbol = semanticModel.GetDeclaredSymbol(diagnosticNode, cancellationToken);
                Debug.Assert(symbol != null);

                // Get symbol declarations to be removed.
                var declarationService = document.GetLanguageService<ISymbolDeclarationService>();
                foreach (var declReference in declarationService.GetDeclarations(symbol))
                {
                    var node = await declReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    declarators.Add(node);

                    // For fields, the declaration node is the variable declarator.
                    // We also track the ancestor FieldDeclarationSyntax which may declare more then one field.
                    if (symbol.Kind == SymbolKind.Field)
                    {
                        var fieldDeclarator = node.FirstAncestorOrSelf<TFieldDeclarationSyntax>();
                        fieldDeclarators.Add(fieldDeclarator);
                    }
                }
            }

            // If all the fields declared within a field declaration are unused,
            // we can remove the entire field declaration instead of individual variable declarators.
            if (fieldDeclarators.Count > 0)
            {
                AdjustDeclarators(fieldDeclarators, declarators);
            }

            // Remove all the symbol declarator nodes.
            foreach (var declarator in declarators)
            {
                editor.RemoveNode(declarator);
            }
        }

        /// <summary>
        /// If all the <paramref name="childDeclarators"/> are contained in <paramref name="declarators"/>,
        /// the removes the <paramref name="childDeclarators"/> from <paramref name="declarators"/>, and
        /// adds the <paramref name="parentDeclaration"/> to the <paramref name="declarators"/>.
        /// </summary>
        protected void AdjustChildDeclarators(SyntaxNode parentDeclaration, IEnumerable<SyntaxNode> childDeclarators, HashSet<SyntaxNode> declarators)
        {
            if(declarators.Contains(parentDeclaration))
            {
                Debug.Assert(childDeclarators.All(c => !declarators.Contains(c)));
                return;
            }

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
