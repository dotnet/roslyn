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
using Microsoft.CodeAnalysis.Shared.Utilities;
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
    internal sealed class SuppressionSuggestedAction : SuggestedAction, ITelemetryDiagnosticID<string>
    {
        private readonly CodeFix _fix;
        private readonly Func<CodeAction, SuggestedActionSet> _getFixAllSuggestedActionSet;

        public SuppressionSuggestedAction(
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            CodeFix fix,
            object provider,
            Func<CodeAction, SuggestedActionSet> getFixAllSuggestedActionSet)
            : base(sourceProvider, workspace, subjectBuffer, provider, fix.Action)
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
                return this.CodeAction.GetNestedCodeActions().Any();
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

            if (this.CodeAction.GetNestedCodeActions().Any())
            {
                var nestedSuggestedActions = ArrayBuilder<SuggestedAction>.GetInstance();
                var fixCount = this.CodeAction.GetNestedCodeActions().Length;

                foreach (var action in this.CodeAction.GetNestedCodeActions())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fixAllSuggestedActionSet = _getFixAllSuggestedActionSet(action);
                    nestedSuggestedActions.Add(new CodeFixSuggestedAction(
                        this.SourceProvider, this.Workspace, this.SubjectBuffer,
                        new CodeFix(_fix.Project, action, _fix.Diagnostics),
                        this.Provider, fixAllSuggestedActionSet, action));
                }

                _actionSets = ImmutableArray.Create(
                    new SuggestedActionSet(nestedSuggestedActions.ToImmutableAndFree()));

                return Task.FromResult(_actionSets);
            }

            return SpecializedTasks.Default<IEnumerable<SuggestedActionSet>>();
        }

        protected override Task InvokeAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            // The top-level action cannot be invoked.
            // However, the nested sub-actions returned above can be.
            throw new NotSupportedException(string.Format(EditorFeaturesResources._0_does_not_support_the_1_operation_However_it_may_contain_nested_2_s_see_2_3_that_support_this_operation,
                nameof(SuppressionSuggestedAction),
                nameof(Invoke),
                nameof(ISuggestedAction),
                nameof(GetActionSetsAsync)));
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
