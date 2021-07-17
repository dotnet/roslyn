﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class CopyPasteAndPrintingClassificationBufferTaggerProvider
    {
        private class Tagger : ForegroundThreadAffinitizedObject, IAccurateTagger<IClassificationTag>, IDisposable
        {
            private readonly CopyPasteAndPrintingClassificationBufferTaggerProvider _owner;
            private readonly ITextBuffer _subjectBuffer;
            private readonly ITaggerEventSource _eventSource;

            // State for the tagger.  Can be accessed from any thread.  Access should be protected by _gate.

            private readonly object _gate = new();
            private TagSpanIntervalTree<IClassificationTag>? _cachedTags;
            private SnapshotSpan? _cachedTaggedSpan;

            public Tagger(
                CopyPasteAndPrintingClassificationBufferTaggerProvider owner,
                ITextBuffer subjectBuffer,
                IAsynchronousOperationListener asyncListener)
                : base(owner.ThreadingContext)
            {
                _owner = owner;
                _subjectBuffer = subjectBuffer;

                // Note: because we use frozen-partial documents for semantic classification, we may end up with incomplete
                // semantics (esp. during solution load).  Because of this, we also register to hear when the full
                // compilation is available so that reclassify and bring ourselves up to date.
                _eventSource = new CompilationAvailableTaggerEventSource(
                    subjectBuffer,
                    asyncListener,
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
                this.AssertIsForeground();
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
                this.AssertIsForeground();

                // we never return any tags for GetTags.  This tagger is only for 'Accurate' scenarios.
                return Array.Empty<ITagSpan<IClassificationTag>>();
            }

            public IEnumerable<ITagSpan<IClassificationTag>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken)
            {
                this.AssertIsForeground();
                if (spans.Count == 0)
                    return Array.Empty<ITagSpan<IClassificationTag>>();

                var firstSpan = spans.First();
                var snapshot = firstSpan.Snapshot;
                Debug.Assert(snapshot.TextBuffer == _subjectBuffer);

                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                    return Array.Empty<ITagSpan<IClassificationTag>>();

                // We want to classify from the start of the first requested span to the end of the 
                // last requested span.
                var spanToTag = new SnapshotSpan(snapshot, Span.FromBounds(spans.First().Start, spans.Last().End));

                GetCachedInfo(out var cachedTaggedSpan, out var cachedTags);

                // We don't need to actually classify if what we're being asked for is a subspan
                // of the last classification we performed.
                var canReuseCache =
                    cachedTaggedSpan?.Snapshot == snapshot &&
                    cachedTaggedSpan.Value.Contains(spanToTag);

                if (!canReuseCache)
                {
                    // Our cache is not there, or is out of date.  We need to compute the up to date results.
                    var context = new TaggerContext<IClassificationTag>(document, snapshot, cancellationToken: cancellationToken);
                    this.ThreadingContext.JoinableTaskFactory.Run(
                        () => ProduceTagsAsync(context, new DocumentSnapshotSpan(document, spanToTag), _owner._typeMap));

                    cachedTaggedSpan = spanToTag;
                    cachedTags = new TagSpanIntervalTree<IClassificationTag>(snapshot.TextBuffer, SpanTrackingMode.EdgeExclusive, context.tagSpans);

                    lock (_gate)
                    {
                        _cachedTaggedSpan = cachedTaggedSpan;
                        _cachedTags = cachedTags;
                    }
                }

                return cachedTags == null
                    ? Array.Empty<ITagSpan<IClassificationTag>>()
                    : cachedTags.GetIntersectingTagSpans(spans);
            }

            private void GetCachedInfo(out SnapshotSpan? cachedTaggedSpan, out TagSpanIntervalTree<IClassificationTag>? cachedTags)
            {
                lock (_gate)
                {
                    cachedTaggedSpan = _cachedTaggedSpan;
                    cachedTags = _cachedTags;
                }
            }

            private static Task ProduceTagsAsync(TaggerContext<IClassificationTag> context, DocumentSnapshotSpan documentSpan, ClassificationTypeMap typeMap)
            {
                var classificationService = documentSpan.Document.GetLanguageService<IClassificationService>();
                return classificationService != null
                    ? SemanticClassificationUtilities.ProduceTagsAsync(context, documentSpan, classificationService, typeMap)
                    : Task.CompletedTask;
            }
        }
    }
}
