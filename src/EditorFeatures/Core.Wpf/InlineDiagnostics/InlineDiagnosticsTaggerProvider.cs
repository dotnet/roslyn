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
using Microsoft.CodeAnalysis.Workspaces;
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
            IDiagnosticAnalyzerService analyzerService,
            IGlobalOptionService globalOptions,
            [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListenerProvider listenerProvider,
            IEditorFormatMapService editorFormatMapService,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService)
            : base(threadingContext, diagnosticService, analyzerService, globalOptions, visibilityTracker, listenerProvider)
        {
            _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            _classificationFormatMapService = classificationFormatMapService;
            _classificationTypeRegistryService = classificationTypeRegistryService;
        }

        protected internal override bool SupportsDignosticMode(DiagnosticMode mode)
        {
            // We support inline diagnostics in both push and pull (since lsp doesn't support inline diagnostics yet).
            return true;
        }

        // Need to override this from AbstractDiagnosticsTaggerProvider because the location option needs to be added
        // to the TaggerEventSource, otherwise it does not get updated until there is a change in the editor.
        protected override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                base.CreateEventSource(textView, subjectBuffer),
                TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, InlineDiagnosticsOptions.Location));
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

        /// <summary>
        /// TODO: is there anything we can do better here? Inline diagnostic tags are not really data, but more UI
        /// elements with specific constrols, positions and events attached to them.  There doesn't seem to be a safe
        /// way to reuse any of these currently.  Ideally we could do something similar to inline-hints where there's a
        /// data tagger portion (which is async and has clean equality semantics), and then the UI portion which just
        /// translates those data-tags to the UI tags.
        /// <para>
        /// Doing direct equality means we'll always end up regenerating all tags.  But hopefully there won't be that
        /// many in a document to matter.
        /// </para>
        /// </summary>
        protected override bool TagEquals(InlineDiagnosticsTag tag1, InlineDiagnosticsTag tag2)
            => tag1 == tag2;
    }
}
