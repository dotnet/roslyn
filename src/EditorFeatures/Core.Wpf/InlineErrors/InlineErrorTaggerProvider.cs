// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineErrors
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(InlineErrorTag))]
    internal class InlineErrorTaggerProvider : AbstractDiagnosticsAdornmentTaggerProvider<InlineErrorTag>
    {
        private readonly IEditorFormatMap _editorFormatMap;
        protected override IEnumerable<PerLanguageOption2<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.InlineErrors);

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

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
        {
            return
                diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error &&
                !string.IsNullOrWhiteSpace(diagnostic.Message) && !diagnostic.IsSuppressed;
        }

        /// <summary>
        /// Creates the InlineErrorTag with the error distinction
        /// </summary>
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
