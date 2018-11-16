// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal abstract class AbstractMemberPullerWithQuickAction
    {
        /// <summary>
        ///  This method is used to check whether the selected member overrides the member in target.
        ///  It just checks the members directly declared in the target.
        /// </summary>
        protected abstract bool IsDeclarationAlreadyInTarget(INamedTypeSymbol target, ISymbol symbol);

        internal async Task<CodeAction> TryComputeRefactoring(
            Document document,
            ISymbol userSelectNodeSymbol,
            INamedTypeSymbol targetTypeSymbol)
        {
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(targetTypeSymbol, new ISymbol[] { userSelectNodeSymbol });
            if (IsDeclarationAlreadyInTarget(targetTypeSymbol, userSelectNodeSymbol) ||
                result._pullUpOperationCauseError)
            {
                return default;
            }

            var generator = new CodeActionAndSolutionGenerator(); 
            var title = string.Format(FeaturesResources.Add_to_0, targetTypeSymbol.ToDisplayString());
            return await generator.TryGetCodeActionAsync(result, document, title);
        }
    }
}
