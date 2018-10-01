// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal abstract class AbstractMemberPusher
    {
        protected string Title { get; }

        protected INamedTypeSymbol TargetTypeSymbol { get; }

        protected SyntaxNode TargetSyntaxNode { get; }

        protected SemanticModel SemanticModel { get; }

        protected SyntaxNode UserSelectedNode { get; }

        protected Document ContextDocument { get; }

        internal AbstractMemberPusher(
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

        protected virtual MemberDeclarationSyntax GeneratePushUpMemberAndFormatting(MemberDeclarationSyntax memberDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorSyntax = null)
        {
            // variableDeclaratorNode is not null means it is field or event
            SyntaxAnnotation[] formatter = { Formatter.Annotation };
            return CreateMemberPushUpSyntax(memberDeclarationSyntax, variableDeclaratorSyntax).WithAdditionalAnnotations(formatter);
        }

        protected virtual BaseFieldDeclarationSyntax FindFieldAndEventDeclaration(VariableDeclaratorSyntax variableDeclaratorNode)
        {
            return variableDeclaratorNode.Ancestors().OfType<BaseFieldDeclarationSyntax>().First();
        }

        protected virtual MemberDeclarationSyntax CreateMemberPushUpSyntax(MemberDeclarationSyntax memberDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorSyntax)
        {
            switch (memberDeclarationSyntax)
            {
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    return CreateMethodPushUpSyntax(methodDeclarationSyntax);
                case EventFieldDeclarationSyntax eventFieldDeclarationSyntax:
                    return CreateEventPushUpSyntax(eventFieldDeclarationSyntax, variableDeclaratorSyntax);
                case PropertyDeclarationSyntax propertyFieldDeclarationSyntax:
                    return CreatePropertyPushUpSyntax(propertyFieldDeclarationSyntax);
                case IndexerDeclarationSyntax indexerDeclarationSyntax:
                    return CreateIndexerPushUpSyntax(indexerDeclarationSyntax);
                case FieldDeclarationSyntax fieldDeclarationSyntax:
                    return CreateFieldPushUpSyntax(fieldDeclarationSyntax, variableDeclaratorSyntax);
                default:
                    throw new ArgumentException($"{nameof(memberDeclarationSyntax)}'s type is invalid");
            }
        }

        internal virtual CodeAction ComputeRefactoring()
        {
            if (UserSelectedNode is VariableDeclaratorSyntax selectVariableDeclaratorNode &&
                AreModifiersValid(selectVariableDeclaratorNode))
            {
                // Event and Field are different, it may contains several declarations in one line
                var selectFieldOrEventNode = FindFieldAndEventDeclaration(selectVariableDeclaratorNode);
                return CreateAction(selectFieldOrEventNode, ContextDocument, selectVariableDeclaratorNode);
            }
            else if (UserSelectedNode is MethodDeclarationSyntax selectMethodNode &&
                    AreModifiersValid(selectMethodNode) &&
                    IsOrdinaryMethod(selectMethodNode))
            {
                return CreateAction(selectMethodNode, ContextDocument);
            }
            else if (UserSelectedNode is BasePropertyDeclarationSyntax selectPropertyOrIndexerNode &&
                    AreModifiersValid(selectPropertyOrIndexerNode))
            {
                return CreateAction(selectPropertyOrIndexerNode, ContextDocument);
            }
            else
            {
                return default;
            }
        }

        internal virtual CodeAction CreateAction(
            MemberDeclarationSyntax selectedMemberNode,
            Document contextDocument,
            VariableDeclaratorSyntax selectVariableDeclaratorNode = null)
        {
            var nodeToPushUp = GeneratePushUpMemberAndFormatting(selectedMemberNode, selectVariableDeclaratorNode);

            if (TargetSyntaxNode.SyntaxTree == selectedMemberNode.SyntaxTree)
            {
                return CreateDocumentChangeAction(nodeToPushUp, contextDocument);
            }
            else
            {
                return CreateSolutionChangeAction(nodeToPushUp, contextDocument);
            }
        }

        protected virtual EventFieldDeclarationSyntax CreateEventPushUpSyntax(EventFieldDeclarationSyntax eventFieldDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorNode)
        {
            var identifierSyntax = variableDeclaratorNode.Parent.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            var declarationList = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
            declarationList = declarationList.Add(variableDeclaratorNode);
            var variableDeclaration = SyntaxFactory.VariableDeclaration(identifierSyntax, declarationList);
            return SyntaxFactory.EventFieldDeclaration(variableDeclaration);
        }

        protected virtual FieldDeclarationSyntax CreateFieldPushUpSyntax(FieldDeclarationSyntax fieldDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorNode)
        {
            var preTypeSyntax = variableDeclaratorNode.Parent.DescendantNodes().OfType<PredefinedTypeSyntax>().First();
            var declarationList = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
            declarationList = declarationList.Add(variableDeclaratorNode);
            var variableDeclaration = SyntaxFactory.VariableDeclaration(preTypeSyntax, declarationList);
            return SyntaxFactory.FieldDeclaration(fieldDeclarationSyntax.AttributeLists, fieldDeclarationSyntax.Modifiers, variableDeclaration);
        }

        abstract protected bool AreModifiersValid(SyntaxNode userSelectNode);

        abstract protected CodeAction CreateDocumentChangeAction(MemberDeclarationSyntax nodeToPushUp, Document contextDocument);

        abstract protected CodeAction CreateSolutionChangeAction(MemberDeclarationSyntax nodeToPushUp, Document contextDocument);

        abstract protected MethodDeclarationSyntax CreateMethodPushUpSyntax(MethodDeclarationSyntax methodDeclarationSyntax);

        abstract protected PropertyDeclarationSyntax CreatePropertyPushUpSyntax(PropertyDeclarationSyntax propertyFieldDeclarationSyntax);

        abstract protected IndexerDeclarationSyntax CreateIndexerPushUpSyntax(IndexerDeclarationSyntax indexerDeclarationSyntax);

    }
}
