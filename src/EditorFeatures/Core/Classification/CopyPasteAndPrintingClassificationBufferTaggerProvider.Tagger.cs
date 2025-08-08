// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification;

internal sealed partial class CopyPasteAndPrintingClassificationBufferTaggerProvider
{
    public sealed class Tagger : IAccurateTagger<IClassificationTag>, IDisposable
    {
        private readonly CopyPasteAndPrintingClassificationBufferTaggerProvider _owner;
        private readonly ITaggerEventSource _eventSource;
        private readonly IGlobalOptionService _globalOptions;

        // State for the tagger.  Can be accessed from any thread.  Access should be protected by _gate.

        private readonly object _gate = new();
        private TagSpanIntervalTree<IClassificationTag>? _cachedTags;
        private SnapshotSpan? _cachedTaggedSpan;

        public Tagger(
            CopyPasteAndPrintingClassificationBufferTaggerProvider owner,
            ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener,
            IGlobalOptionService globalOptions)
        {
            _owner = owner;
            _globalOptions = globalOptions;

            _eventSource = TaggerEventSources.Compose(
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, asyncListener),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer));

            _eventSource.Changed += OnEventSourceChanged;
            _eventSource.Connect();
        }

        // Explicitly a no-op.  This classifier does not support change notifications. See comment in
        // OnEventSourceChanged_OnForeground for more details.
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged { add { } remove { } }

        public void Dispose()
        {
            _owner._threadingContext.ThrowIfNotOnUIThread();
            _eventSource.Changed -= OnEventSourceChanged;
            _eventSource.Disconnect();
        }

        private void OnEventSourceChanged(object? sender, TaggerEventArgs _)
        {
            lock (_gate)
            {
                _cachedTags = null;
                _cachedTaggedSpan = null;
            }

            // Note: we explicitly do *not* call into TagsChanged here.  This type exists only for the copy/paste
            // scenario, and in the case the editor always calls into us for the span in question, ignoring
            // TagsChanged, as per DPugh:
            //
            //    For rich text copy, we always call the buffer classifier to get the classifications of the copied
            //    text. It ignores any tags changed events.
            //
            // It's important that we do not call TagsChanged here as the only thing we could do is notify that the
            // entire doc is changed, and that incurs a heavy cost for the editor reacting to that notification.
        }

        public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            // we never return any tags for GetTags.  This tagger is only for 'Accurate' scenarios.
            return [];
        }

        private static IEnumerable<TagSpan<IClassificationTag>> GetIntersectingTags(NormalizedSnapshotSpanCollection spans, TagSpanIntervalTree<IClassificationTag> cachedTags)
        {
            using var pooledObject = SegmentedListPool.GetPooledList<TagSpan<IClassificationTag>>(out var list);

            cachedTags.AddIntersectingTagSpans(spans, list);

            // Use yield return mechanism to allow the segmented list to get returned back to the
            // pool after usage. This does cause an allocation for the yield state machinery, but
            // that is better than not freeing a potentially large segmented list back to the pool.
            foreach (var item in list)
                yield return item;
        }

        IEnumerable<ITagSpan<IClassificationTag>> IAccurateTagger<IClassificationTag>.GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken)
            => GetAllTags(spans, cancellationToken);

        public IEnumerable<TagSpan<IClassificationTag>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken)
        {
            if (spans.Count == 0)
                return [];

            var snapshot = spans.First().Snapshot;

            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return [];

            // We want to classify from the start of the first requested span to the end of the 
            // last requested span.
            var spanToTag = new SnapshotSpan(snapshot, Span.FromBounds(spans.First().Start, spans.Last().End));

            var (cachedTaggedSpan, cachedTags) = GetCachedInfo();

            // We don't need to actually classify if what we're being asked for is a subspan
            // of the last classification we performed.
            if (cachedTaggedSpan?.Snapshot == snapshot &&
                cachedTaggedSpan.Value.Contains(spanToTag))
            {
                Contract.ThrowIfNull(cachedTags);
                return GetIntersectingTags(spans, cachedTags);
            }
            else
            {
                return ComputeAndCacheAllTags(spans, snapshot, document, spanToTag, cancellationToken);
            }
        }

        private IEnumerable<TagSpan<IClassificationTag>> ComputeAndCacheAllTags(
            NormalizedSnapshotSpanCollection spans,
            ITextSnapshot snapshot,
            Document document,
            SnapshotSpan spanToTag,
            CancellationToken cancellationToken)
        {
            var classificationService = document.GetRequiredLanguageService<IClassificationService>();

            // Our cache is not there, or is out of date.  We need to compute the up to date results.
            var options = _globalOptions.GetClassificationOptions(document.Project.Language);

            // Final list of tags to produce, containing syntax/semantic/embedded classification tags.
            using var _ = SegmentedListPool.GetPooledList<TagSpan<IClassificationTag>>(out var mergedTags);

            _owner._threadingContext.JoinableTaskFactory.Run(async () =>
            {
                // Defer to our helper which will compute syntax/semantic/embedded classifications, properly
                // layering them into the final result we return.
                await TotalClassificationAggregateTagger.AddTagsAsync(
                    new NormalizedSnapshotSpanCollection(spanToTag),
                    mergedTags,
                    // We should only be asking for a single span when getting the syntactic classifications
                    GetTaggingFunction(requireSingleSpan: true, (span, buffer) => classificationService.AddSyntacticClassificationsAsync(document, span, buffer, cancellationToken)),
                    // We should only be asking for a single span when getting the semantic classifications
                    GetTaggingFunction(requireSingleSpan: true, (span, buffer) => classificationService.AddSemanticClassificationsAsync(document, span, options, buffer, cancellationToken)),
                    //  Note: many string literal spans may be passed in when getting embedded classifications
                    GetTaggingFunction(requireSingleSpan: false, (span, buffer) => classificationService.AddEmbeddedLanguageClassificationsAsync(document, span, options, buffer, cancellationToken)),
                    arg: default).ConfigureAwait(false);
            });

            var cachedTags = new TagSpanIntervalTree<IClassificationTag>(snapshot, SpanTrackingMode.EdgeExclusive, mergedTags);
            lock (_gate)
            {
                _cachedTaggedSpan = spanToTag;
                _cachedTags = cachedTags;
            }

            return GetIntersectingTags(spans, cachedTags);

            Func<NormalizedSnapshotSpanCollection, SegmentedList<TagSpan<IClassificationTag>>, VoidResult, Task> GetTaggingFunction(
                bool requireSingleSpan, Func<TextSpan, SegmentedList<ClassifiedSpan>, Task> addTagsAsync)
            {
                Contract.ThrowIfTrue(requireSingleSpan && spans.Count != 1, "We should only be asking for a single span");
                return (spans, tempBuffer, _) => AddSpansAsync(spans, tempBuffer, addTagsAsync);
            }

            async Task AddSpansAsync(
                NormalizedSnapshotSpanCollection spans,
                SegmentedList<TagSpan<IClassificationTag>> result,
                Func<TextSpan, SegmentedList<ClassifiedSpan>, Task> addAsync)
            {
                // temp buffer we can use across all our classification calls.  Should be cleared between each call.
                using var _ = Classifier.GetPooledList(out var tempBuffer);

                foreach (var span in spans)
                {
                    tempBuffer.Clear();
                    await addAsync(span.Span.ToTextSpan(), tempBuffer).ConfigureAwait(false);

                    foreach (var classifiedSpan in tempBuffer)
                        result.Add(ClassificationUtilities.Convert(_owner._typeMap, snapshot, classifiedSpan));
                }
            }
        }

        private (SnapshotSpan? cachedTaggedSpan, TagSpanIntervalTree<IClassificationTag>? cachedTags) GetCachedInfo()
        {
            lock (_gate)
                return (_cachedTaggedSpan, _cachedTags);
        }
    }
}
