// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Suggested action for fix all occurrences code fix.  Note: this is only used
    /// as a 'flavor' inside CodeFixSuggestionAction.
    /// </summary>
    internal sealed partial class FixAllSuggestedAction : SuggestedAction, ITelemetryDiagnosticID<string>
    {
        private readonly Diagnostic _fixedDiagnostic;

        internal FixAllSuggestedAction(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            FixAllState fixAllState,
            Diagnostic originalFixedDiagnostic,
            IAsynchronousOperationListener operationListener)
            : base(workspace, subjectBuffer, editHandler, waitIndicator,
                  new FixAllCodeAction(fixAllState), fixAllState.FixAllProvider,
                  operationListener)
        {
            _fixedDiagnostic = originalFixedDiagnostic;
        }

        public string GetDiagnosticID()
        {
            // we log diagnostic id as it is if it is from us
            if (_fixedDiagnostic.Descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.Telemetry))
            {
                return _fixedDiagnostic.Id;
            }

            // if it is from third party, we use hashcode
            return _fixedDiagnostic.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        protected override async Task InvokeAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesSession, cancellationToken))
            {
                await base.InvokeAsync(progressTracker, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}