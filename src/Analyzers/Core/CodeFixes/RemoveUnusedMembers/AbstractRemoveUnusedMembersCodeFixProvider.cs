// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedMembers
{
    internal abstract class AbstractRemoveUnusedMembersCodeFixProvider<TFieldDeclarationSyntax> : SyntaxEditorBasedCodeFixProvider
        where TFieldDeclarationSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId);

        /// <summary>
        /// This method adjusts the <paramref name="declarators"/> to remove based on whether or not all variable declarators
        /// within a field declaration should be removed,
        /// i.e. if all the fields declared within a field declaration are unused,
        /// we can remove the entire field declaration instead of individual variable declarators.
        /// </summary>
        protected abstract void AdjustAndAddAppropriateDeclaratorsToRemove(HashSet<TFieldDeclarationSyntax> fieldDeclarators, HashSet<SyntaxNode> declarators);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, AnalyzersResources.Remove_unused_member, nameof(AnalyzersResources.Remove_unused_member));
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var declarators = new HashSet<SyntaxNode>();
            var fieldDeclarators = new HashSet<TFieldDeclarationSyntax>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declarationService = document.GetRequiredLanguageService<ISymbolDeclarationService>();

            // Compute declarators to remove, and also track common field declarators.
            foreach (var diagnostic in diagnostics)
            {
                // Get symbol to be removed.
                var diagnosticNode = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                var symbol = semanticModel.GetDeclaredSymbol(diagnosticNode, cancellationToken);
                Contract.ThrowIfNull(symbol);

                // Get symbol declarations to be removed.
                foreach (var declReference in declarationService.GetDeclarations(symbol))
                {
                    var node = await declReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    declarators.Add(node);

                    // For fields, the declaration node is the variable declarator.
                    // We also track the ancestor FieldDeclarationSyntax which may declare more then one field.
                    if (symbol.Kind == SymbolKind.Field)
                    {
                        var fieldDeclarator = node.FirstAncestorOrSelf<TFieldDeclarationSyntax>();
                        Contract.ThrowIfNull(fieldDeclarator);
                        fieldDeclarators.Add(fieldDeclarator);
                    }
                }
            }

            // If all the fields declared within a field declaration are unused,
            // we can remove the entire field declaration instead of individual variable declarators.
            if (fieldDeclarators.Count > 0)
            {
                AdjustAndAddAppropriateDeclaratorsToRemove(fieldDeclarators, declarators);
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
        protected static void AdjustAndAddAppropriateDeclaratorsToRemove(SyntaxNode parentDeclaration, IEnumerable<SyntaxNode> childDeclarators, HashSet<SyntaxNode> declarators)
        {
            if (declarators.Contains(parentDeclaration))
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
    }
}
