// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings
{
    internal abstract class FixAllCodeAction : CodeAction
    {
        internal readonly IFixAllState FixAllState;
        private bool _showPreviewChangesDialog;

        protected FixAllCodeAction(
            IFixAllState fixAllState, bool showPreviewChangesDialog)
        {
            FixAllState = fixAllState;
            _showPreviewChangesDialog = showPreviewChangesDialog;
        }

        protected abstract bool IsInternalProvider(IFixAllState fixAllState);
        protected abstract IFixAllContext CreateFixAllContext(IFixAllState fixAllState, IProgressTracker progressTracker, CancellationToken cancellationToken);

        public override string Title
            => this.FixAllState.Scope switch
            {
                FixAllScope.Document => FeaturesResources.Document,
                FixAllScope.Project => FeaturesResources.Project,
                FixAllScope.Solution => FeaturesResources.Solution,
                FixAllScope.ContainingMember => FeaturesResources.Containing_Member,
                FixAllScope.ContainingType => FeaturesResources.Containing_Type,
                _ => throw ExceptionUtilities.UnexpectedValue(this.FixAllState.Scope),
            };

        internal override string Message => FeaturesResources.Computing_fix_all_occurrences_code_fix;

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => await ComputeOperationsAsync(new ProgressTracker(), cancellationToken).ConfigureAwait(false);

        internal override Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FixAllLogger.LogState(FixAllState, IsInternalProvider(FixAllState));

            var service = FixAllState.Project.Solution.Workspace.Services.GetRequiredService<IFixAllGetFixesService>();

            var fixAllContext = CreateFixAllContext(FixAllState, progressTracker, cancellationToken);
            progressTracker.Description = fixAllContext.GetDefaultFixAllTitle();

            return service.GetFixAllOperationsAsync(fixAllContext, _showPreviewChangesDialog);
        }

        internal sealed override Task<Solution?> GetChangedSolutionAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FixAllLogger.LogState(FixAllState, IsInternalProvider(FixAllState));

            var service = FixAllState.Project.Solution.Workspace.Services.GetRequiredService<IFixAllGetFixesService>();

            var fixAllContext = CreateFixAllContext(FixAllState, progressTracker, cancellationToken);
            progressTracker.Description = fixAllContext.GetDefaultFixAllTitle();

            return service.GetFixAllChangedSolutionAsync(fixAllContext);
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly FixAllCodeAction _fixAllCodeAction;

            internal TestAccessor(FixAllCodeAction fixAllCodeAction)
                => _fixAllCodeAction = fixAllCodeAction;

            /// <summary>
            /// Gets a reference to <see cref="_showPreviewChangesDialog"/>, which can be read or written by test code.
            /// </summary>
            public ref bool ShowPreviewChangesDialog
                => ref _fixAllCodeAction._showPreviewChangesDialog;
        }
    }
}
