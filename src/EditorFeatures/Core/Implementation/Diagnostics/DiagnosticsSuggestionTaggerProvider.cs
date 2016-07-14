// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
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
        protected internal override IEnumerable<Option<bool>> Options => s_tagSourceOptions;

        [ImportingConstructor]
        public DiagnosticsSuggestionTaggerProvider(
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> listeners)
            : base(diagnosticService, notificationService, listeners)
        {
        }

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
        {
            return diagnostic.Severity == DiagnosticSeverity.Info;
        }

        protected override SuggestionTag CreateTag(DiagnosticData diagnostic)
        {
            return SuggestionTag.Instance;
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