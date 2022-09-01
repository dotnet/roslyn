// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(IErrorTag))]
    internal partial class DiagnosticsSuggestionTaggerProvider :
        AbstractDiagnosticsAdornmentTaggerProvider<IErrorTag>
    {
        private static readonly IEnumerable<Option2<bool>> s_tagSourceOptions =
            ImmutableArray.Create(EditorComponentOnOffOptions.Tagger, InternalFeatureOnOffOptions.Squiggles);

        protected override IEnumerable<Option2<bool>> Options => s_tagSourceOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticsSuggestionTaggerProvider(
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, diagnosticService, globalOptions, listenerProvider)
        {
        }

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
            => diagnostic.Severity == DiagnosticSeverity.Info;

        protected override IErrorTag CreateTag(Workspace workspace, DiagnosticData diagnostic)
            => new ErrorTag(
                PredefinedErrorTypeNames.HintedSuggestion,
                CreateToolTipContent(workspace, diagnostic));

        protected override SnapshotSpan AdjustSnapshotSpan(SnapshotSpan snapshotSpan, int minimumLength)
        {
            // We always want suggestion tags to be two characters long.
            return AdjustSnapshotSpan(snapshotSpan, minimumLength: 2, maximumLength: 2);
        }
    }
}
