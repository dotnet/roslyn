// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Features.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(ClassificationTag))]
    internal sealed partial class DiagnosticsClassificationTaggerProvider : AbstractDiagnosticsTaggerProvider<ClassificationTag>
    {
        private readonly ClassificationTypeMap _typeMap;
        private readonly ClassificationTag _classificationTag;
        private readonly EditorOptionsService _editorOptionsService;

        protected sealed override ImmutableArray<IOption2> Options { get; } = ImmutableArray.Create<IOption2>(DiagnosticsOptionsStorage.Classification);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticsClassificationTaggerProvider(
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IDiagnosticAnalyzerService analyzerService,
            ClassificationTypeMap typeMap,
            EditorOptionsService editorOptionsService,
            [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, diagnosticService, analyzerService, editorOptionsService.GlobalOptions, visibilityTracker, listenerProvider.GetListener(FeatureAttribute.Classification))
        {
            _typeMap = typeMap;
            _classificationTag = new ClassificationTag(_typeMap.GetClassificationType(ClassificationTypeDefinitions.UnnecessaryCode));
            _editorOptionsService = editorOptionsService;
        }

        // If we are under high contrast mode, the editor ignores classification tags that fade things out,
        // because that reduces contrast. Since the editor will ignore them, there's no reason to produce them.
        protected sealed override bool IsEnabled
            => !_editorOptionsService.Factory.GlobalOptions.GetOptionValue(DefaultTextViewHostOptions.IsInContrastModeId);

        protected sealed override bool SupportsDiagnosticMode(DiagnosticMode mode)
        {
            // We only support solution crawler push diagnostics.  When lsp pull diagnostics are on, diagnostic fading
            // is handled by the lsp client.
            return mode == DiagnosticMode.SolutionCrawlerPush;
        }

        protected sealed override bool IncludeDiagnostic(DiagnosticData data)
        {
            if (!data.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
            {
                return false;
            }

            // Do not fade if user has disabled the fading option corresponding to this diagnostic.
            if (IDEDiagnosticIdToOptionMappingHelper.TryGetMappedFadingOption(data.Id, out var fadingOption))
            {
                return data.Language != null
                    && _editorOptionsService.GlobalOptions.GetOption(fadingOption, data.Language);
            }

            return true;
        }

        protected sealed override ITagSpan<ClassificationTag> CreateTagSpan(Workspace workspace, SnapshotSpan span, DiagnosticData data)
            => new TagSpan<ClassificationTag>(span, _classificationTag);

        protected sealed override ImmutableArray<DiagnosticDataLocation> GetLocationsToTag(DiagnosticData diagnosticData)
        {
            if (diagnosticData.TryGetUnnecessaryDataLocations(out var locationsToTag))
            {
                return locationsToTag.Value;
            }

            // Default to the base implementation for the diagnostic data
            return base.GetLocationsToTag(diagnosticData);
        }

        protected sealed override bool TagEquals(ClassificationTag tag1, ClassificationTag tag2)
            => tag1.ClassificationType.Classification == tag2.ClassificationType.Classification;
    }
}
