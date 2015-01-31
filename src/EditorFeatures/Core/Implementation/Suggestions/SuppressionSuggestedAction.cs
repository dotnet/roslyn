// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

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

        private ImmutableArray<SuggestedActionSet> _actionSets;
        public override IEnumerable<SuggestedActionSet> ActionSets
        {
            get
            {
                if (_actionSets != null)
                {
                    return _actionSets;
                }

                var suppressionAction = (SuppressionCodeAction)this.CodeAction;
                if ((suppressionAction.NestedActions != null) && suppressionAction.NestedActions.Any())
                {
                    var nestedSuggestedActions = ImmutableArray.CreateBuilder<SuggestedAction>();

                    foreach (var c in suppressionAction.NestedActions)
                    {
                        nestedSuggestedActions.Add(new CodeFixSuggestedAction(
                                this.Workspace, this.SubjectBuffer, this.EditHandler,
                                new CodeFix(c, _fix.Diagnostics), this.Provider, null));
                    }

                    _actionSets = ImmutableArray.Create(
                        new SuggestedActionSet(nestedSuggestedActions.ToImmutable()));

                    return _actionSets;
                }

                return null;
            }
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            // The top-level action cannot be invoked.
            // However, the nested sub-actions returned above can be.
            throw new NotSupportedException(string.Format(EditorFeaturesResources.OperationNotSupported,
                nameof(SuppressionSuggestedAction),
                nameof(Invoke),
                nameof(ISuggestedAction),
                nameof(ActionSets)));
        }

        public override object GetPreview(CancellationToken cancellationToken)
        {
            // The top-level action won't show any preview.
            // However, the nested sub-actions returned above will show preview.
            return null;
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
