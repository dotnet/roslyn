// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(SuggestionTag))]
    internal partial class DiagnosticsSuggestionTaggerProvider : 
        AbstractDiagnosticsAdornmentTaggerProvider<SuggestionTag>
    {
        private static readonly IEnumerable<Option<bool>> s_tagSourceOptions =
            ImmutableArray.Create(EditorComponentOnOffOptions.Tagger, InternalFeatureOnOffOptions.Squiggles, ServiceComponentOnOffOptions.DiagnosticProvider);

        private readonly IEditorFormatMap _editorFormatMap;

        protected internal override IEnumerable<Option<bool>> Options => s_tagSourceOptions;

        private readonly object _suggestionTagGate = new object();
        private SuggestionTag _suggestionTag;

        [ImportingConstructor]
        public DiagnosticsSuggestionTaggerProvider(
            IEditorFormatMapService editorFormatMapService,
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> listeners)
            : base(diagnosticService, notificationService, listeners)
        {
            _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            _editorFormatMap.FormatMappingChanged += OnFormatMappingChanged;
            _suggestionTag = new SuggestionTag(_editorFormatMap);
        }

        private void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            lock (_suggestionTagGate)
            {
                _suggestionTag = new SuggestionTag(_editorFormatMap);
            }
        }

        protected override ITaggerEventSource GetTaggerEventSource()
        {
            return TaggerEventSources.OnEditorFormatMapChanged(
                _editorFormatMap, TaggerDelay.NearImmediate);
        }

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
        {
            return diagnostic.Severity == DiagnosticSeverity.Info;
        }

        protected override SuggestionTag CreateTag(DiagnosticData diagnostic)
        {
            lock(_suggestionTagGate)
            {
                return _suggestionTag;
            }
        }

        protected override SnapshotSpan AdjustSnapshotSpan(SnapshotSpan snapshotSpan, int minimumLength)
        {
            snapshotSpan = base.AdjustSnapshotSpan(snapshotSpan, minimumLength);

            // Cap a suggestion line length at two characters.
            var span = snapshotSpan.Span;
            snapshotSpan = new SnapshotSpan(snapshotSpan.Snapshot,
                new Span(span.Start, Math.Min(span.Length, 2)));

            return snapshotSpan;
        }
    }
}