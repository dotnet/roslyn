// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(IErrorTag))]
    internal partial class DiagnosticsSquiggleTaggerProvider : AbstractDiagnosticsAdornmentTaggerProvider<IErrorTag>
    {
        private static readonly IEnumerable<Option<bool>> s_tagSourceOptions =
            ImmutableArray.Create(EditorComponentOnOffOptions.Tagger, InternalFeatureOnOffOptions.Squiggles, ServiceComponentOnOffOptions.DiagnosticProvider);

        protected override IEnumerable<Option<bool>> Options => s_tagSourceOptions;

        [ImportingConstructor]
        public DiagnosticsSquiggleTaggerProvider(
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, diagnosticService, notificationService, listenerProvider)
        {
        }

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
        {
            var isUnnecessary = diagnostic.Severity == DiagnosticSeverity.Hidden && diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary);

            return
                (diagnostic.Severity == DiagnosticSeverity.Warning || diagnostic.Severity == DiagnosticSeverity.Error || isUnnecessary) &&
                !string.IsNullOrWhiteSpace(diagnostic.Message);
        }

        protected override IErrorTag CreateTag(DiagnosticData diagnostic)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(diagnostic.Message));
            var errorType = GetErrorTypeFromDiagnostic(diagnostic);
            if (errorType == null)
            {
                // unknown diagnostic kind.
                // we don't provide tagging for unknown diagnostic kind. 
                //
                // it should be provided by the one who introduced the new diagnostic kind.
                return null;
            }

            return new ErrorTag(errorType, diagnostic.Message);
        }

        private string GetErrorTypeFromDiagnostic(DiagnosticData diagnostic)
        {
            if (diagnostic.IsSuppressed)
            {
                // Don't squiggle suppressed diagnostics.
                return null;
            }

            return GetErrorTypeFromDiagnosticTags(diagnostic) ??
                   GetErrorTypeFromDiagnosticSeverity(diagnostic);
        }

        private string GetErrorTypeFromDiagnosticTags(DiagnosticData diagnostic)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.EditAndContinue))
            {
                return EditAndContinueErrorTypeDefinition.Name;
            }

            return null;
        }

        private static string GetErrorTypeFromDiagnosticSeverity(DiagnosticData diagnostic)
        {
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    return PredefinedErrorTypeNames.SyntaxError;
                case DiagnosticSeverity.Warning:
                    return PredefinedErrorTypeNames.Warning;
                case DiagnosticSeverity.Info:
                    return null;
                case DiagnosticSeverity.Hidden:
                    if (diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
                    {
                        // This ensures that we have an 'invisible' squiggle (which will in turn
                        // display Quick Info on mouse hover) for the hidden diagnostics that we
                        // report for 'Remove Unnecessary Usings' and 'Simplify Type Name'. The
                        // presence of Quick Info pane for such squiggles allows platform
                        // to display Light Bulb for the corresponding fixes (per their current
                        // design platform can only display light bulb if Quick Info pane is present).
                        return PredefinedErrorTypeNames.Suggestion;
                    }

                    return null;
                default:
                    return PredefinedErrorTypeNames.OtherError;
            }
        }
    }
}
