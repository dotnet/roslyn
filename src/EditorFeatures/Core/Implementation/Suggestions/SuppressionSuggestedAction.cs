// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
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

        public SuppressionSuggestedAction(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            CodeFix fix,
            object provider) :
                base(workspace, subjectBuffer, editHandler, fix.Action, provider)
        {
            _fix = fix;
        }

        public override bool HasActionSets
        {
            get
            {
                var suppressionAction = (SuppressionCodeAction)this.CodeAction;
                return (suppressionAction.NestedActions != null) && suppressionAction.NestedActions.Any();
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

            var suppressionAction = (SuppressionCodeAction)this.CodeAction;
            if ((suppressionAction.NestedActions != null) && suppressionAction.NestedActions.Any())
            {
                var nestedSuggestedActions = ImmutableArray.CreateBuilder<SuggestedAction>();

                foreach (var c in suppressionAction.NestedActions)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    nestedSuggestedActions.Add(new CodeFixSuggestedAction(
                            this.Workspace, this.SubjectBuffer, this.EditHandler,
                            new CodeFix(c, _fix.Diagnostics), this.Provider, null));
                }

                _actionSets = ImmutableArray.Create(
                    new SuggestedActionSet(nestedSuggestedActions.ToImmutable()));

                return Task.FromResult(_actionSets);
            }

            return SpecializedTasks.Default<IEnumerable<SuggestedActionSet>>();
        }

        public override void Invoke(CancellationToken cancellationToken)
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
            return diagnostic.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }
    }
}
