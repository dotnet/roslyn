// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMembrUp.Dialog
{
    internal class ClassPullerWithDialog : AbstractMemberPullerWithDialog
    {
        internal ClassPullerWithDialog(Document document) : base(document)
        {
        }

        internal async Task<Solution> ComputeChangedSolution(
            PullMemberDialogResult result,
            CancellationToken cancellationToken)
        {
            var targetSyntaxNode = await CodeGenerationService.
                FindMostRelevantNameSpaceOrTypeDeclarationAsync(ContextDocument.Project.Solution, result.Target);

            if (targetSyntaxNode != null)
            {
                var solutionEditor = new SolutionEditor(ContextDocument.Project.Solution);
                var targetEditor = await solutionEditor.GetDocumentEditorAsync(ContextDocument.Project.Solution.GetDocumentId(targetSyntaxNode.SyntaxTree));

                await RemoveMembers(result, ContextDocument, solutionEditor, cancellationToken);
                var changedTarget = ChangeTargetType(result, targetSyntaxNode, CodeGenerationService);
                AddMembersToTarget(result, targetEditor, targetSyntaxNode, changedTarget, CodeGenerationService, cancellationToken);
                return solutionEditor.GetChangedSolution();
            }
            else
            {
                return default;
            }
        }

        private SyntaxNode ChangeTargetType(
            PullMemberDialogResult result,
            SyntaxNode targetSyntaxNode,
            ICodeGenerationService codeGenerationService)
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
                    // TODO: should we make all classes abstract? Or just this node?
                    var options = new CodeGenerationOptions(reuseSyntax: true);
                    return codeGenerationService.CreateNamedTypeDeclaration(GetAbstractMemberSymbol(result.Target) as INamedTypeSymbol, options: options);
                }
            }
            return targetSyntaxNode;
        }

        private void AddMembersToTarget(
            PullMemberDialogResult result,
            DocumentEditor editor,
            SyntaxNode originTarget,
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

            var options = new CodeGenerationOptions(generateMembers: false, generateMethodBodies: false, reuseSyntax: true);
            var membersAddedTarget = codeGenerationService.AddMembers(targetNodeSyntax, pullUpMemberSymbols, options: options, cancellationToken);
            editor.ReplaceNode(originTarget, membersAddedTarget);
        }

        private async Task RemoveMembers(
            PullMemberDialogResult result,
            Document contextDocument,
            SolutionEditor solutionEditor,
            CancellationToken cancellationToken)
        {
            // If user want to make it abstract then don't remove the original member
            await ChangeMembers(
                result,
                selectionPair => !selectionPair.makeAbstract,
                async (syntax, symbol, containingTypeNode) =>
                {
                    var editor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Project.Solution.GetDocumentId(containingTypeNode.SyntaxTree));

                    ChangeService.RemoveNode(editor, syntax, symbol);
                },
                cancellationToken);
        }

        private ISymbol GetAbstractMemberSymbol(ISymbol symbol)
        {
            if (symbol.IsAbstract)
            {
                return symbol;
            }
            var modifier = DeclarationModifiers.From(symbol).WithIsAbstract(true);
            if (symbol is IMethodSymbol methodSymbol)
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, modifiers: modifier);
            }
            else if (symbol is IPropertySymbol propertySymbol)
            {
                return CodeGenerationSymbolFactory.CreatePropertySymbol(propertySymbol, modifiers: modifier);
            }
            else if (symbol is IEventSymbol eventSymbol)
            {
                return CodeGenerationSymbolFactory.CreateEventSymbol(eventSymbol, modifiers: modifier);
            }
            else if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                return CodeGenerationSymbolFactory.
                    CreateNamedTypeSymbol(namedTypeSymbol.GetAttributes(), namedTypeSymbol.DeclaredAccessibility, modifier, namedTypeSymbol.TypeKind, namedTypeSymbol.Name, namedTypeSymbol.TypeParameters, namedTypeSymbol.BaseType, namedTypeSymbol.Interfaces, namedTypeSymbol.SpecialType, namedTypeSymbol.GetMembers());
            }
            else
            {
                throw new ArgumentException($"{nameof(symbol)} should be method, property, event, indexer or class");
            }
        }
    }
}
