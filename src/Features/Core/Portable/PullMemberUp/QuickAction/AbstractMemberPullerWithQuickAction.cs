// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal abstract class AbstractMemberPullerWithQuickAction
    {
        /// <summary>
        ///  This method is used to check whether the selected member overrides the member in destination.
        ///  It just checks the members directly declared in the destination.
        /// </summary>
        protected abstract bool IsSelectedMemberDeclarationAlreadyInDestination(INamedTypeSymbol destination, ISymbol symbol);

        internal async Task<CodeAction> TryComputeRefactoring(
            Document document,
            ISymbol selectedMember,
            INamedTypeSymbol destinationType,
            CancellationToken cancellationToken)
        {
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(destinationType, ImmutableArray.Create(selectedMember));
            if (result.PullUpOperationCausesError ||
                IsSelectedMemberDeclarationAlreadyInDestination(destinationType, selectedMember))
            {
                return default;
            }

            var generator = new CodeActionAndSolutionGenerator(); 
            return await generator.TryGetCodeActionAsync(result, document, cancellationToken);
        }
    }
}
