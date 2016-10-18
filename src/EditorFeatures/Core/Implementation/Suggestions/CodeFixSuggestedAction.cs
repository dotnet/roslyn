// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Represents light bulb menu item for code fixes.
    /// </summary>
    internal sealed class CodeFixSuggestedAction : SuggestedAction, ISuggestedActionWithFlavors, ITelemetryDiagnosticID<string>
    {
        private readonly CodeFix _fix;
        public SuggestedActionSet FixAllSuggestedActionSet { get; }

        private ImmutableArray<SuggestedActionSet> _actionSets;

        public CodeFixSuggestedAction(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            CodeFix fix,
            CodeAction action,
            object provider,
            SuggestedActionSet fixAllSuggestedActionSet,
            IAsynchronousOperationListener operationListener)
            : base(workspace, subjectBuffer, editHandler, waitIndicator, action, provider, operationListener)
        {
            _fix = fix;
            FixAllSuggestedActionSet = fixAllSuggestedActionSet;
        }

        public override bool HasActionSets
        {
            get
            {
                // HasActionSets is called synchronously on the UI thread. In order to avoid blocking the UI thread,
                // we need to provide a 'quick' answer here as opposed to the 'right' answer. Providing the 'right'
                // answer is expensive (because we will need to call CodeAction.GetPreviewOperationsAsync() (to
                // compute whether or not we should display the flavored action for 'Preview Changes') which in turn
                // will involve computing the changed solution for the ApplyChangesOperation for the fix / refactoring
                // So we always return 'true' here (so that platform will call GetActionSetsAsync() below). Platform
                // guarantees that nothing bad will happen if we return 'true' here and later return 'null' / empty
                // collection from within GetPreviewAsync().

                return true;
            }
        }

        public async override Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Light bulb will always invoke this property on the UI thread.
            AssertIsForeground();

            if (_actionSets.IsDefault)
            {
                var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();

                _actionSets = await extensionManager.PerformFunctionAsync(Provider, async () =>
                {
                    var builder = ArrayBuilder<SuggestedActionSet>.GetInstance();

                    // We use ConfigureAwait(true) to stay on the UI thread.
                    var previewChangesSuggestedActionSet = await GetPreviewChangesSuggestedActionSetAsync(cancellationToken).ConfigureAwait(true);
                    if (previewChangesSuggestedActionSet != null)
                    {
                        builder.Add(previewChangesSuggestedActionSet);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var fixAllSuggestedActionSet = this.FixAllSuggestedActionSet;
                    if (fixAllSuggestedActionSet != null)
                    {
                        builder.Add(fixAllSuggestedActionSet);
                    }

                    return builder.ToImmutableAndFree();
                    // We use ConfigureAwait(true) to stay on the UI thread.
                }, defaultValue: ImmutableArray<SuggestedActionSet>.Empty).ConfigureAwait(true);
            }

            Contract.ThrowIfTrue(_actionSets.IsDefault);
            return _actionSets;
        }

        private async Task<SuggestedActionSet> GetPreviewChangesSuggestedActionSetAsync(CancellationToken cancellationToken)
        {
            var previewResult = await GetPreviewResultAsync(cancellationToken).ConfigureAwait(true);
            if (previewResult == null)
            {
                return null;
            }

            var changeSummary = previewResult.ChangeSummary;
            if (changeSummary == null)
            {
                return null;
            }

            var previewAction = new PreviewChangesCodeAction(Workspace, CodeAction, changeSummary);
            var previewSuggestedAction = new PreviewChangesSuggestedAction(
                Workspace, SubjectBuffer, EditHandler, WaitIndicator, previewAction, Provider, OperationListener);
            return new SuggestedActionSet(ImmutableArray.Create(previewSuggestedAction));
        }

        /// <summary>
        /// If the provided fix all context is non-null and the context's code action Id matches the given code action's Id then,
        /// returns the set of fix all occurrences actions associated with the code action.
        /// </summary>
        internal static SuggestedActionSet GetFixAllSuggestedActionSet(
            CodeAction action,
            int actionCount,
            FixAllState fixAllState,
            IEnumerable<FixAllScope> supportedScopes,
            Diagnostic firstDiagnostic,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            IAsynchronousOperationListener operationListener)
        {
            if (fixAllState == null)
            {
                return null;
            }

            if (actionCount > 1 && action.EquivalenceKey == null)
            {
                return null;
            }

            var fixAllSuggestedActions = ArrayBuilder<FixAllSuggestedAction>.GetInstance();
            foreach (var scope in supportedScopes)
            {
                var fixAllStateForScope = fixAllState.WithScopeAndEquivalenceKey(scope, action.EquivalenceKey);
                var fixAllAction = new FixAllCodeAction(fixAllStateForScope, showPreviewChangesDialog: true);
                var fixAllSuggestedAction = new FixAllSuggestedAction(
                    workspace, subjectBuffer, editHandler, waitIndicator, fixAllAction,
                    fixAllStateForScope.FixAllProvider, firstDiagnostic, operationListener);
                fixAllSuggestedActions.Add(fixAllSuggestedAction);
            }

            return new SuggestedActionSet(
                fixAllSuggestedActions.ToImmutableAndFree(),
                title: EditorFeaturesResources.Fix_all_occurrences_in);
        }

        public string GetDiagnosticID()
        {
            var diagnostic = _fix.PrimaryDiagnostic;

            // we log diagnostic id as it is if it is from us
            if (diagnostic.Descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.Telemetry))
            {
                return diagnostic.Id;
            }

            // if it is from third party, we use hashcode
            return diagnostic.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        protected override DiagnosticData GetDiagnostic()
        {
            return _fix.GetPrimaryDiagnosticData();
        }
    }
}