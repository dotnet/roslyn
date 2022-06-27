// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(InlineDiagnosticsTag))]
    internal class InlineDiagnosticsTaggerProvider : AbstractDiagnosticsAdornmentTaggerProvider<InlineDiagnosticsTag>
    {
        private readonly IEditorFormatMap _editorFormatMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private readonly IClassificationTypeRegistryService _classificationTypeRegistryService;

        protected sealed override IEnumerable<PerLanguageOption2<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(InlineDiagnosticsOptions.EnableInlineDiagnostics);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineDiagnosticsTaggerProvider(
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            IEditorFormatMapService editorFormatMapService,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService)
            : base(threadingContext, diagnosticService, globalOptions, listenerProvider)
        {
            _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            _classificationFormatMapService = classificationFormatMapService;
            _classificationTypeRegistryService = classificationTypeRegistryService;
        }

        // Need to override this from AbstractDiagnosticsTaggerProvider because the location option needs to be added
        // to the TaggerEventSource, otherwise it does not get updated until there is a change in the editor.
        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                base.CreateEventSource(textViewOpt, subjectBuffer),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineDiagnosticsOptions.Location));
        }

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
        {
            return
                diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error &&
                !string.IsNullOrWhiteSpace(diagnostic.Message) &&
                !diagnostic.IsSuppressed;
        }

        protected override InlineDiagnosticsTag? CreateTag(Workspace workspace, DiagnosticData diagnostic)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(diagnostic.Message));
            var errorType = GetErrorTypeFromDiagnostic(diagnostic);
            if (errorType is null)
            {
                return null;
            }

            if (diagnostic.DocumentId is null)
            {
                return null;
            }

            var project = workspace.CurrentSolution.GetProject(diagnostic.DocumentId.ProjectId);
            if (project is null)
            {
                return null;
            }

            var locationOption = GlobalOptions.GetOption(InlineDiagnosticsOptions.Location, project.Language);
            var navigateService = workspace.Services.GetRequiredService<INavigateToLinkService>();
            return new InlineDiagnosticsTag(errorType, diagnostic, _editorFormatMap, _classificationFormatMapService,
                _classificationTypeRegistryService, locationOption, navigateService);
        }

        private static string? GetErrorTypeFromDiagnostic(DiagnosticData diagnostic)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                return diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.EditAndContinue)
                    ? EditAndContinueErrorTypeDefinition.Name
                    : PredefinedErrorTypeNames.SyntaxError;
            }
            else if (diagnostic.Severity == DiagnosticSeverity.Warning)
            {
                return PredefinedErrorTypeNames.Warning;
            }
            else
            {
                return null;
            }
        }
    }
}
