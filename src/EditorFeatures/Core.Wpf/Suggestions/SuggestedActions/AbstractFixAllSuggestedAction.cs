// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Suggested action for fix all occurrences for a code fix or a code refactoring.
    /// </summary>
    internal abstract class AbstractFixAllSuggestedAction : SuggestedAction
    {
        public CodeAction OriginalCodeAction { get; }

        public IFixAllState FixAllState { get; }

        protected AbstractFixAllSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            IFixAllState fixAllState,
            CodeAction originalCodeAction,
            AbstractFixAllCodeAction fixAllCodeAction)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer,
                   fixAllState.FixAllProvider, fixAllCodeAction)
        {
            OriginalCodeAction = originalCodeAction;
            FixAllState = fixAllState;
        }

        public override bool TryGetTelemetryId(out Guid telemetryId)
        {
            // We get the telemetry id for the original code action we are fixing,
            // not the special 'FixAllCodeAction'.  that is the .CodeAction this
            // SuggestedAction is pointing at.
            telemetryId = OriginalCodeAction.GetTelemetryId(FixAllState.Scope);
            return true;
        }

        protected override async Task InnerInvokeAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var fixAllKind = FixAllState.FixAllKind;
            var functionId = fixAllKind switch
            {
                FixAllKind.CodeFix => FunctionId.CodeFixes_FixAllOccurrencesSession,
                FixAllKind.Refactoring => FunctionId.Refactoring_FixAllOccurrencesSession,
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllKind)
            };

            using (Logger.LogBlock(functionId, FixAllLogger.CreateCorrelationLogMessage(FixAllState.CorrelationId), cancellationToken))
            {
                await base.InnerInvokeAsync(progressTracker, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
