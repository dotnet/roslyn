// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(IErrorTag))]
    internal partial class DiagnosticsSuggestionTaggerProvider :
        AbstractDiagnosticsAdornmentTaggerProvider<IErrorTag>
    {
        private static readonly IEnumerable<Option<bool>> s_tagSourceOptions =
            ImmutableArray.Create(EditorComponentOnOffOptions.Tagger, InternalFeatureOnOffOptions.Squiggles, ServiceComponentOnOffOptions.DiagnosticProvider);

        protected override IEnumerable<Option<bool>> Options => s_tagSourceOptions;

        [ImportingConstructor]
        public DiagnosticsSuggestionTaggerProvider(
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, diagnosticService, notificationService, listenerProvider)
        {
        }

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
            => diagnostic.Severity == DiagnosticSeverity.Info;

        protected override IErrorTag CreateTag(DiagnosticData diagnostic) =>
            new ErrorTag(PredefinedErrorTypeNames.HintedSuggestion, diagnostic.Message);

        protected override SnapshotSpan AdjustSnapshotSpan(SnapshotSpan snapshotSpan, int minimumLength)
        {
            // We always want suggestion tags to be two characters long.
            return base.AdjustSnapshotSpan(snapshotSpan, minimumLength: 2, maximumLength: 2);
        }
    }
}
