// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal interface IPullMemberUpWithQuickActionService : ILanguageService
    {
        Task<CodeAction> ComputeClassRefactoring(INamedTypeSymbol targetTypeSymbol, CodeRefactoringContext context, SemanticModel semanticModel, SyntaxNode userSelectedNode);

        Task<CodeAction> ComputeInterfaceRefactoring(INamedTypeSymbol targetTypeSymbol, CodeRefactoringContext context, SemanticModel semanticModel, SyntaxNode userSelectedNode);
    }
}
