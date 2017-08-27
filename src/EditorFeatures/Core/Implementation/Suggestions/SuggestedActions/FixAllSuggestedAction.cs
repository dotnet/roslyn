// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
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

        /// <summary>
        /// The original code-action that we are a fix-all for.  i.e. _originalCodeAction
        /// would be something like "use 'var' instead of 'int'", this suggestion action
        /// and our <see cref="SuggestedAction.CodeAction"/> is the actual action that 
        /// will perform the fix in the appropriate document/project/solution scope.
        /// </summary>
        private readonly CodeAction _originalCodeAction;
        private readonly FixAllState _fixAllState;

        internal FixAllSuggestedAction(
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            FixAllState fixAllState,
            Diagnostic originalFixedDiagnostic,
            CodeAction originalCodeAction)
            : base(sourceProvider, workspace, subjectBuffer,
                   fixAllState.FixAllProvider, new FixAllCodeAction(fixAllState))
        {
            _fixedDiagnostic = originalFixedDiagnostic;
            _originalCodeAction = originalCodeAction;
            _fixAllState = fixAllState;
        }

        public override bool TryGetTelemetryId(out Guid telemetryId)
        {
            // We get the telemetry id for the original code action we are fixing,
            // not the special 'FixAllCodeAction'.  that is the .CodeAction this
            // SuggestedAction is pointing at.
            var prefix = GetTelemetryPrefix(_originalCodeAction);
            var scope = GetTelemetryScope();
            telemetryId = new Guid(prefix, scope, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            return true;
        }

        private short GetTelemetryScope()
        {
            switch (_fixAllState.Scope)
            {
                case FixAllScope.Document: return 1;
                case FixAllScope.Project: return 2;
                case FixAllScope.Solution: return 3;
                default: return 4;
            }
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

        protected override void InnerInvoke(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesSession, cancellationToken))
            {
                base.InnerInvoke(progressTracker, cancellationToken);
            }
        }
    }
}
