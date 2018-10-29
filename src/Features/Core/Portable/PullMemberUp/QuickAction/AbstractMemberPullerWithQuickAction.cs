// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal abstract class AbstractMemberPullerWithQuickAction
    {
        protected string Title { get; set; }

        protected INamedTypeSymbol TargetTypeSymbol { get; set; }

        protected SyntaxNode TargetTypeNode { get; set; }

        protected SyntaxNode UserSelectedNode { get; set; }

        protected Document ContextDocument { get; set; }

        protected ICodeGenerationService CodeGenerationService { get; set; }

        protected IPullMemberUpSyntaxChangeService RemoveService { get; set; }

        protected CancellationToken _cancellationToken;

        internal async virtual Task<CodeAction> ComputeRefactoring(
            INamedTypeSymbol targetTypeSymbol,
            CodeRefactoringContext context,
            SyntaxNode userSelectedNode,
            ISymbol userSelectNodeSymbol)
        {
            Title = FeaturesResources.Add_To + targetTypeSymbol.Name;
            CodeGenerationService = context.Document.Project.LanguageServices.GetService<ICodeGenerationService>();
            RemoveService = context.Document.Project.LanguageServices.GetRequiredService<IPullMemberUpSyntaxChangeService>();
            _cancellationToken = context.CancellationToken;
            ContextDocument = context.Document;
            UserSelectedNode = userSelectedNode;
            TargetTypeSymbol = targetTypeSymbol;

            if (userSelectNodeSymbol is IMethodSymbol methodSymbol &&
                methodSymbol.MethodKind != MethodKind.Ordinary)
            {
                return default;
            }

            if (IsDeclarationAlreadyInTarget(targetTypeSymbol, userSelectNodeSymbol) ||
                !AreModifiersValid(targetTypeSymbol, userSelectNodeSymbol))
            {
                return default;
            }

            TargetTypeNode = await CodeGenerationService.FindMostRelevantNameSpaceOrTypeDeclarationAsync(context.Document.Project.Solution, targetTypeSymbol);

            if (TargetTypeNode == null)
            {
                return default;
            }

            return await CreateAction(userSelectNodeSymbol);
        }

        private bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(targetSymbol, new (ISymbol, bool)[] { (selectedMember, false)});
            return result.IsValid;
        }

        internal abstract Task<CodeAction> CreateAction(ISymbol memberSymbol);

        protected abstract bool IsDeclarationAlreadyInTarget(INamedTypeSymbol targetSymbol, ISymbol userSelectedNodeSymbol);

    }
}
