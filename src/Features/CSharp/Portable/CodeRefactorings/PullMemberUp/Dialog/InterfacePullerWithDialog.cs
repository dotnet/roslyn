// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.Dialog
{
    internal class InterfacePullerWithDialog
    {
        internal async Task<Solution> ComputeChangedSolution(
            PullMemberDialogResult result,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var targetSyntaxNode = await codeGenerationService.
                FindMostRelevantNameSpaceOrTypeDeclarationAsync(contextDocument.Project.Solution, result.Target);

            if (targetSyntaxNode != null)
            {
                var solutionEditor = new SolutionEditor(contextDocument.Project.Solution);
                var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(
                   contextDocument.Project.Solution.GetDocumentId(targetSyntaxNode.SyntaxTree));
                var contextDocumentEditor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Id);

                var membersAddedTargetNode = AddMembersToTarget(result, contextDocument, targetSyntaxNode, codeGenerationService);
                targetDocumentEditor.ReplaceNode(targetSyntaxNode, membersAddedTargetNode);
                await ChangeAccessibilityToPublic(result, contextDocumentEditor, targetSyntaxNode, codeGenerationService, cancellationToken);

                return solutionEditor.GetChangedSolution();
            }
            else
            {
                return default;
            }
        }

        private async Task ChangeAccessibilityToPublic(
            PullMemberDialogResult result,
            DocumentEditor editor,
            SyntaxNode targetNode,
            ICodeGenerationService codeGenerationService,
            CancellationToken cancellationToken)
        {
            var nonPublicMembers = result.SelectedMembers.
                Where(selectionPair => selectionPair.member.DeclaredAccessibility != Accessibility.Public).
                Select(selectionPair => selectionPair.member);

            var tasks = nonPublicMembers.
                Select(async symbol => (memberSyntax: (await symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntaxAsync(cancellationToken)), memberSymbol: symbol));
            var syntaxAndSymbolPairs = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach ((var syntax, var symbol) in syntaxAndSymbolPairs)
            {
                if (syntax != null)
                {
                    if (symbol is IEventSymbol eventSymbol)
                    {
                        if (syntax.Parent != null &&
                            syntax.Parent.Parent is BaseFieldDeclarationSyntax eventDeclaration)
                        {
                            if (eventDeclaration.Declaration.Variables.Count == 1)
                            {
                                editor.SetAccessibility(eventDeclaration, Accessibility.Public);
                            }
                            else if (eventDeclaration.Declaration.Variables.Count > 1)
                            {
                                // If multiple declaration on same line
                                // e.g. private EventHandler event Event1, Event2, Event3
                                // change Event1 to public need to create a new declaration
                                var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
                                var publicSyntax = codeGenerationService.CreateEventDeclaration(eventSymbol, CodeGenerationDestination.InterfaceType, options);

                                editor.RemoveNode(syntax);
                                editor.AddMember(targetNode, publicSyntax);

                                // If all of them are removed, how to remove the type and modifiers
                            }
                        }
                    }
                    else
                    {
                        editor.SetAccessibility(syntax, Accessibility.Public);
                    }
                }
            }
        }

        private SyntaxNode AddMembersToTarget(
            PullMemberDialogResult result,
            Document contextDocument,
            SyntaxNode targetNode,
            ICodeGenerationService codeGenerationService)
        {
            var symbolsToPullUp = result.SelectedMembers.
                Select(selectionPair =>
                {
                    if (selectionPair.member is IPropertySymbol propertySymbol)
                    {
                        return CodeGenerationSymbolFactory.CreatePropertySymbol(
                                propertySymbol,
                                propertySymbol.GetAttributes(),
                                Accessibility.Public,
                                DeclarationModifiers.From(propertySymbol),
                                propertySymbol.ExplicitInterfaceImplementations,
                                propertySymbol.Name,
                                propertySymbol.IsIndexer,
                                propertySymbol.GetMethod.DeclaredAccessibility == Accessibility.Public ? propertySymbol.GetMethod : null,
                                propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Public ? propertySymbol.SetMethod : null);
                    }
                    else
                    {
                        return selectionPair.member;
                    }
                });

            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);

            return codeGenerationService.AddMembers(targetNode, symbolsToPullUp, options:options);
        }
    }
}
