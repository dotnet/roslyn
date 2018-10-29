// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal class ClassPullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        protected override bool IsDeclarationAlreadyInTarget(INamedTypeSymbol targetSymbol, ISymbol userSelectedNodeSymbol)
        {
            if (userSelectedNodeSymbol is IFieldSymbol fieldSymbol)
            {
                return targetSymbol.GetMembers().Any(member => member.Name == fieldSymbol.Name);
            }
            else
            {
                var overrideMethodSet = new HashSet<ISymbol>();
                if (userSelectedNodeSymbol is IMethodSymbol methodSymbol)
                {
                    for (var symbol = methodSymbol.OverriddenMethod; symbol != null; symbol = symbol.OverriddenMethod)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else if (userSelectedNodeSymbol is IPropertySymbol propertySymbol)
                {
                    for (var symbol = propertySymbol.OverriddenProperty; symbol != null; symbol = symbol.OverriddenProperty)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else if (userSelectedNodeSymbol is IEventSymbol eventSymbol)
                {
                    for (var symbol = eventSymbol.OverriddenEvent; symbol != null; symbol = symbol.OverriddenEvent)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else
                {
                    throw new ArgumentException($"{userSelectedNodeSymbol} should be method, property or event");
                }

                var membersInTargetClass =
                        targetSymbol.GetMembers().Where(member =>
                        {
                            if (member is IMethodSymbol method)
                            {
                                return method.MethodKind == MethodKind.Ordinary;
                            }
                            else if (member.Kind == SymbolKind.Field)
                            {
                                return !member.IsImplicitlyDeclared;
                            }
                            else
                            {
                                return true;
                            }
                        });
                return overrideMethodSet.Intersect(membersInTargetClass).Any();
            }
        }

        private async Task<CodeAction> CreateDocumentOrSolutionAction(
            ISymbol memberSymbol,
            SyntaxNode targetNode,
            CodeGenerationOptions options)
        {
            if (targetNode != null)
            {
                if (targetNode.SyntaxTree == targetNode.SyntaxTree)
                {
                    return await CreateDocumentChangedAction(memberSymbol, targetNode, options);
                }
                else
                {
                    return await CreateSolutionChangedAction(memberSymbol, targetNode, options);
                }
            }
            return default;
        }

        private async Task<CodeAction> CreateDocumentChangedAction(
            ISymbol memberSymbol,
            SyntaxNode targetNode,
            CodeGenerationOptions options)
        {
            var nodeWithMemberAdded = CodeGenerationService.AddMembers(targetNode, new ISymbol[] { memberSymbol }, options, _cancellationToken);
            var editor = await DocumentEditor.CreateAsync(ContextDocument, _cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(targetNode, nodeWithMemberAdded);
            RemoveService.RemoveNode(editor, UserSelectedNode, memberSymbol);
            return new DocumentChangeAction(Title, _ => Task.FromResult(editor.GetChangedDocument()));
        }

        private async Task<CodeAction> CreateSolutionChangedAction(
            ISymbol memberSymbol,
            SyntaxNode targetNode,
            CodeGenerationOptions options)
        {
            var nodeWithMemberAdded = CodeGenerationService.AddMembers(targetNode, new ISymbol[] { memberSymbol }, options, _cancellationToken);
            var solutionEditor = new SolutionEditor(ContextDocument.Project.Solution);
            var targetDocument = ContextDocument.Project.Solution.GetDocument(targetNode.SyntaxTree);

            var contextDocumentEditor = await solutionEditor.GetDocumentEditorAsync(ContextDocument.Id, _cancellationToken).ConfigureAwait(false);
            var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(targetDocument.Id, _cancellationToken).ConfigureAwait(false);

            targetDocumentEditor.ReplaceNode(targetNode, nodeWithMemberAdded);
            RemoveService.RemoveNode(contextDocumentEditor, UserSelectedNode, memberSymbol);

            return new SolutionChangeAction(Title, _ => Task.FromResult(solutionEditor.GetChangedSolution()));
        }

        internal override async Task<CodeAction> CreateAction(ISymbol member)
        {
            var options = new CodeGenerationOptions(reuseSyntax: true);

            if (member.Kind == SymbolKind.Event)
            {
                options = new CodeGenerationOptions(
                        reuseSyntax: true,
                        generateMembers:false,
                        generateMethodBodies:false);
            }
            return await CreateDocumentOrSolutionAction(member, TargetTypeNode, options);
        }

    }
}
