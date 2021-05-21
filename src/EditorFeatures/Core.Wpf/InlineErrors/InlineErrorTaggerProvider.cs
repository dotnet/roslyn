// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineErrors
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(InlineErrorTag))]
    internal class InlineErrorTaggerProvider : AbstractDiagnosticsAdornmentTaggerProvider<InlineErrorTag>
    {
        private readonly IEditorFormatMap _editorFormatMap;
        protected internal override bool IsEnabled => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineErrorTaggerProvider(
            IThreadingContext threadingContext,
            IEditorFormatMapService editorFormatMapService,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, diagnosticService, listenerProvider)
        {
            _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
        }

        protected override SnapshotSpan AdjustSnapshotSpan(SnapshotSpan span, int minimumLength)
        => AdjustSnapshotSpan(span, minimumLength, int.MaxValue);

        protected static new SnapshotSpan AdjustSnapshotSpan(SnapshotSpan span, int minimumLength, int maximumLength)
        {
            var snapshot = span.Snapshot;

            // new length
            var length = Math.Min(Math.Max(span.Length, minimumLength), maximumLength);

            // make sure start + length is smaller than snapshot.Length and start is >= 0
            var start = Math.Max(0, Math.Min(span.Start, snapshot.Length - length));

            // make sure length is smaller than snapshot.Length which can happen if start == 0
            return new SnapshotSpan(snapshot, start, Math.Min(start + length, snapshot.Length) - start);
        }

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
        {
            return
                (diagnostic.Severity == DiagnosticSeverity.Warning || diagnostic.Severity == DiagnosticSeverity.Error) &&
                !string.IsNullOrWhiteSpace(diagnostic.Message);
        }

        protected override InlineErrorTag? CreateTag(Workspace workspace, DiagnosticData diagnostic)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(diagnostic.Message));
            var errorType = GetErrorTypeFromDiagnostic(diagnostic);
            if (errorType is null)
            {
                return null;
            }

            return new InlineErrorTag(errorType, diagnostic, _editorFormatMap);
        }

        private static string? GetErrorTypeFromDiagnostic(DiagnosticData diagnostic)
        {
            if (diagnostic.IsSuppressed)
            {
                // Don't squiggle suppressed diagnostics.
                return null;
            }

            return GetErrorTypeFromDiagnosticTags(diagnostic) ??
                   GetErrorTypeFromDiagnosticSeverity(diagnostic);
        }

        private static string? GetErrorTypeFromDiagnosticTags(DiagnosticData diagnostic)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.EditAndContinue))
            {
                return EditAndContinueErrorTypeDefinition.Name;
            }

            return null;
        }

        private static string? GetErrorTypeFromDiagnosticSeverity(DiagnosticData diagnostic)
        {
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    return PredefinedErrorTypeNames.SyntaxError;
                case DiagnosticSeverity.Warning:
                    return PredefinedErrorTypeNames.Warning;
                default:
                    return null;
            }
        }
    }
}
