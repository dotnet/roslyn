// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.StringIndentation;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.StringIndentation;

/// <summary>
/// This factory is called to create taggers that provide information about how strings are indented.
/// </summary>
[Export(typeof(IViewTaggerProvider))]
[TagType(typeof(StringIndentationTag))]
[VisualStudio.Utilities.ContentType(ContentTypeNames.CSharpContentType)]
[VisualStudio.Utilities.ContentType(ContentTypeNames.VisualBasicContentType)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class StringIndentationTaggerProvider(
    TaggerHost taggerHost,
    IEditorFormatMapService editorFormatMapService)
    : AsynchronousViewportTaggerProvider<StringIndentationTag>(taggerHost, FeatureAttribute.StringIndentation)
{
    private readonly IEditorFormatMap _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");

    protected override ImmutableArray<IOption2> Options { get; } = [StringIndentationOptionsStorage.StringIdentation];

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
        ITextView textView, ITextBuffer subjectBuffer)
    {
        // Note: we don't listen for OnTextChanged.  They'll get reported by the ViewSpan changing. 
        return TaggerEventSources.Compose(
            TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView),
            new EditorFormatMapChangedEventSource(_editorFormatMap));
    }

    protected override async Task ProduceTagsAsync(
        TaggerContext<StringIndentationTag> context, DocumentSnapshotSpan documentSnapshotSpan, CancellationToken cancellationToken)
    {
        var document = documentSnapshotSpan.Document;
        if (document == null)
            return;

        if (!GlobalOptions.GetOption(StringIndentationOptionsStorage.StringIdentation, document.Project.Language))
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
                    this,
                    _editorFormatMap,
                    region.OrderedHoleSpans.SelectAsArray(s => s.ToSnapshotSpan(snapshot)))));
        }
    }

    protected override bool TagEquals(StringIndentationTag tag1, StringIndentationTag tag2)
        => tag1.Equals(tag2);
}
