// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal sealed class FixAllCodeRefactoringCodeAction : CodeAction
    {
        internal readonly FixAllState FixAllState;

        internal FixAllCodeRefactoringCodeAction(FixAllState fixAllState)
        {
            FixAllState = fixAllState;
        }

        public override string Title
            => this.FixAllState.FixAllScope switch
            {
                FixAllScope.Document => FeaturesResources.Document,
                FixAllScope.Project => FeaturesResources.Project,
                FixAllScope.Solution => FeaturesResources.Solution,
                FixAllScope.Selection => FeaturesResources.Selection,
                FixAllScope.ContainingMember => FeaturesResources.Containing_Member,
                FixAllScope.ContainingType => FeaturesResources.Containing_Type,
                _ => throw new NotSupportedException(),
            };

        internal override string Message => FeaturesResources.Computing_fix_all_occurrences_code_fix;

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => await ComputeOperationsAsync(new ProgressTracker(), cancellationToken).ConfigureAwait(false);

        internal override Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var service = FixAllState.Project.Solution.Workspace.Services.GetService<IFixAllCodeRefactoringGetFixesService>();

            var fixAllContext = new FixAllContext(FixAllState, progressTracker, cancellationToken);
            if (progressTracker != null)
                progressTracker.Description = FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext);

            return service.GetFixAllOperationsAsync(fixAllContext);
        }

        internal sealed override Task<Solution> GetChangedSolutionAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var service = FixAllState.Project.Solution.Workspace.Services.GetService<IFixAllCodeRefactoringGetFixesService>();

            var fixAllContext = new FixAllContext(FixAllState, progressTracker, cancellationToken);
            if (progressTracker != null)
                progressTracker.Description = FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext);

            return service.GetFixAllChangedSolutionAsync(fixAllContext);
        }
    }
}
