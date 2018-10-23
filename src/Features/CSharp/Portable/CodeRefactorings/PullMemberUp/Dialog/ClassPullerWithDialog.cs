// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
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
    internal class ClassPullerWithDialog
    {
        private void AddMembersToTarget(
            PullMemberDialogResult result,
            DocumentEditor editor,
            SyntaxNode targetNodeSyntax,
            ICodeGenerationService codeGenerationService,
            CancellationToken cancellationToken)
        {
            var pullUpMemberSymbols = result.SelectedMembers.Select(userSelection => userSelection.member);
            if (result.Target.IsAbstract)
            {
                var abstractMembersSymbol = result.SelectedMembers.Where(selectionPair => selectionPair.makeAbstract).
                    Select(selectionPair => GetAbstractMemberSymbol(selectionPair.member));

                pullUpMemberSymbols = result.SelectedMembers.Where(selectionPair => !selectionPair.makeAbstract).
                    Select(selection => selection.member).Concat(abstractMembersSymbol);
            }

            var options = new CodeGenerationOptions(reuseSyntax: true);
            var membersAddedTarget = codeGenerationService.AddMembers(targetNodeSyntax, pullUpMemberSymbols, options: options, cancellationToken);
            editor.ReplaceNode(targetNodeSyntax, membersAddedTarget);
        }

        private void ChangeTargetType(
            PullMemberDialogResult result,
            SyntaxNode targetSyntaxNode,
            DocumentEditor editor)
        {
            // If try to pull an abstract member to ordinary class
            // Change the class to abstract
            if (result.Target is INamedTypeSymbol target &&
                !target.IsAbstract &&
                target.TypeKind == TypeKind.Class)
            {
                var changeTargetToAbstract = result.SelectedMembers.Aggregate(false,
                    (acc, selectionPair) => acc || selectionPair.makeAbstract || selectionPair.member.IsAbstract);

                if (changeTargetToAbstract)
                {
                    // TODO: if there are multiple partial classes, should change them 
                    // all?
                    editor.SetModifiers(targetSyntaxNode,
                        DeclarationModifiers.From(result.Target).WithIsAbstract(true));
                }
            }
        }

        private async Task ChangeContainingType(
            PullMemberDialogResult result,
            SyntaxNode targetSyntaxNode,
            DocumentEditor editor,
            CancellationToken cancellationToken)
        {
            // If user want to make it abstract then don't remove the original member
            var tasks = result.SelectedMembers.Where(selectionPair => !selectionPair.makeAbstract).
                Select(async selectionPair => (memberSyntax: (await selectionPair.member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntaxAsync(cancellationToken)), memberSymbol: selectionPair.member));
            var membersToRemove = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach ((var syntax, var symbol) in membersToRemove)
            {
                if (syntax != null)
                {
                    if (symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Event)
                    {
                        if (syntax.Parent != null &&
                            syntax.Parent.Parent is BaseFieldDeclarationSyntax fieldOrEventSyntaxDeclaration)
                        {
                            if (fieldOrEventSyntaxDeclaration.Declaration.Variables.Count() == 1)
                            {
                                editor.RemoveNode(fieldOrEventSyntaxDeclaration);
                            }
                            else
                            {
                                editor.RemoveNode(syntax);
                            }
                        }
                    }
                }
                else
                {
                    editor.RemoveNode(syntax);
                }
            }
        }

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
                var contextEditor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Id, cancellationToken);
                var targetEditor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Project.Solution.GetDocumentId(targetSyntaxNode.SyntaxTree));

                await ChangeContainingType(result, targetSyntaxNode, contextEditor, cancellationToken);

                AddMembersToTarget(result, targetEditor, targetSyntaxNode, codeGenerationService, cancellationToken);

                ChangeTargetType(result, targetSyntaxNode, targetEditor);

                return solutionEditor.GetChangedSolution();
            }
            else
            {
                return default;
            }
        }

        private ISymbol GetAbstractMemberSymbol(ISymbol memberSymbol)
        {
            if (memberSymbol.IsAbstract)
            {
                return memberSymbol;
            }
            var modifier = DeclarationModifiers.From(memberSymbol).WithIsAbstract(true);
            if (memberSymbol is IMethodSymbol methodSymbol)
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, modifiers: modifier);
            }
            else if (memberSymbol is IPropertySymbol propertySymbol)
            {
                return CodeGenerationSymbolFactory.CreatePropertySymbol(propertySymbol, modifiers: modifier);
            }
            else if (memberSymbol is IEventSymbol eventSymbol)
            {
                return CodeGenerationSymbolFactory.CreateEventSymbol(eventSymbol, modifiers: modifier);
            }
            else if (memberSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                return CodeGenerationSymbolFactory.
                    CreateNamedTypeSymbol(namedTypeSymbol.GetAttributes(), namedTypeSymbol.DeclaredAccessibility, modifier, namedTypeSymbol.TypeKind, namedTypeSymbol.Name, namedTypeSymbol.TypeParameters, namedTypeSymbol.BaseType, namedTypeSymbol.Interfaces, namedTypeSymbol.SpecialType, namedTypeSymbol.GetMembers());
            }
            else
            {
                throw new ArgumentException($"{nameof(memberSymbol)} should be method, property, event, indexer or class");
            }
        }
    }
}
