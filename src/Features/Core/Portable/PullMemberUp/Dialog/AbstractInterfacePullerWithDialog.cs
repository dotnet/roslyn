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
                var targetDocument = contextDocument.Project.Solution.GetDocumentId(targetSyntaxNode.SyntaxTree);
                var solutionEditor = new SolutionEditor(contextDocument.Project.Solution);
                var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(
                    contextDocument.Project.Solution.GetDocumentId(targetSyntaxNode.SyntaxTree));

                AddMembersToTarget(result, targetDocumentEditor, targetSyntaxNode, codeGenerationService);
                await ChangeMembersToPublicAndNonStatic(result, contextDocument, solutionEditor, codeGenerationService, cancellationToken);

                return solutionEditor.GetChangedSolution();
            }
            else
            {
                return default;
            }
        }

        private async Task ChangeMembersToPublicAndNonStatic(
            PullMemberDialogResult result,
            Document contextDocument,
            SolutionEditor solutionEditor,
            ICodeGenerationService codeGenerationService,
            CancellationToken cancellationToken)
        {
            await ChangeMembers(
                result,
                solutionEditor,
                contextDocument,
                selectionPair => selectionPair.member.IsStatic || selectionPair.member.DeclaredAccessibility != Accessibility.Public,
                (syntax, symbol, containingTypeNode, editor) =>
                {
                    ChangeMemberToPublicAndNonStatic(editor, symbol, syntax, containingTypeNode, codeGenerationService);
                },
                cancellationToken);
        }

        private IMethodSymbol CreatePublicGetterAndSetter(IMethodSymbol setterOrGetter, IPropertySymbol containingProperty)
        {
            if (setterOrGetter == null || setterOrGetter.DeclaredAccessibility == Accessibility.Public)
            {
                return setterOrGetter;
            }

            if (containingProperty.DeclaredAccessibility == Accessibility.Public)
            {
                return setterOrGetter.DeclaredAccessibility == Accessibility.Public ? setterOrGetter : null;
            }

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
                                CreatePublicGetterAndSetter(propertySymbol.GetMethod, propertySymbol),
                                CreatePublicGetterAndSetter(propertySymbol.SetMethod, propertySymbol));
                    }
                    else
                    {
                        return selectionPair.member;
                    }
                });

            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            editor.ReplaceNode(targetNode, codeGenerationService.AddMembers(targetNode, symbolsToPullUp, options: options));
        }

        protected abstract void ChangeMemberToPublicAndNonStatic(DocumentEditor editor, ISymbol symbol, SyntaxNode node, SyntaxNode containingTypeNode, ICodeGenerationService codeGenerationService);
    }
}
