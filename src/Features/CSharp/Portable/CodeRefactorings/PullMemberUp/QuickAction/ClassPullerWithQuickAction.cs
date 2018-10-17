// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal class ClassPullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        internal ClassPullerWithQuickAction(
            INamedTypeSymbol targetClassSymbol,
            CodeRefactoringContext context,
            SemanticModel semanticModel,
            SyntaxNode userSelectedNode):
            base(targetClassSymbol, context, semanticModel, userSelectedNode)
        {
        }
        internal override bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            var validator = new ClassModifiersValidator();
            return validator.AreModifiersValid(targetSymbol, new ISymbol[] { selectedMember });
        }

        protected override CodeAction CreateDocumentChangeAction(SyntaxNode nodeToPullUp, Document contextDocument)
        {
            var changedDocument = ChangedSolutionAndDocumentCreator.MoveMemberToDocumentAsync(
                nodeToPullUp, TargetSyntaxNode,
                UserSelectedNode, contextDocument, CancellationToken);
            return new DocumentChangeAction(Title, _ => changedDocument);
        }

        protected override CodeAction CreateSolutionChangeAction(SyntaxNode nodeToPullUp, Document contextDocument)
        {
            var changedSolution = ChangedSolutionAndDocumentCreator.MoveMemberToSolutionAsync(
                nodeToPullUp, TargetSyntaxNode,
                UserSelectedNode, contextDocument, CancellationToken);
            return new SolutionChangeAction(Title, _ => changedSolution);
        }

        internal override CodeAction CreateAction(IMethodSymbol methodSymbol, Document contextDocument)
        {
            var targetNode = CodeGenerationService.AddMethod(TargetSyntaxNode, methodSymbol);
            return CreateDocumentOrSolutionChangedAction(targetNode, contextDocument);
        }

        internal override CodeAction CreateAction(IPropertySymbol propertyOrIndexerNode, Document contextDocument)
        {
            var targetNode = CodeGenerationService.AddProperty(TargetSyntaxNode, propertyOrIndexerNode);
            return CreateDocumentOrSolutionChangedAction(targetNode, contextDocument);
        }

        internal override CodeAction CreateAction(IEventSymbol eventSymbol, Document contextDocument)
        {
            var option = new CodeGenerationOptions();
            var targetNode = CodeGenerationService.AddEvent(TargetSyntaxNode, eventSymbol);
            return CreateDocumentOrSolutionChangedAction(targetNode, contextDocument);
        }

        internal override CodeAction CreateAction(IFieldSymbol fieldSyntax, Document contextDocument)
        {
            var option = new CodeGenerationOptions(generateMembers: true);
            var targetNode = CodeGenerationService.AddField(TargetSyntaxNode, fieldSyntax, option);
            return CreateDocumentOrSolutionChangedAction(targetNode, contextDocument);
        }
    }
}
