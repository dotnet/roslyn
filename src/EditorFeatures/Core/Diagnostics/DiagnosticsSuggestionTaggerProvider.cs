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
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(IErrorTag))]
    internal sealed partial class DiagnosticsSuggestionTaggerProvider :
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
            IDiagnosticAnalyzerService analyzerService,
            IGlobalOptionService globalOptions,
            [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, diagnosticService, analyzerService, globalOptions, visibilityTracker, listenerProvider)
        {
        }

        protected internal override bool IncludeDiagnostic(DiagnosticData diagnostic)
            => diagnostic.Severity == DiagnosticSeverity.Info;

        protected internal override bool SupportsDignosticMode(DiagnosticMode mode)
        {
            // We only support push diagnostics.  When pull diagnostics are on, ellipses suggestions are handled by the
            // lsp client.
            return mode == DiagnosticMode.Push;
        }

        protected override IErrorTag CreateTag(Workspace workspace, DiagnosticData diagnostic)
            => new RoslynErrorTag(PredefinedErrorTypeNames.HintedSuggestion, workspace, diagnostic);

        protected override SnapshotSpan AdjustSnapshotSpan(SnapshotSpan snapshotSpan)
        {
            // We always want suggestion tags to be two characters long.
            return AdjustSnapshotSpan(snapshotSpan, minimumLength: 2, maximumLength: 2);
        }

        protected override bool TagEquals(IErrorTag tag1, IErrorTag tag2)
        {
            Contract.ThrowIfFalse(tag1 is RoslynErrorTag);
            Contract.ThrowIfFalse(tag2 is RoslynErrorTag);
            return tag1.Equals(tag2);
        }
    }
}
