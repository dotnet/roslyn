// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.StringIndentation;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.StringIndentation
{
    /// <summary>
    /// This factory is called to create taggers that provide information about how strings are indented.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(StringIndentationTag))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.CSharpContentType)]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class StringIndentationTaggerProvider : AsynchronousTaggerProvider<StringIndentationTag>
    {
        private readonly IEditorFormatMap _editorFormatMap;

        protected override IEnumerable<PerLanguageOption2<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.StringIdentation);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StringIndentationTaggerProvider(
            IThreadingContext threadingContext,
            IEditorFormatMapService editorFormatMapService,
            IGlobalOptionService globalOptions,
            [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, globalOptions, visibilityTracker, listenerProvider.GetListener(FeatureAttribute.StringIndentation))
        {
            _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

        /// <summary>
        /// We want the span tracking mode to be inclusive here.  That way if the user types space here:
        /// 
        /// <code>
        /// var v = """
        ///            goo
        ///         """
        ///        ^ // here
        /// </code>
        /// 
        /// then the span of the tag will grow to the right and the line will immediately redraw in the correct position
        /// while we're in the process of recomputing the up to date tags.
        /// </summary>
        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeInclusive;

        protected override ITaggerEventSource CreateEventSource(
            ITextView? textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                new EditorFormatMapChangedEventSource(_editorFormatMap),
                TaggerEventSources.OnTextChanged(subjectBuffer));
        }

        protected override async Task ProduceTagsAsync(
            TaggerContext<StringIndentationTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition, CancellationToken cancellationToken)
        {
            var document = documentSnapshotSpan.Document;
            if (document == null)
                return;

            if (!GlobalOptions.GetOption(FeatureOnOffOptions.StringIdentation, document.Project.Language))
                return;

            var service = document.GetLanguageService<IStringIndentationService>();
            if (service == null)
                return;

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var regions = await service.GetStringIndentationRegionsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (regions.Length == 0)
                return;

            var snapshot = snapshotSpan.Snapshot;
            foreach (var region in regions)
            {
                var line = snapshot.GetLineFromPosition(region.IndentSpan.End);

                // If the indent is on the first column, then no need to actually show anything (plus we can't as we
                // want to draw one column earlier, and that column doesn't exist).
                if (line.Start == region.IndentSpan.End)
                    continue;

                context.AddTag(new TagSpan<StringIndentationTag>(
                    region.IndentSpan.ToSnapshotSpan(snapshot),
                    new StringIndentationTag(
                        _editorFormatMap,
                        region.OrderedHoleSpans.SelectAsArray(s => s.ToSnapshotSpan(snapshot)))));
            }
        }
    }
}
