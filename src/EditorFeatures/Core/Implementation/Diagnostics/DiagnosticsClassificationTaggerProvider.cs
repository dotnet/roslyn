// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [TagType(typeof(ClassificationTag))]
    internal partial class DiagnosticsClassificationTaggerProvider : AbstractDiagnosticsTaggerProvider<ClassificationTag>
    {
        private static readonly IEnumerable<Option<bool>> s_tagSourceOptions = new[] { EditorComponentOnOffOptions.Tagger, InternalFeatureOnOffOptions.Classification, ServiceComponentOnOffOptions.DiagnosticProvider };

        private readonly ClassificationTypeMap _typeMap;
        private readonly ClassificationTag _classificationTag;

        protected internal override IEnumerable<Option<bool>> Options => s_tagSourceOptions;

        [ImportingConstructor]
        public DiagnosticsClassificationTaggerProvider(
            IDiagnosticService diagnosticService,
            ClassificationTypeMap typeMap,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> listeners)
            : base(diagnosticService, notificationService, new AggregateAsynchronousOperationListener(listeners, FeatureAttribute.Classification))
        {
            _typeMap = typeMap;
            _classificationTag = new ClassificationTag(_typeMap.GetClassificationType(ClassificationTypeDefinitions.UnnecessaryCode));
        }

        // if we are under high contrast mode, don't return anything.
        // this basically will make us not fade out in high contrast mode (ex, unused usings)
        protected internal override bool IsEnabled => !HighContrastChecker.IsHighContrast;

        protected internal override bool IncludeDiagnostic(DiagnosticData data) =>
            data.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary);

        protected internal override ITagSpan<ClassificationTag> CreateTagSpan(bool isLiveUpdate, SnapshotSpan span, DiagnosticData data) =>
            new TagSpan<ClassificationTag>(span, _classificationTag);
    }
}