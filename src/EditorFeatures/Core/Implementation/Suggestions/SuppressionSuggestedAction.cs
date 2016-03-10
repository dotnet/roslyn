// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Represents top-level light bulb menu item for the suppression fix.
    /// The top-level item itself does nothing. It doesn't display a preview and can't be invoked / applied.
    /// The top-level item is simply a container for the fixes displayed as sub-menu items.
    /// </summary>
    internal class SuppressionSuggestedAction : SuggestedAction, ITelemetryDiagnosticID<string>
    {
        private readonly CodeFix _fix;
        private readonly Func<CodeAction, SuggestedActionSet> _getFixAllSuggestedActionSet;

        public SuppressionSuggestedAction(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            CodeFix fix,
            object provider,
            Func<CodeAction, SuggestedActionSet> getFixAllSuggestedActionSet,
            IAsynchronousOperationListener operationListener)
            : base(workspace, subjectBuffer, editHandler, waitIndicator, fix.Action, provider, operationListener)
        {
            _fix = fix;
            _getFixAllSuggestedActionSet = getFixAllSuggestedActionSet;
        }

        // Put suppressions at the end of everything.
        internal override CodeActionPriority Priority => CodeActionPriority.None;

        public override bool HasActionSets
        {
            get
            {
                return this.CodeAction.GetCodeActions().Any();
            }
        }

        private IEnumerable<SuggestedActionSet> _actionSets;
        public override Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_actionSets != null)
            {
                return Task.FromResult(_actionSets);
            }

            if (this.CodeAction.GetCodeActions().Any())
            {
                var nestedSuggestedActions = ImmutableArray.CreateBuilder<SuggestedAction>();
                var fixCount = this.CodeAction.GetCodeActions().Length;

                foreach (var c in this.CodeAction.GetCodeActions())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fixAllSuggestedActionSet = _getFixAllSuggestedActionSet(c);
                    nestedSuggestedActions.Add(new CodeFixSuggestedAction(
                        this.Workspace, this.SubjectBuffer, this.EditHandler, this.WaitIndicator, new CodeFix(_fix.Project, c, _fix.Diagnostics),
                        c, this.Provider, fixAllSuggestedActionSet, this.OperationListener));
                }

                _actionSets = ImmutableArray.Create(
                    new SuggestedActionSet(nestedSuggestedActions.ToImmutable()));

                return Task.FromResult(_actionSets);
            }

            return SpecializedTasks.Default<IEnumerable<SuggestedActionSet>>();
        }

        protected override Task InvokeAsync(CancellationToken cancellationToken)
        {
            // The top-level action cannot be invoked.
            // However, the nested sub-actions returned above can be.
            throw new NotSupportedException(string.Format(EditorFeaturesResources.OperationNotSupported,
                nameof(SuppressionSuggestedAction),
                nameof(Invoke),
                nameof(ISuggestedAction),
                nameof(GetActionSetsAsync)));
        }

        public override bool HasPreview
        {
            get
            {
                // The top-level action won't show any preview.
                // However, the nested sub-actions returned above will show preview.
                return false;
            }
        }

        public override Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            // The top-level action won't show any preview.
            // However, the nested sub-actions returned above will show preview.
            return SpecializedTasks.Default<object>();
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
            return diagnostic.Id.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }
    }
}
