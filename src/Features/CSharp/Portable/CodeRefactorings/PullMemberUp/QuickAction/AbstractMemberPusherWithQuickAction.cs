// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeGeneration;
using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal abstract class AbstractMemberPullerWithQuickAction
    {
        protected string Title { get; }

        protected INamedTypeSymbol TargetTypeSymbol { get; }

        // TODO: How to get the target Syntax Node
        // 1. start with only works with one node
        // 2. start with 
        protected SyntaxNode TargetSyntaxNode { get; }

        protected SemanticModel SemanticModel { get; }

        protected ISymbol UserSelectedNodeSymbol { get; }

        protected SyntaxNode UserSelectedNode { get; }

        protected Document ContextDocument { get; }

        protected ICodeGenerationService CodeGenerationService { get; }

        protected ChangedSolutionAndDocumentCreator ChangedSolutionAndDocumentCreator { get; }

        protected CancellationToken CancellationToken { get; }

        internal AbstractMemberPullerWithQuickAction(
            INamedTypeSymbol targetTypeSymbol,
            CodeRefactoringContext context,
            SemanticModel semanticModel,
            SyntaxNode userSelectedNode)
        {
            Title = $"Add to {targetTypeSymbol.Name}";
            TargetTypeSymbol = targetTypeSymbol;
            TargetSyntaxNode = TargetTypeSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            SemanticModel = semanticModel;
            UserSelectedNodeSymbol = SemanticModel.GetDeclaredSymbol(userSelectedNode);
            UserSelectedNode = userSelectedNode;
            ContextDocument = context.Document;
            CodeGenerationService = context.Document.Project.LanguageServices.GetService<ICodeGenerationService>();
            ChangedSolutionAndDocumentCreator = new ChangedSolutionAndDocumentCreator();
            CancellationToken = context.CancellationToken;
        }

        protected virtual bool IsOrdinaryMethod(IMethodSymbol methodNodeSymbol)
        {
            return methodNodeSymbol.MethodKind == MethodKind.Ordinary;
        }

        internal virtual CodeAction ComputeRefactoring()
        {
            if (UserSelectedNodeSymbol is IFieldSymbol fieldSymbol &&
                AreModifiersValid(TargetTypeSymbol, UserSelectedNodeSymbol))
            {
                return CreateAction(fieldSymbol, ContextDocument);
            }
            else if (UserSelectedNodeSymbol is IMethodSymbol methodSymbol &&
                    AreModifiersValid(TargetTypeSymbol, UserSelectedNodeSymbol) &&
                    IsOrdinaryMethod(methodSymbol))
            {
                return CreateAction(methodSymbol, ContextDocument);
            }
            else if (UserSelectedNodeSymbol is IPropertySymbol propertyOrIndexerSymbol &&
                    AreModifiersValid(TargetTypeSymbol, propertyOrIndexerSymbol))
            {
                return CreateAction(propertyOrIndexerSymbol, ContextDocument);
            }
            else if (UserSelectedNodeSymbol is IEventSymbol eventSymbol &&
                AreModifiersValid(TargetTypeSymbol, UserSelectedNodeSymbol))
            {
                return CreateAction(eventSymbol, ContextDocument);
            }
            else
            {
                return default;
            }
        }

        protected CodeAction CreateDocumentOrSolutionChangedAction(
            SyntaxNode nodeToPullUp,
            Document contextDocument)
        {
            if (TargetSyntaxNode.SyntaxTree == UserSelectedNode.SyntaxTree)
            {
                return CreateDocumentChangeAction(nodeToPullUp, contextDocument);
            }
            else
            {
                return CreateSolutionChangeAction(nodeToPullUp, contextDocument);
            }
        }

        internal abstract CodeAction CreateAction(IMethodSymbol methodSymbol, Document contextDocument);

        internal abstract CodeAction CreateAction(IPropertySymbol propertyOrIndexerNode, Document contextDocument);

        internal abstract CodeAction CreateAction(IEventSymbol eventSymbol, Document contextDocument);

        internal abstract CodeAction CreateAction(IFieldSymbol fieldSyntax, Document contextDocument);

        internal abstract bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMembers);

        protected abstract CodeAction CreateDocumentChangeAction(SyntaxNode nodeToPullUp, Document contextDocument);

        protected abstract CodeAction CreateSolutionChangeAction(SyntaxNode nodeToPullUp, Document contextDocument);
    }
}
