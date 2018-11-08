// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal abstract class AbstractMemberPullerWithQuickAction
    {
        internal CodeAction ComputeRefactoring(
            INamedTypeSymbol targetTypeSymbol,
            Document document,
            ISymbol userSelectNodeSymbol)
        {
            var title = FeaturesResources.Add_to + " " + targetTypeSymbol.Name;
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(targetTypeSymbol, new (ISymbol, bool)[] { (userSelectNodeSymbol, false)});
            if (IsDeclarationAlreadyInTarget(targetTypeSymbol, userSelectNodeSymbol) ||
                !result.IsValid)
            {
                return default;
            }

            var generator = document.Project.LanguageServices.GetService<IPullMemberUpActionAndSolutionGenerator>();
            return generator.GetCodeActionAsync(result, document, title);
        }

        protected abstract bool IsDeclarationAlreadyInTarget(INamedTypeSymbol target, ISymbol symbol);
    }
}
