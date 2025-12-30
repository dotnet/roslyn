// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable;

internal sealed partial class EnableNullableCodeRefactoringProvider : CodeRefactoringProvider
{
    public sealed override CodeAnalysis.CodeRefactorings.RefactorAllProvider? GetRefactorAllProvider()
        => RefactorAllProvider.Instance;

    private sealed class RefactorAllProvider : CodeAnalysis.CodeRefactorings.RefactorAllProvider
    {
        public static readonly RefactorAllProvider Instance = new();

        private RefactorAllProvider()
        {
        }

        public override IEnumerable<RefactorAllScope> GetSupportedRefactorAllScopes()
            => [RefactorAllScope.Solution];

        public override async Task<CodeAction?> GetRefactoringAsync(RefactorAllContext fixAllContext)
        {
            Debug.Assert(fixAllContext.Scope == RefactorAllScope.Solution);
            return new FixAllCodeAction(EnableNullableReferenceTypesInSolutionAsync);

            async Task<Solution> EnableNullableReferenceTypesInSolutionAsync(
                CodeActionPurpose purpose, IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                var solution = fixAllContext.Solution;
                foreach (var projectId in solution.ProjectIds)
                {
                    var project = solution.GetRequiredProject(projectId);
                    if (!ShouldOfferRefactoring(project))
                        continue;

                    solution = await EnableNullableReferenceTypesAsync(project, purpose, fixAllContext.CancellationToken).ConfigureAwait(false);
                }

                return solution;
            }
        }

        private sealed class FixAllCodeAction(Func<CodeActionPurpose, IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> createChangedSolution)
            : CodeAction.SolutionChangeAction(
                CSharpFeaturesResources.Enable_nullable_reference_types_in_solution,
                (progress, cancellationToken) => createChangedSolution(CodeActionPurpose.Apply, progress, cancellationToken),
                nameof(CSharpFeaturesResources.Enable_nullable_reference_types_in_solution),
                CodeActionPriority.Default,
                CodeActionCleanup.Default)
        {
            private readonly Func<CodeActionPurpose, IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> _createChangedSolution = createChangedSolution;

            protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                var changedSolution = await _createChangedSolution(
                    CodeActionPurpose.Preview, progress, cancellationToken).ConfigureAwait(false);
                return changedSolution is null ? [] : [new ApplyChangesOperation(changedSolution)];
            }
        }
    }
}
