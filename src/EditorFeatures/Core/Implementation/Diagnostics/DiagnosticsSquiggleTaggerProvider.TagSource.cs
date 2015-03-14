// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal partial class DiagnosticsSquiggleTaggerProvider
    {
        internal class TagSource : AbstractAggregatedDiagnosticsTagSource<IErrorTag>
        {
            private readonly bool _blueSquiggleForBuildDiagnostic;

            public TagSource(
                ITextBuffer subjectBuffer,
                IForegroundNotificationService notificationService,
                DiagnosticService service,
                IOptionService optionService,
                IAsynchronousOperationListener asyncListener)
                : base(subjectBuffer, notificationService, service, asyncListener)
            {
                _blueSquiggleForBuildDiagnostic = optionService.GetOption(InternalDiagnosticsOptions.BlueSquiggleForBuildDiagnostic);
            }

            protected override int MinimumLength
            {
                get
                {
                    return 1;
                }
            }

            protected override bool ShouldInclude(DiagnosticData diagnostic)
            {
                var isUnnecessary = (diagnostic.Severity == DiagnosticSeverity.Hidden && diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary));

                return
                    (diagnostic.Severity == DiagnosticSeverity.Warning || diagnostic.Severity == DiagnosticSeverity.Error || isUnnecessary) &&
                    !string.IsNullOrWhiteSpace(diagnostic.Message);
            }

            protected override TagSpan<IErrorTag> CreateTagSpan(SnapshotSpan span, DiagnosticData diagnostic)
            {
                Contract.Requires(!string.IsNullOrWhiteSpace(diagnostic.Message));
                var errorType = GetErrorTypeFromDiagnostic(diagnostic);
                if (errorType == null)
                {
                    // unknown diagnostic kind.
                    // we don't provide tagging for unknown diagnostic kind. 
                    //
                    // it should be provided by the one who introduced the new diagnostic kind.
                    return null;
                }

                return new TagSpan<IErrorTag>(span, new ErrorTag(errorType, diagnostic.Message));
            }

            private string GetErrorTypeFromDiagnostic(DiagnosticData diagnostic)
            {
                return GetErrorTypeFromDiagnosticTags(diagnostic) ??
                       GetErrorTypeFromDiagnosticProperty(diagnostic) ??
                       GetErrorTypeFromDiagnosticSeverity(diagnostic);
            }

            private string GetErrorTypeFromDiagnosticProperty(DiagnosticData diagnostic)
            {
                if (diagnostic.Properties.Count == 0)
                {
                    return null;
                }

                string value;
                if (!diagnostic.Properties.TryGetValue(WellKnownDiagnosticPropertyNames.Origin, out value))
                {
                    return null;
                }

                if (value == WellKnownDiagnosticTags.Build && _blueSquiggleForBuildDiagnostic)
                {
                    return PredefinedErrorTypeNames.CompilerError;
                }

                return null;
            }

            private string GetErrorTypeFromDiagnosticTags(DiagnosticData diagnostic)
            {
                if (diagnostic.CustomTags.Count <= 1)
                {
                    return null;
                }

                switch (diagnostic.CustomTags[0])
                {
                    case WellKnownDiagnosticTags.EditAndContinue:
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
                            // presence of Quick Info pane for such squiggles allows allows platform
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
}
