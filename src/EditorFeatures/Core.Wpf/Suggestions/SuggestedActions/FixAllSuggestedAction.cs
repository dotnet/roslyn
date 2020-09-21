// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Suggested action for fix all occurrences code fix.  Note: this is only used
    /// as a 'flavor' inside CodeFixSuggestionAction.
    /// </summary>
    internal sealed partial class FixAllSuggestedAction : SuggestedAction, ITelemetryDiagnosticID<string>, IFixAllSuggestedAction
    {
        public Diagnostic Diagnostic { get; }

        /// <summary>
        /// The original code-action that we are a fix-all for.  i.e. _originalCodeAction
        /// would be something like "use 'var' instead of 'int'", this suggestion action
        /// and our <see cref="SuggestedAction.CodeAction"/> is the actual action that 
        /// will perform the fix in the appropriate document/project/solution scope.
        /// </summary>
        public CodeAction OriginalCodeAction { get; }

        public FixAllState FixAllState { get; }

        internal FixAllSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            FixAllState fixAllState,
            Diagnostic originalFixedDiagnostic,
            CodeAction originalCodeAction)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer,
                   fixAllState.FixAllProvider, new FixAllCodeAction(fixAllState))
        {
            Diagnostic = originalFixedDiagnostic;
            OriginalCodeAction = originalCodeAction;
            FixAllState = fixAllState;
        }

        public override bool TryGetTelemetryId(out Guid telemetryId)
        {
            // We get the telemetry id for the original code action we are fixing,
            // not the special 'FixAllCodeAction'.  that is the .CodeAction this
            // SuggestedAction is pointing at.
            telemetryId = OriginalCodeAction.GetType().GetTelemetryId(FixAllState.Scope.GetScopeIdForTelemetry());
            return true;
        }

        public string GetDiagnosticID()
            => Diagnostic.GetTelemetryDiagnosticID();

        protected override void InnerInvoke(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesSession, FixAllLogger.CreateCorrelationLogMessage(FixAllState.CorrelationId), cancellationToken))
            {
                base.InnerInvoke(progressTracker, cancellationToken);
            }
        }
    }
}
