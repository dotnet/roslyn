// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.BracePairs;

#pragma warning disable CS0618 // IBracePairTag is obsolete while editor works on this API.
[Export(typeof(IViewTaggerProvider))]
[VisualStudio.Utilities.Name(nameof(BracePairsTaggerProvider))]
[VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
[TagType(typeof(IBracePairTag))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class BracePairsTaggerProvider(
    IThreadingContext threadingContext,
    IGlobalOptionService globalOptionService,
    [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
    IAsynchronousOperationListenerProvider listenerProvider) : AsynchronousViewportTaggerProvider<IBracePairTag>(threadingContext,
          globalOptionService,
          visibilityTracker,
          listenerProvider.GetListener(FeatureAttribute.BracePairs))
{
    protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

    protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
    {
        return TaggerEventSources.Compose(
            TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView),
            TaggerEventSources.OnTextChanged(subjectBuffer),
            TaggerEventSources.OnParseOptionChanged(subjectBuffer));
    }

    protected override async Task ProduceTagsAsync(TaggerContext<IBracePairTag> context, DocumentSnapshotSpan spanToTag, CancellationToken cancellationToken)
    {
        var document = spanToTag.Document;
        if (document is null)
            return;

        var service = document.GetLanguageService<IBracePairsService>();
        if (service is null)
            return;

        using var _ = ArrayBuilder<BracePairData>.GetInstance(out var bracePairs);

        await service.AddBracePairsAsync(
            document, spanToTag.SnapshotSpan.Span.ToTextSpan(), bracePairs, cancellationToken).ConfigureAwait(false);

        var snapshot = spanToTag.SnapshotSpan.Snapshot;
        foreach (var bracePair in bracePairs)
        {
            var start = CreateSnapshotSpan(bracePair.Start, snapshot);
            var end = CreateSnapshotSpan(bracePair.End, snapshot);
            if (start is null && end is null)
                continue;

            context.AddTag(new TagSpan<IBracePairTag>(
                new SnapshotSpan(snapshot, Span.FromBounds(bracePair.Start.Start, bracePair.End.End)),
                new BracePairTag(start, end)));
        }

        return;

        static SnapshotSpan? CreateSnapshotSpan(TextSpan span, ITextSnapshot snapshot)
            => span.IsEmpty ? null : span.ToSnapshotSpan(snapshot);
    }

    protected override bool TagEquals(IBracePairTag tag1, IBracePairTag tag2)
    {
        if (tag1 is null && tag2 is null)
            return true;

        if (tag1 is null || tag2 is null)
            return false;

        return SpanEquals(tag1.Start, tag2.Start) &&
               SpanEquals(tag1.End, tag2.End);
    }
}
#pragma warning restore CS0618 // Type or member is obsolete

