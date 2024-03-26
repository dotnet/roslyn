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
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
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

            var snapshot = spans.First().Snapshot;
            Debug.Assert(snapshot.TextBuffer == _subjectBuffer);

            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return [];

            // We want to classify from the start of the first requested span to the end of the 
            // last requested span.
            var spanToTag = TextSpan.FromBounds(spans.First().Start, spans.Last().End);

            var (cachedTaggedSpan, cachedTags) = GetCachedInfo();

            // We don't need to actually classify if what we're being asked for is a subspan
            // of the last classification we performed.
            if (cachedTaggedSpan?.Snapshot == snapshot &&
                cachedTaggedSpan.Value.Span.ToTextSpan().Contains(spanToTag))
            {
                Contract.ThrowIfNull(cachedTags);
                return SegmentedListPool<ITagSpan<IClassificationTag>>.ComputeList(
                    static (args, tags) => args.cachedTags.AddIntersectingTagSpans(args.spans, tags),
                    (cachedTags, spans));
            }

            // Our cache is not there, or is out of date.  We need to compute the up to date results.
            var options = _globalOptions.GetClassificationOptions(document.Project.Language);

            using var helper = new Helper(_owner, document, snapshot, spanToTag, options);

            _owner._threadingContext.JoinableTaskFactory.Run(() => helper.AddTagsAsync(cancellationToken));

            cachedTaggedSpan = spanToTag.ToSnapshotSpan(snapshot);
            cachedTags = new TagSpanIntervalTree<IClassificationTag>(
                snapshot.TextBuffer, SpanTrackingMode.EdgeExclusive, helper.MergedTags);

            lock (_gate)
            {
                _cachedTaggedSpan = cachedTaggedSpan;
                _cachedTags = cachedTags;
            }

            return SegmentedListPool<ITagSpan<IClassificationTag>>.ComputeList(
                static (args, tags) => args.cachedTags.AddIntersectingTagSpans(args.spans, tags),
                (cachedTags, spans));
        }

        private (SnapshotSpan? cachedTaggedSpan, TagSpanIntervalTree<IClassificationTag>? cachedTags) GetCachedInfo()
        {
            lock (_gate)
                return (_cachedTaggedSpan, _cachedTags);
        }

        private readonly struct Helper : IDisposable
        {
            private readonly CopyPasteAndPrintingClassificationBufferTaggerProvider _owner;
            private readonly Document _document;
            private readonly ITextSnapshot _snapshot;
            private readonly TextSpan _spanToTag;
            private readonly ClassificationOptions _options;
            private readonly IClassificationService _classificationService;
            private readonly PooledObject<SegmentedList<ClassifiedSpan>> _tempBuffer;
            private readonly PooledObject<SegmentedList<ITagSpan<IClassificationTag>>> _mergedTags;

            public Helper(
                CopyPasteAndPrintingClassificationBufferTaggerProvider owner,
                Document document,
                ITextSnapshot snapshot,
                TextSpan spanToTag,
                ClassificationOptions options)
            {
                _owner = owner;
                _document = document;
                _snapshot = snapshot;
                _options = options;
                _spanToTag = spanToTag;
                _classificationService = document.GetRequiredLanguageService<IClassificationService>();

                // temp buffer we can use across all our classification calls.  Should be cleared between each call.
                _tempBuffer = Classifier.GetPooledList(out _);

                // Final list of tags to produce, containing syntax/semantic/embedded classification tags.
                _mergedTags = SegmentedListPool.GetPooledList<ITagSpan<IClassificationTag>>(out _);
            }

            public void Dispose()
            {
                _tempBuffer.Dispose();
                _mergedTags.Dispose();
            }

            public SegmentedList<ITagSpan<IClassificationTag>> MergedTags => _mergedTags.Object;

            internal async Task AddTagsAsync(CancellationToken cancellationToken)
            {
                // Defer to our helper which will compute syntax/semantic/embedded classifications, properly
                // layering them into the final result we return.
                await TotalClassificationAggregateTagger.AddTagsAsync(
                    new NormalizedSnapshotSpanCollection(_spanToTag.ToSnapshotSpan(_snapshot)),
                    this.MergedTags,
                    AddSyntacticSpansAsync,
                    AddSemanticSpansAsync,
                    AddEmbeddedSpansAsync,
                    (this, cancellationToken)).ConfigureAwait(false);
            }

            private static async ValueTask AddSyntacticSpansAsync(
                NormalizedSnapshotSpanCollection spans,
                SegmentedList<ITagSpan<IClassificationTag>> result,
                (Helper helper, CancellationToken cancellationToken) arg)
            {
                Contract.ThrowIfTrue(spans.Count != 1, "We should only be asking for a single span when getting the syntactic classifications");

                await AddSpansAsync(spans, result,
                    static (helper, span, cancellationToken) =>
                        helper._classificationService.AddSyntacticClassificationsAsync(helper._document, span, helper._tempBuffer.Object, cancellationToken),
                    arg.helper, arg.cancellationToken).ConfigureAwait(false);
            }

            private async ValueTask AddSemanticSpansAsync(
                NormalizedSnapshotSpanCollection spans,
                SegmentedList<ITagSpan<IClassificationTag>> result,
                (Helper helper, CancellationToken cancellationToken) arg)
            {
                Contract.ThrowIfTrue(spans.Count != 1, "We should only be asking for a single span when getting the semantic classifications");

                await AddSpansAsync(
                    spans, result,
                    static (helper, span, cancellationToken) =>
                        helper._classificationService.AddSemanticClassificationsAsync(helper._document, span, helper._options, helper._tempBuffer.Object, cancellationToken),
                    arg.helper, arg.cancellationToken).ConfigureAwait(false);
            }

            private static async ValueTask AddEmbeddedSpansAsync(
                NormalizedSnapshotSpanCollection stringLiteralSpans,
                SegmentedList<ITagSpan<IClassificationTag>> result,
                (Helper helper, CancellationToken cancellationToken) arg)
            {
                // Note: many string literal spans may be passed in here.
                await AddSpansAsync(stringLiteralSpans, result,
                    static (helper, span, cancellationToken) => helper._classificationService.AddEmbeddedLanguageClassificationsAsync(
                        helper._document, span, helper._options, helper._tempBuffer.Object, cancellationToken),
                    arg.helper, arg.cancellationToken).ConfigureAwait(false);
            }

            private static async ValueTask AddSpansAsync(
                NormalizedSnapshotSpanCollection spans,
                SegmentedList<ITagSpan<IClassificationTag>> result,
                Func<Helper, TextSpan, CancellationToken, Task> addAsync,
                Helper helper,
                CancellationToken cancellationToken)
            {
                foreach (var span in spans)
                {
                    Contract.ThrowIfTrue(helper._tempBuffer.Object.Count != 0);
                    await addAsync(helper, span.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);

                    foreach (var classifiedSpan in helper._tempBuffer.Object)
                        result.Add(ClassificationUtilities.Convert(helper._owner._typeMap, helper._snapshot, classifiedSpan));

                    helper._tempBuffer.Object.Clear();
                }
            }
        }
    }
}
