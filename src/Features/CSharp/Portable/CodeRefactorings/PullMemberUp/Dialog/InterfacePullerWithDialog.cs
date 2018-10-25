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
    internal class InterfacePullerWithDialog : AbstractMemberPullerWithDialog
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
                                editor.AddMember(containingTypeNode, publicSyntax);
                            }
                        }
                    }
                    else
                    {
                        editor.SetAccessibility(syntax, Accessibility.Public);
                    }
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
                    var modifier = DeclarationModifiers.From(symbol).WithIsStatic(false);
                    var editor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Project.Solution.GetDocumentId(containingTypeNode.SyntaxTree));
                    if (symbol is IEventSymbol eventSymbol)
                    {
                        if (syntax.Parent != null &&
                            syntax.Parent.Parent is BaseFieldDeclarationSyntax eventDeclaration)
                        {
                            if (eventDeclaration.Declaration.Variables.Count == 1)
                            {
                                editor.SetModifiers(eventDeclaration, modifier);
                            }
                            else if (eventDeclaration.Declaration.Variables.Count > 1)
                            {
                                var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
                                var nonStaticSymbol = CodeGenerationSymbolFactory.CreateEventSymbol(
                                    eventSymbol.GetAttributes(),
                                    eventSymbol.DeclaredAccessibility,
                                    modifier,
                                    eventSymbol.Type,
                                    eventSymbol.ExplicitInterfaceImplementations,
                                    eventSymbol.Name,
                                    eventSymbol.AddMethod,
                                    eventSymbol.RemoveMethod,
                                    eventSymbol.RaiseMethod);
                                var nonStaticSyntax = codeGenerationService.CreateEventDeclaration(nonStaticSymbol, options: options);
                                editor.RemoveNode(syntax);
                                editor.AddMember(containingTypeNode, nonStaticSyntax);
                            }
                        }
                    }
                    else
                    {
                        editor.SetModifiers(syntax, modifier);
                    }
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
                   methodKind: setterOrGetter.MethodKind == MethodKind.PropertyGet ?
                               MethodKind.PropertyGet : MethodKind.PropertySet);
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
    }
}
