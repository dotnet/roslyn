using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

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
            private ITextSnapshot _cachedSnapshot_doNotAccessDirectly;

            private IEditorClassificationService _classificationService;

            public Tagger(SemanticClassificationBufferTaggerProvider owner, ITextBuffer subjectBuffer)
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

            private ITextSnapshot CachedSnapshot
            {
                get
                {
                    this.AssertIsForeground();
                    return _cachedSnapshot_doNotAccessDirectly;
                }

                set
                {
                    this.AssertIsForeground();
                    _cachedSnapshot_doNotAccessDirectly = value;
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
                this.CachedSnapshot = null;

                // And notify any concerned parties that we have new tags.
                this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(_subjectBuffer.CurrentSnapshot.GetFullSpan()));
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

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
                {
                    return Array.Empty<ITagSpan<IClassificationTag>>();
                }

                var firstSpan = spans.First();
                var snapshot = firstSpan.Snapshot;
                Debug.Assert(snapshot.TextBuffer == _subjectBuffer);

                var cachedSnapshot = this.CachedSnapshot;

                if (snapshot != cachedSnapshot)
                {
                    // Our cache is not there, or is out of date.  We need to compute the up to date 
                    // results.

                    var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                    {
                        return Array.Empty<ITagSpan<IClassificationTag>>();
                    }

                    _classificationService = _classificationService ?? document.Project.LanguageServices.GetService<IEditorClassificationService>();

                    var context = new TaggerContext<IClassificationTag>(document, snapshot, cancellationToken: cancellationToken);
                    var spanToTag = new DocumentSnapshotSpan(document, snapshot.GetFullSpan());
                    var task = SemanticClassificationUtilities.ProduceTagsAsync(context, spanToTag, _classificationService, _owner._typeMap);
                    task.Wait(cancellationToken);

                    CachedSnapshot = snapshot;
                    CachedTags = new TagSpanIntervalTree<IClassificationTag>(snapshot.TextBuffer, SpanTrackingMode.EdgeExclusive, context.tagSpans);
                }

                if (this.CachedTags == null)
                {
                    return Array.Empty<ITagSpan<IClassificationTag>>();
                }

                return this.CachedTags.GetIntersectingTagSpans(spans);
            }
        }
    }
}
