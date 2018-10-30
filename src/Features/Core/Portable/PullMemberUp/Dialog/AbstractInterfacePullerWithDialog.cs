// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog
{
    internal abstract class AbstractInterfacePullerWithDialog : AbstractMemberPullerWithDialog, ILanguageService
    {
        internal async Task<Solution> ComputeChangedSolution(
            PullMemberDialogResult result,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = contextDocument.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            var targetSyntaxNode = await codeGenerationService.
                FindMostRelevantNameSpaceOrTypeDeclarationAsync(contextDocument.Project.Solution, result.Target);

            if (targetSyntaxNode != null )
            {
                var solutionEditor = new SolutionEditor(contextDocument.Project.Solution);
                var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(
                   contextDocument.Project.Solution.GetDocumentId(targetSyntaxNode.SyntaxTree));

                AddMembersToTarget(result, targetDocumentEditor, targetSyntaxNode, codeGenerationService);

                await ChangeMembersToPublic(result, contextDocument ,solutionEditor, codeGenerationService, cancellationToken);

                await ChangeMembersToNonStatic(result, contextDocument, solutionEditor, codeGenerationService, cancellationToken);

                return solutionEditor.GetChangedSolution();
            }
            else
            {
                return default;
            }
        }

        private async Task ChangeMembersToPublic(
            PullMemberDialogResult result,
            Document contextDocument,
            SolutionEditor solutionEditor,
            ICodeGenerationService codeGenerationService,
            CancellationToken cancellationToken)
        {
            await ChangeMembers(
                result,
                selectionPair => selectionPair.member.DeclaredAccessibility != Accessibility.Public,
                async (syntax, symbol, containingTypeNode) =>
                {
                    var editor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Project.Solution.GetDocumentId(containingTypeNode.SyntaxTree));
                    ChangeMemberToPublic(editor, symbol, syntax, containingTypeNode, codeGenerationService);
                },
                cancellationToken);
        }

        private async Task ChangeMembersToNonStatic(
            PullMemberDialogResult result,
            Document contextDocument,
            SolutionEditor solutionEditor,
            ICodeGenerationService codeGenerationService,
            CancellationToken cancellationToken)
        {
            await ChangeMembers(
                result,
                selectionPair => selectionPair.member.IsStatic,
                async (syntax, symbol, containingTypeNode) =>
                {
                    var editor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Project.Solution.GetDocumentId(containingTypeNode.SyntaxTree));
                    ChangeMemberToNonStatic(editor, symbol, syntax, containingTypeNode, codeGenerationService);
                },
                cancellationToken);
        }

        private IMethodSymbol CreatePublicGetterAndSetter(IMethodSymbol setterOrGetter)
        {
            if (setterOrGetter == null || setterOrGetter.DeclaredAccessibility == Accessibility.Public)
            {
                return setterOrGetter;
            }
            else
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                   setterOrGetter.GetAttributes(),
                   Accessibility.Public,
                   DeclarationModifiers.From(setterOrGetter),
                   setterOrGetter.ReturnType,
                   setterOrGetter.RefKind,
                   setterOrGetter.ExplicitInterfaceImplementations,
                   setterOrGetter.Name,
                   setterOrGetter.TypeParameters,
                   setterOrGetter.Parameters,
                   methodKind: setterOrGetter.MethodKind);
            }
        }

        private void AddMembersToTarget(
            PullMemberDialogResult result,
            DocumentEditor editor,
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
                                CreatePublicGetterAndSetter(propertySymbol.GetMethod),
                                CreatePublicGetterAndSetter(propertySymbol.SetMethod));
                    }
                    else
                    {
                        return selectionPair.member;
                    }
                });

            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            editor.ReplaceNode(targetNode, codeGenerationService.AddMembers(targetNode, symbolsToPullUp, options: options));
        }

        protected abstract void ChangeMemberToNonStatic(DocumentEditor editor, ISymbol symbol, SyntaxNode node, SyntaxNode containingTypeNode, ICodeGenerationService codeGenerationService);

        protected abstract void ChangeMemberToPublic(DocumentEditor editor, ISymbol symbol, SyntaxNode node, SyntaxNode containingTypeNode, ICodeGenerationService codeGenerationService);
    }
}
