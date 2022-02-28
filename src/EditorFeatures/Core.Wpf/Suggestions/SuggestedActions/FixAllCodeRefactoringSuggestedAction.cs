// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Suggested action for fix all occurrences for a code refactoring.  Note: this is only used
    /// as a 'flavor' inside CodeRefactoringSuggestionAction.
    /// </summary>
    internal sealed class FixAllCodeRefactoringSuggestedAction : SuggestedAction, IFixAllCodeRefactoringSuggestedAction
    {
        /// <summary>
        /// The original code-action that we are a fix-all for.  i.e. _originalCodeAction
        /// would be something like "use 'var' instead of 'int'", this suggestion action
        /// and our <see cref="SuggestedAction.CodeAction"/> is the actual action that 
        /// will perform the fix in the appropriate document/project/solution scope.
        /// </summary>
        public CodeAction OriginalCodeAction { get; }

        public FixAllState FixAllState { get; }

        internal FixAllCodeRefactoringSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            FixAllState fixAllState,
            CodeAction originalCodeAction)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer,
                   fixAllState.FixAllProvider, new FixAllCodeRefactoringCodeAction(fixAllState))
        {
            OriginalCodeAction = originalCodeAction;
            FixAllState = fixAllState;
        }

        public override bool TryGetTelemetryId(out Guid telemetryId)
        {
            // We get the telemetry id for the original code action we are fixing,
            // not the special 'FixAllCodeAction'.  that is the .CodeAction this
            // SuggestedAction is pointing at.
            telemetryId = OriginalCodeAction.GetType().GetTelemetryId(FixAllState.FixAllScope.GetScopeIdForTelemetry());
            return true;
        }

        protected override async Task InnerInvokeAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesSession, FixAllLogger.CreateCorrelationLogMessage(FixAllState.CorrelationId), cancellationToken))
            {
                await base.InnerInvokeAsync(progressTracker, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
