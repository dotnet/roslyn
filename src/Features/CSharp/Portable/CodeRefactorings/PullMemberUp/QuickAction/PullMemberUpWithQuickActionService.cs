// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp.QuickAction;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.QuickAction
{
    [ExportLanguageService(typeof(IPullMemberUpWithQuickActionService), LanguageNames.CSharp), Shared]
    internal class PullMemberUpWithQuickActionService : IPullMemberUpWithQuickActionService
    {
        public async Task<CodeAction> ComputeClassRefactoring(INamedTypeSymbol targetTypeSymbol, CodeRefactoringContext context, SemanticModel semanticModel, SyntaxNode userSelectedNode)
        {
            var classPuller = new ClassPullerWithQuickAction();
            return await classPuller.ComputeRefactoring(targetTypeSymbol, context, semanticModel, userSelectedNode);
        }

        public async  Task<CodeAction> ComputeInterfaceRefactoring(INamedTypeSymbol targetTypeSymbol, CodeRefactoringContext context, SemanticModel semanticModel, SyntaxNode userSelectedNode)
        {
            var interfacePuller = new InterfacePullerWithQuickAction();
            return await interfacePuller.ComputeRefactoring(targetTypeSymbol, context, semanticModel, userSelectedNode);
        }
    }
}
