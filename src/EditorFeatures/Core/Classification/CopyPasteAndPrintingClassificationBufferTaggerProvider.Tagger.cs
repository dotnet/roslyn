// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification;

internal partial class CopyPasteAndPrintingClassificationBufferTaggerProvider
{
    private sealed class Tagger : IAccurateTagger<IClassificationTag>, IDisposable
    {
        private readonly CopyPasteAndPrintingClassificationBufferTaggerProvider _owner;
        private readonly ITextBuffer _subjectBuffer;
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
            _subjectBuffer = subjectBuffer;
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
            _owner._threadingContext.ThrowIfNotOnUIThread();

            // we never return any tags for GetTags.  This tagger is only for 'Accurate' scenarios.
            return [];
        }

        public IEnumerable<ITagSpan<IClassificationTag>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken)
        {
            _owner._threadingContext.ThrowIfNotOnUIThread();
            if (spans.Count == 0)
                return [];

            var firstSpan = spans.First();
            var snapshot = firstSpan.Snapshot;
            Debug.Assert(snapshot.TextBuffer == _subjectBuffer);

            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return [];

            var classificationService = document.GetLanguageService<IClassificationService>();
            if (classificationService == null)
                return [];

            // We want to classify from the start of the first requested span to the end of the 
            // last requested span.
            var spanToTag = new SnapshotSpan(snapshot, Span.FromBounds(spans.First().Start, spans.Last().End));

            GetCachedInfo(out var cachedTaggedSpan, out var cachedTags);

            // We don't need to actually classify if what we're being asked for is a subspan
            // of the last classification we performed.
            var canReuseCache =
                cachedTaggedSpan?.Snapshot == snapshot &&
                cachedTaggedSpan.Value.Contains(spanToTag);

            var options = _globalOptions.GetClassificationOptions(document.Project.Language);

            // temp buffer we can use across all our classification calls.  Should be cleared between each call.
            using var _1 = Classifier.GetPooledList(out var tempClassifiedSpans);

            if (!canReuseCache)
            {
                // Our cache is not there, or is out of date.  We need to compute the up to date results.

                // Final list of tags to produce, containing syntax/semantic/embedded classification tags.
                using var _2 = SegmentedListPool.GetPooledList<ITagSpan<IClassificationTag>>(out var mergedTags);

                _owner._threadingContext.JoinableTaskFactory.Run(async () =>
                {
                    // Defer to our helper which will compute syntax/semantic/embedded classifications, properly
                    // layering them into the final result we return.
                    await TotalClassificationAggregateTagger.AddTagsAsync(
                        new NormalizedSnapshotSpanCollection(spanToTag),
                        mergedTags,
                        AddSyntacticSpansAsync,
                        AddSemanticSpansAsync,
                        AddEmbeddedSpansAsync,
                        /*unused*/ false).ConfigureAwait(false);
                });

                cachedTaggedSpan = spanToTag;
                cachedTags = new TagSpanIntervalTree<IClassificationTag>(snapshot.TextBuffer, SpanTrackingMode.EdgeExclusive, mergedTags);

                lock (_gate)
                {
                    _cachedTaggedSpan = cachedTaggedSpan;
                    _cachedTags = cachedTags;
                }
            }

            return SegmentedListPool.ComputeList(
                static (args, tags) => args.cachedTags?.AddIntersectingTagSpans(args.spans, tags),
                (cachedTags, spans),
                _: (ITagSpan<IClassificationTag>?)null);

            async ValueTask AddSyntacticSpansAsync(NormalizedSnapshotSpanCollection spans, SegmentedList<ITagSpan<IClassificationTag>> result, bool _)
            {
                Contract.ThrowIfTrue(spans.Count != 1, "We should only be asking for a single span when getting the syntactic classifications");

                await AddSpansAsync(spans, result,
                    span => classificationService.AddSyntacticClassificationsAsync(document, span, tempClassifiedSpans, cancellationToken)).ConfigureAwait(false);
            }

            async ValueTask AddSemanticSpansAsync(NormalizedSnapshotSpanCollection spans, SegmentedList<ITagSpan<IClassificationTag>> result, bool _)
            {
                Contract.ThrowIfTrue(spans.Count != 1, "We should only be asking for a single span when getting the semantic classifications");

                await AddSpansAsync(spans, result,
                    span => classificationService.AddSemanticClassificationsAsync(document, span, options, tempClassifiedSpans, cancellationToken)).ConfigureAwait(false);
            }

            async ValueTask AddEmbeddedSpansAsync(NormalizedSnapshotSpanCollection stringLiteralSpans, SegmentedList<ITagSpan<IClassificationTag>> result, bool _)
            {
                // Note: many string literal spans may be passed in here.
                await AddSpansAsync(stringLiteralSpans, result,
                    span => classificationService.AddEmbeddedLanguageClassificationsAsync(document, span, options, tempClassifiedSpans, cancellationToken)).ConfigureAwait(false);
            }

            async ValueTask AddSpansAsync(
                NormalizedSnapshotSpanCollection spans,
                SegmentedList<ITagSpan<IClassificationTag>> result,
                Func<TextSpan, Task> addAsync)
            {
                Contract.ThrowIfTrue(tempClassifiedSpans.Count != 0);

                foreach (var span in spans)
                {
                    await addAsync(span.Span.ToTextSpan()).ConfigureAwait(false);

                    foreach (var classifiedSpan in tempClassifiedSpans)
                        result.Add(ClassificationUtilities.Convert(_owner._typeMap, snapshot, classifiedSpan));

                    tempClassifiedSpans.Clear();
                }
            }
        }

        private void GetCachedInfo(out SnapshotSpan? cachedTaggedSpan, out TagSpanIntervalTree<IClassificationTag>? cachedTags)
        {
            lock (_gate)
            {
                cachedTaggedSpan = _cachedTaggedSpan;
                cachedTags = _cachedTags;
            }
        }
    }
}
