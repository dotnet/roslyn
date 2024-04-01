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
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable;

internal partial class EnableNullableCodeRefactoringProvider : CodeRefactoringProvider
{
    internal sealed override CodeAnalysis.CodeRefactorings.FixAllProvider? GetFixAllProvider()
        => FixAllProvider.Instance;

    private sealed class FixAllProvider : CodeAnalysis.CodeRefactorings.FixAllProvider
    {
        public static readonly FixAllProvider Instance = new();

        private FixAllProvider()
        {
        }

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => ImmutableArray.Create(FixAllScope.Solution);

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            Debug.Assert(fixAllContext.Scope == FixAllScope.Solution);
            return Task.FromResult<CodeAction?>(new FixAllCodeAction(EnableNullableReferenceTypesInSolutionAsync));

            async Task<Solution> EnableNullableReferenceTypesInSolutionAsync(
                CodeActionPurpose purpose, IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                var solution = fixAllContext.Solution;
                foreach (var projectId in solution.ProjectIds)
                {
                    var project = solution.GetRequiredProject(projectId);
                    if (!ShouldOfferRefactoring(project))
                        continue;

                    solution = await EnableNullableReferenceTypesAsync(project, purpose,
                        fixAllContext.GetOptionsProvider(), progress, fixAllContext.CancellationToken).ConfigureAwait(false);
                }

                return solution;
            }
        }

        private sealed class FixAllCodeAction(Func<CodeActionPurpose, IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> createChangedSolution)
            : CodeAction.SolutionChangeAction(
                CSharpFeaturesResources.Enable_nullable_reference_types_in_solution,
                (progress, cancellationToken) => createChangedSolution(CodeActionPurpose.Apply, progress, cancellationToken),
                nameof(CSharpFeaturesResources.Enable_nullable_reference_types_in_solution))
        {
            private readonly Func<CodeActionPurpose, IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> _createChangedSolution = createChangedSolution;

            protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                var changedSolution = await _createChangedSolution(
                    CodeActionPurpose.Preview, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
                if (changedSolution is null)
                    return [];

                return new CodeActionOperation[] { new ApplyChangesOperation(changedSolution) };
            }
        }
    }
}
