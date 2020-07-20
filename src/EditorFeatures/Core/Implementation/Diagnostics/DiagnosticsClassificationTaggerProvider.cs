// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(ClassificationTag))]
    internal partial class DiagnosticsClassificationTaggerProvider : AbstractDiagnosticsTaggerProvider<ClassificationTag>
    {
        private static readonly IEnumerable<Option2<bool>> s_tagSourceOptions = new[] { EditorComponentOnOffOptions.Tagger, InternalFeatureOnOffOptions.Classification, ServiceComponentOnOffOptions.DiagnosticProvider };

        private readonly ClassificationTypeMap _typeMap;
        private readonly ClassificationTag _classificationTag;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;

        protected override IEnumerable<Option2<bool>> Options => s_tagSourceOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticsClassificationTaggerProvider(
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            ClassificationTypeMap typeMap,
            IForegroundNotificationService notificationService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, diagnosticService, notificationService, listenerProvider.GetListener(FeatureAttribute.Classification))
        {
            _typeMap = typeMap;
            _classificationTag = new ClassificationTag(_typeMap.GetClassificationType(ClassificationTypeDefinitions.UnnecessaryCode));
            _editorOptionsFactoryService = editorOptionsFactoryService;
        }

        // If we are under high contrast mode, the editor ignores classification tags that fade things out,
        // because that reduces contrast. Since the editor will ignore them, there's no reason to produce them.
        protected internal override bool IsEnabled
            => !_editorOptionsFactoryService.GlobalOptions.GetOptionValue(DefaultTextViewHostOptions.IsInContrastModeId);

        protected internal override bool IncludeDiagnostic(DiagnosticData data)
            => data.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary);

        protected internal override ITagSpan<ClassificationTag> CreateTagSpan(Workspace workspace, bool isLiveUpdate, SnapshotSpan span, DiagnosticData data)
            => new TagSpan<ClassificationTag>(span, _classificationTag);

        protected internal override ImmutableArray<DiagnosticDataLocation> GetLocationsToTag(DiagnosticData diagnosticData)
        {
            // If there are 'unnecessary' locations specified in the property bag, use those instead of the main diagnostic location.
            if (diagnosticData.AdditionalLocations?.Count > 0
                && diagnosticData.Properties != null
                && diagnosticData.Properties.TryGetValue(WellKnownDiagnosticTags.Unnecessary, out var unnecessaryIndices)
                && unnecessaryIndices is object)
            {
                using var _ = PooledObjects.ArrayBuilder<DiagnosticDataLocation>.GetInstance(out var locationsToTag);

                var additionalLocations = diagnosticData.AdditionalLocations.ToImmutableArray();
                foreach (var index in GetLocationIndices(unnecessaryIndices))
                    locationsToTag.Add(additionalLocations[index]);

                return locationsToTag.ToImmutable();
            }

            // Default to the base implementation for the diagnostic data
            return base.GetLocationsToTag(diagnosticData);

            static IEnumerable<int> GetLocationIndices(string indicesProperty)
            {
                try
                {
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(indicesProperty));
                    var serializer = new DataContractJsonSerializer(typeof(IEnumerable<int>));
                    var result = serializer.ReadObject(stream) as IEnumerable<int>;
                    return result ?? Array.Empty<int>();
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                    return ImmutableArray<int>.Empty;
                }
            }
        }
    }
}
