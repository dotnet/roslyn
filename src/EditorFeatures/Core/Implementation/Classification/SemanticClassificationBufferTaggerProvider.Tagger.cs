// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SemanticClassificationBufferTaggerProvider
    {
        private class Tagger : ForegroundThreadAffinitizedObject, IAccurateTagger<IClassificationTag>, IDisposable
        {
            private readonly SemanticClassificationBufferTaggerProvider _owner;
            private readonly ITextBuffer _subjectBuffer;
            private readonly ITaggerEventSource _eventSource;

            private TagSpanIntervalTree<IClassificationTag> _cachedTags_doNotAccessDirectly;
            private SnapshotSpan? _cachedTaggedSpan_doNotAccessDirectly;

            public Tagger(SemanticClassificationBufferTaggerProvider owner, ITextBuffer subjectBuffer)
                : base(owner.ThreadingContext)
            {
                _owner = owner;
                _subjectBuffer = subjectBuffer;

                const TaggerDelay Delay = TaggerDelay.Short;
                _eventSource = TaggerEventSources.Compose(
                    TaggerEventSources.OnSemanticChanged(subjectBuffer, Delay, _owner._semanticChangeNotificationService),
                    TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, Delay));

                ConnectToEventSource();
            }

            public void Dispose()
            {
                this.AssertIsForeground();
                _eventSource.Disconnect();
            }

            private TagSpanIntervalTree<IClassificationTag> CachedTags
            {
                get
                {
                    this.AssertIsForeground();
                    return _cachedTags_doNotAccessDirectly;
                }

                set
                {
                    this.AssertIsForeground();
                    _cachedTags_doNotAccessDirectly = value;
                }
            }

            private SnapshotSpan? CachedTaggedSpan
            {
                get
                {
                    this.AssertIsForeground();
                    return _cachedTaggedSpan_doNotAccessDirectly;
                }

                set
                {
                    this.AssertIsForeground();
                    _cachedTaggedSpan_doNotAccessDirectly = value;
                }
            }

            private void ConnectToEventSource()
            {
                _eventSource.Changed += (s, e) =>
                {
                    _owner._notificationService.RegisterNotification((Action)OnEventSourceChanged,
                        _owner._asyncListener.BeginAsyncOperation("SemanticClassificationBufferTaggerProvider"));
                };

                _eventSource.Connect();
            }

            private void OnEventSourceChanged()
            {
                this.AssertIsForeground();

                // When something changes, clear the cached data we have.
                this.CachedTags = null;
                this.CachedTaggedSpan = null;

                // And notify any concerned parties that we have new tags.
                this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(_subjectBuffer.CurrentSnapshot.GetFullSpan()));
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
                => GetTagsWorker(spans, CancellationToken.None);

            public IEnumerable<ITagSpan<IClassificationTag>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken)
                => GetTagsWorker(spans, cancellationToken);

            private IEnumerable<ITagSpan<IClassificationTag>> GetTagsWorker(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken)
            {
                this.AssertIsForeground();

                if (spans.Count == 0)
                {
                    return Array.Empty<ITagSpan<IClassificationTag>>();
                }

                var firstSpan = spans.First();
                var snapshot = firstSpan.Snapshot;
                Debug.Assert(snapshot.TextBuffer == _subjectBuffer);

                // We want to classify from the start of the first requested span to the end of the 
                // last requested span.
                var spanToTag = new SnapshotSpan(snapshot,
                    Span.FromBounds(spans.First().Start, spans.Last().End));

                // We don't need to actually classify if what we're being asked for is a subspan
                // of the last classification we performed.
                var cachedTaggedSpan = this.CachedTaggedSpan;
                var canReuseCache =
                    cachedTaggedSpan?.Snapshot == snapshot &&
                    cachedTaggedSpan.Value.Contains(spanToTag);

                if (!canReuseCache)
                {
                    // Our cache is not there, or is out of date.  We need to compute the up to date 
                    // results.

                    var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                    {
                        return Array.Empty<ITagSpan<IClassificationTag>>();
                    }

                    var context = new TaggerContext<IClassificationTag>(document, snapshot, cancellationToken: cancellationToken);
                    var task = ProduceTagsAsync(
                        context, new DocumentSnapshotSpan(document, spanToTag), _owner._typeMap);

                    task.Wait(cancellationToken);

                    CachedTaggedSpan = spanToTag;
                    CachedTags = new TagSpanIntervalTree<IClassificationTag>(snapshot.TextBuffer, SpanTrackingMode.EdgeExclusive, context.tagSpans);
                }

                if (this.CachedTags == null)
                {
                    return Array.Empty<ITagSpan<IClassificationTag>>();
                }

                return this.CachedTags.GetIntersectingTagSpans(spans);
            }

            private Task ProduceTagsAsync(TaggerContext<IClassificationTag> context, DocumentSnapshotSpan documentSpan, ClassificationTypeMap typeMap)
            {
                var document = documentSpan.Document;

                var classificationService = document.GetLanguageService<IClassificationService>();
                if (classificationService != null)
                {
                    return SemanticClassificationUtilities.ProduceTagsAsync(context, documentSpan, classificationService, typeMap);
                }

                return Task.CompletedTask;
            }
        }
    }
}
