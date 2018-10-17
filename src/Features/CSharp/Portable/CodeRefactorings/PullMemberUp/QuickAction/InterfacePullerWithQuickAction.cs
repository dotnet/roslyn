// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeGeneration;
using System;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal class InterfacePullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        internal InterfacePullerWithQuickAction(
            INamedTypeSymbol targetInterfaceSymbol,
            CodeRefactoringContext context,
            SemanticModel semanticModel,
            SyntaxNode userSelectedNode):
            base(targetInterfaceSymbol, context, semanticModel, userSelectedNode)
        {
        }

        internal override bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMembers)
        {
            var validator = new InterfaceModifiersValidator();
            return validator.AreModifiersValid(targetSymbol, new ISymbol[] { selectedMembers });
        }

        internal override CodeAction ComputeRefactoring()
        {
            if (IsDeclarationAlreadyInTarget())
            {
                return default;
            }
            return base.ComputeRefactoring();
        }

        protected override CodeAction CreateSolutionChangeAction(SyntaxNode nodeToPullUp, Document contextDocument)
        {
            var changedSolution = ChangedSolutionAndDocumentCreator.
                AddMembersToSolutionAsync(new SyntaxNode[] { nodeToPullUp }, TargetSyntaxNode,
                contextDocument, CancellationToken);
            return new SolutionChangeAction(Title, _ => changedSolution);
        }

        protected override CodeAction CreateDocumentChangeAction(SyntaxNode nodeToPullUp, Document contextDocument)
        {
            var changedDocument = ChangedSolutionAndDocumentCreator.
                AddMembersToDocumentAsync(new SyntaxNode[] { nodeToPullUp }, TargetSyntaxNode,
                contextDocument, CancellationToken);
            return new DocumentChangeAction(Title, _ => changedDocument);
        }

        private bool IsDeclarationAlreadyInTarget()
        {
            var allMembers = TargetTypeSymbol.GetMembers();

            foreach (var member in allMembers)
            {
                var implementationOfMember = UserSelectedNodeSymbol.ContainingType.FindImplementationForInterfaceMember(member);
                if (UserSelectedNodeSymbol.OriginalDefinition.Equals(implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }
            return false;
        }

        internal override CodeAction CreateAction(IMethodSymbol methodSymbol, Document contextDocument)
        {
            // Maybe change it to async ???
            var targetNode = CodeGenerationService.AddMethod(TargetSyntaxNode, methodSymbol);
            return CreateDocumentOrSolutionChangedAction(targetNode, contextDocument);
        }

        internal override CodeAction CreateAction(IPropertySymbol propertyOrIndexerNode, Document contextDocument)
        {
            var option = new CodeGenerationOptions(generateDefaultAccessibility: false);
            var targetNode = CodeGenerationService.AddProperty(TargetSyntaxNode, propertyOrIndexerNode, option);
            return CreateDocumentOrSolutionChangedAction(targetNode, contextDocument);
        }

        internal override CodeAction CreateAction(IEventSymbol eventSymbol, Document contextDocument)
        {
            var option = new CodeGenerationOptions(generateMembers: false, generateMethodBodies: false);
            var targetNode = CodeGenerationService.AddEvent(TargetSyntaxNode, eventSymbol, option);
            return CreateDocumentOrSolutionChangedAction(targetNode, contextDocument);
        }

        internal override CodeAction CreateAction(IFieldSymbol fieldSymbol, Document contextDocument)
        {
            throw new NotImplementedException("Can't pull a field up to interface");
        }
    }
}
