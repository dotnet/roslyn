// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal abstract class AbstractMemberPullerWithQuickAction
    {
        protected string Title { get; set; }

        protected INamedTypeSymbol TargetTypeSymbol { get; set; }

        protected SyntaxNode TargetTypeNode { get; set; }

        protected ISymbol UserSelectedNodeSymbol { get; set; }

        protected SyntaxNode UserSelectedNode { get; set; }

        protected Document ContextDocument { get; set; }

        protected ICodeGenerationService CodeGenerationService { get; set; }


        protected virtual bool IsOrdinaryMethod(IMethodSymbol methodNodeSymbol)
        {
            return methodNodeSymbol.MethodKind == MethodKind.Ordinary;
        }

        internal async virtual Task<CodeAction> ComputeRefactoring(
            INamedTypeSymbol targetTypeSymbol,
            CodeRefactoringContext context,
            SemanticModel semanticModel,
            SyntaxNode userSelectedNode)
        {
            Title = $"Add to {targetTypeSymbol.Name}";
            TargetTypeSymbol = targetTypeSymbol;
            UserSelectedNodeSymbol = semanticModel.GetDeclaredSymbol(userSelectedNode);
            UserSelectedNode = userSelectedNode;
            ContextDocument = context.Document;
            CodeGenerationService = context.Document.Project.LanguageServices.GetService<ICodeGenerationService>();

            if (IsDeclarationAlreadyInTarget())
            {
                return default;
            }

            TargetTypeNode = await CodeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(ContextDocument.Project.Solution, targetTypeSymbol);
            if (UserSelectedNodeSymbol is IFieldSymbol fieldSymbol &&
                AreModifiersValid(TargetTypeSymbol, UserSelectedNodeSymbol))
            {
                return await CreateAction(fieldSymbol, ContextDocument);
            }
            else if (UserSelectedNodeSymbol is IMethodSymbol methodSymbol &&
                    AreModifiersValid(TargetTypeSymbol, UserSelectedNodeSymbol) &&
                    IsOrdinaryMethod(methodSymbol))
            {
                return await CreateAction(methodSymbol, ContextDocument);
            }
            else if (UserSelectedNodeSymbol is IPropertySymbol propertyOrIndexerSymbol &&
                    AreModifiersValid(TargetTypeSymbol, propertyOrIndexerSymbol))
            {
                return await CreateAction(propertyOrIndexerSymbol, ContextDocument);
            }
            else if (UserSelectedNodeSymbol is IEventSymbol eventSymbol &&
                AreModifiersValid(TargetTypeSymbol, UserSelectedNodeSymbol))
            {
                return await CreateAction(eventSymbol, ContextDocument);
            }
            else
            {
                return default;
            }
        }

        internal abstract Task<CodeAction> CreateAction(IMethodSymbol methodSymbol, Document contextDocument);

        internal abstract Task<CodeAction> CreateAction(IPropertySymbol propertyOrIndexerNode, Document contextDocument);

        internal abstract Task<CodeAction> CreateAction(IEventSymbol eventSymbol, Document contextDocument);

        internal abstract Task<CodeAction> CreateAction(IFieldSymbol fieldSyntax, Document contextDocument);

        internal abstract bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMembers);

        protected abstract bool IsDeclarationAlreadyInTarget();
    }
}
