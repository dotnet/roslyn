// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal abstract class AbstractMemberPusherWithQuickAction
    {
        protected string Title { get; }

        protected INamedTypeSymbol TargetTypeSymbol { get; }

        protected SyntaxNode TargetSyntaxNode { get; }

        protected SemanticModel SemanticModel { get; }

        protected SyntaxNode UserSelectedNode { get; }

        protected Document ContextDocument { get; }

        protected AbstractPushUpMemberSyntaxGenerator SyntaxGenerator { get; set; }
        
        internal AbstractMemberPusherWithQuickAction(
            INamedTypeSymbol targetTypeSymbol,
            SemanticModel semanticModel,
            SyntaxNode userSelectedNode,
            Document contextDocument)
        {
            Title = $"Add to {targetTypeSymbol.Name}";
            TargetTypeSymbol = targetTypeSymbol;
            TargetSyntaxNode = TargetTypeSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            SemanticModel = semanticModel;
            UserSelectedNode = userSelectedNode;
            ContextDocument = contextDocument;
        }

        protected virtual bool IsOrdinaryMethod(MethodDeclarationSyntax methodNode)
        {
            return SemanticModel.GetDeclaredSymbol(methodNode).MethodKind == MethodKind.Ordinary;
        }

        protected virtual BaseFieldDeclarationSyntax FindFieldAndEventDeclaration(SyntaxNode variableDeclaratorNode)
        {
            return variableDeclaratorNode.Ancestors().OfType<BaseFieldDeclarationSyntax>().First();
        }

        internal virtual CodeAction ComputeRefactoring()
        {
            if (UserSelectedNode is VariableDeclaratorSyntax selectVariableDeclaratorNode &&
                AreModifiersValid(TargetTypeSymbol, SemanticModel.GetDeclaredSymbol(UserSelectedNode)))
            {
                // When user select the identifier of event or field, the node will be VariableDeclaratorNode
                // which is different from selecting method or property
                var selectFieldOrEventNode = FindFieldAndEventDeclaration(selectVariableDeclaratorNode);
                return CreateAction(selectFieldOrEventNode, ContextDocument, selectVariableDeclaratorNode);
            }
            else if (UserSelectedNode is MethodDeclarationSyntax selectMethodNode &&
                    AreModifiersValid(TargetTypeSymbol, SemanticModel.GetDeclaredSymbol(UserSelectedNode)) &&
                    IsOrdinaryMethod(selectMethodNode))
            {
                return CreateAction(selectMethodNode, ContextDocument);
            }
            else if (UserSelectedNode is BasePropertyDeclarationSyntax selectPropertyOrIndexerNode &&
                    AreModifiersValid(TargetTypeSymbol, SemanticModel.GetDeclaredSymbol(UserSelectedNode)))
            {
                return CreateAction(selectPropertyOrIndexerNode, ContextDocument);
            }
            else
            {
                return default;
            }
        }

        internal virtual CodeAction CreateAction(
            BaseFieldDeclarationSyntax eventOrFieldSyntax,
            Document contextDocument,
            VariableDeclaratorSyntax userSelectedNode)
        {
            var nodeToPushUp = SyntaxGenerator.CreateMemberSyntax(eventOrFieldSyntax, userSelectedNode);
            return CreateDocumentOrSolutionChangedAction(eventOrFieldSyntax, nodeToPushUp, contextDocument);
        }

        internal virtual CodeAction CreateAction(MethodDeclarationSyntax methodSyntaxNode, Document contextDocument)
        {
            var nodeToPushUp = SyntaxGenerator.CreateMemberSyntax(methodSyntaxNode, null);
            return CreateDocumentOrSolutionChangedAction(methodSyntaxNode, nodeToPushUp, contextDocument);
        }
        internal virtual CodeAction CreateAction(BasePropertyDeclarationSyntax propertyOrIndexerNode, Document contextDocument)
        {
            var nodeToPushUp = SyntaxGenerator.CreateMemberSyntax(propertyOrIndexerNode, null);
            return CreateDocumentOrSolutionChangedAction(propertyOrIndexerNode, nodeToPushUp, contextDocument);
        }

        private CodeAction CreateDocumentOrSolutionChangedAction(
            MemberDeclarationSyntax selectedMemberNode,
            MemberDeclarationSyntax nodeToPushUp,
            Document contextDocument)
        {
            if (TargetSyntaxNode.SyntaxTree == selectedMemberNode.SyntaxTree)
            {
                return CreateDocumentChangeAction(nodeToPushUp, contextDocument);
            }
            else
            {
                return CreateSolutionChangeAction(nodeToPushUp, contextDocument);
            }
        }

        internal abstract bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMembers);

        protected abstract CodeAction CreateDocumentChangeAction(MemberDeclarationSyntax nodeToPushUp, Document contextDocument);

        protected abstract CodeAction CreateSolutionChangeAction(MemberDeclarationSyntax nodeToPushUp, Document contextDocument);
    }
}
