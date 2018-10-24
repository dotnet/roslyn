// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PullMemberUp.QuickAction;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal class ClassPullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        protected override bool IsDeclarationAlreadyInTarget()
        {
            if (UserSelectedNodeSymbol is IFieldSymbol fieldSymbol)
            {
                return TargetTypeSymbol.GetMembers().Any(member => member.Name == fieldSymbol.Name);
            }
            else
            {
                var overrideMethodSet = new HashSet<ISymbol>();
                if (UserSelectedNodeSymbol is IMethodSymbol methodSymbol)
                {
                    for (var symbol = methodSymbol.OverriddenMethod; symbol != null; symbol = symbol.OverriddenMethod)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else if (UserSelectedNodeSymbol is IPropertySymbol propertySymbol)
                {
                    for (var symbol = propertySymbol.OverriddenProperty; symbol != null; symbol = symbol.OverriddenProperty)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else if (UserSelectedNodeSymbol is IEventSymbol eventSymbol)
                {
                    for (var symbol = eventSymbol.OverriddenEvent; symbol != null; symbol = symbol.OverriddenEvent)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else
                {
                    throw new ArgumentException($"{UserSelectedNodeSymbol} should be method, property or event");
                }

                var membersInTargetClass =
                        TargetTypeSymbol.GetMembers().Where(member =>
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

        internal override bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            var validator = new ClassModifiersValidator();
            return validator.AreModifiersValid(targetSymbol, new ISymbol[] { selectedMember });
        }

        private async Task<CodeAction> CreateDocumentOrSolutionAction(ISymbol memberSymbol, Document contextDocument, CodeGenerationOptions options)
        {
            if (TargetTypeNode != null)
            {
                if (TargetTypeNode.SyntaxTree == UserSelectedNode.SyntaxTree)
                {
                    return await CreateDocumentChangedAction(memberSymbol, contextDocument, options);
                }
                else
                {
                    return await CreateSolutionChangedAction(memberSymbol, contextDocument, options);
                }
            }
            return default;
        }

        private async Task<CodeAction> CreateDocumentChangedAction(ISymbol memberSymbol, Document contextDocument, CodeGenerationOptions options)
        {
            var nodeWithMemberAdded = CodeGenerationService.AddMembers(TargetTypeNode, new ISymbol[] { memberSymbol }, options);
            var editor = await DocumentEditor.CreateAsync(contextDocument);
            editor.ReplaceNode(TargetTypeNode, nodeWithMemberAdded);
            RemoveNode(editor);
            return new DocumentChangeAction(Title, _ => Task.FromResult(editor.GetChangedDocument()));
        }

        private async Task<CodeAction> CreateSolutionChangedAction(ISymbol memberSymbol, Document contextDocument, CodeGenerationOptions options)
        {
            var nodeWithMemberAdded = CodeGenerationService.AddMembers(TargetTypeNode, new ISymbol[] { memberSymbol }, options);
            var solutionEditor = new SolutionEditor(contextDocument.Project.Solution);
            var targetDocument = contextDocument.Project.Solution.GetDocument(TargetTypeNode.SyntaxTree);

            var contextDocumentEditor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Id);
            var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(targetDocument.Id);

            targetDocumentEditor.ReplaceNode(TargetTypeNode, nodeWithMemberAdded);
            RemoveNode(contextDocumentEditor);

            return new SolutionChangeAction(Title, _ => Task.FromResult(solutionEditor.GetChangedSolution()));
        }

        internal override async Task<CodeAction> CreateAction(IMethodSymbol methodSymbol, Document contextDocument)
        {
            var options = new CodeGenerationOptions(reuseSyntax: true);
            return await CreateSolutionChangedAction(methodSymbol, contextDocument, options);
        }

        internal override async Task<CodeAction> CreateAction(IPropertySymbol propertyOrIndexerNode, Document contextDocument)
        {
            var options = new CodeGenerationOptions(reuseSyntax: true);
            return await CreateSolutionChangedAction(propertyOrIndexerNode, contextDocument, options);
        }

        internal override async Task<CodeAction> CreateAction(IEventSymbol eventSymbol, Document contextDocument)
        {
            var options = new CodeGenerationOptions(
                reuseSyntax: true,
                generateMembers:false,
                generateMethodBodies:false);
            return await CreateSolutionChangedAction(eventSymbol, contextDocument, options);
        }

        internal override async Task<CodeAction> CreateAction(IFieldSymbol fieldSymbol, Document contextDocument)
        {
            var options = new CodeGenerationOptions(reuseSyntax: true);
            return await CreateSolutionChangedAction(fieldSymbol, contextDocument, options);
        }

        private void RemoveNode(DocumentEditor editor)
        {
            if (UserSelectedNode is VariableDeclaratorSyntax variableDeclarator &&
                (UserSelectedNodeSymbol.Kind == SymbolKind.Field || UserSelectedNodeSymbol.Kind == SymbolKind.Event))
            {
                if (variableDeclarator.Parent != null &&
                    variableDeclarator.Parent.Parent is BaseFieldDeclarationSyntax fieldOrEventDeclaration)
                {
                    if (fieldOrEventDeclaration.Declaration.Variables.Count() == 1)
                    {
                        // If there is only one variable, e.g.
                        // public int i = 0;
                        // Just remove all
                        editor.RemoveNode(fieldOrEventDeclaration);
                    }
                    else
                    {
                        // If there are multiple variables, e.g.
                        // public int i, j = 0;
                        // Remove only one variable
                        editor.RemoveNode(variableDeclarator);
                    }
                }
            }
            else
            {
                editor.RemoveNode(UserSelectedNode);
            }
        }
    }
}
