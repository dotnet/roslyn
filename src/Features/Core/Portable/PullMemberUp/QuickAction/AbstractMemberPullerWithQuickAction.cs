// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal abstract class AbstractMemberPullerWithQuickAction
    {
        protected abstract bool IsDeclarationAlreadyInTarget(INamedTypeSymbol target, ISymbol symbol);

        internal CodeAction ComputeRefactoring(
            Document document,
            ISymbol userSelectNodeSymbol,
            INamedTypeSymbol targetTypeSymbol)
        {
            var title = FeaturesResources.Add_to + " " + targetTypeSymbol.Name;
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(targetTypeSymbol, new (ISymbol, bool)[] { (userSelectNodeSymbol, false)});
            if (IsDeclarationAlreadyInTarget(targetTypeSymbol, userSelectNodeSymbol) ||
                !result.IsPullUpOperationCauseError)
            {
                return default;
            }

            var generator = new CodeActionAndSolutionGenerator(); 
            return generator.GetCodeActionAsync(result, document, title);
        }
    }
}
