// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private sealed partial class Tagger : IAccurateTagger<TTag>, IDisposable
        {
            /// <summary>
            /// If we get more than this many differences, then we just issue it as a single change
            /// notification.  The number has been completely made up without any data to support it.
            /// 
            /// Internal for testing purposes.
            /// </summary>
            private const int CoalesceDifferenceCount = 10;

            #region Fields that can be accessed from either thread

            private readonly ITextBuffer _subjectBuffer;

            private readonly TagSource _tagSource;

            #endregion

            #region Fields that can only be accessed from the foreground thread

            /// <summary>
            /// The batch change notifier that we use to throttle update to the UI.
            /// </summary>
            private readonly BatchChangeNotifier _batchChangeNotifier;

            #endregion

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public Tagger(
                IAsynchronousOperationListener listener,
                IForegroundNotificationService notificationService,
                TagSource tagSource,
                ITextBuffer subjectBuffer)
            {
                Contract.ThrowIfNull(subjectBuffer);

                _subjectBuffer = subjectBuffer;
                _batchChangeNotifier = new BatchChangeNotifier(
                    subjectBuffer, listener, notificationService, NotifyEditorNow);

                _tagSource = tagSource;

                _tagSource.OnTaggerAdded(this);
                _tagSource.TagsChangedForBuffer += OnTagsChangedForBuffer;
                _tagSource.Paused += OnPaused;
                _tagSource.Resumed += OnResumed;
            }

            public void Dispose()
            {
                _tagSource.Resumed -= OnResumed;
                _tagSource.Paused -= OnPaused;
                _tagSource.TagsChangedForBuffer -= OnTagsChangedForBuffer;
                _tagSource.OnTaggerDisposed(this);
            }

            private void OnPaused(object sender, EventArgs e)
                => _batchChangeNotifier.Pause();

            private void OnResumed(object sender, EventArgs e)
                => _batchChangeNotifier.Resume();

            private void OnTagsChangedForBuffer(ICollection<KeyValuePair<ITextBuffer, DiffResult>> changes)
            {
                _tagSource.AssertIsForeground();

                // Note: This operation is uncancellable. Once we've been notified here, our cached tags
                // in the tag source are new. If we don't update the UI of the editor then we will end
                // up in an inconsistent state between us and the editor where we have new tags but the
                // editor will never know.

                foreach (var change in changes)
                {
                    if (change.Key != _subjectBuffer)
                    {
                        continue;
                    }

                    // Now report them back to the UI on the main thread.

                    // We ask to update UI immediately for removed tags
                    NotifyEditors(change.Value.Removed, _tagSource.RemovedTagNotificationDelay);
                    NotifyEditors(change.Value.Added, _tagSource.AddedTagNotificationDelay);
                }
            }

            private void NotifyEditors(NormalizedSnapshotSpanCollection changes, TaggerDelay delay)
            {
                _tagSource.AssertIsForeground();

                if (changes.Count == 0)
                {
                    // nothing to do.
                    return;
                }

                if (delay == TaggerDelay.NearImmediate)
                {
                    // if delay is immediate, we let notifier knows about the change right away
                    _batchChangeNotifier.EnqueueChanges(changes);
                    return;
                }

                // if delay is anything more than that, we let notifier knows about the change after given delay
                // event notification is not cancellable.
                _tagSource.RegisterNotification(() => _batchChangeNotifier.EnqueueChanges(changes), (int)delay.ComputeTimeDelay(_subjectBuffer).TotalMilliseconds, CancellationToken.None);
            }

            private void NotifyEditorNow(NormalizedSnapshotSpanCollection normalizedSpans)
            {
                _batchChangeNotifier.AssertIsForeground();

                using (Logger.LogBlock(FunctionId.Tagger_BatchChangeNotifier_NotifyEditorNow, CancellationToken.None))
                {
                    if (normalizedSpans.Count == 0)
                    {
                        return;
                    }

                    var tagsChanged = this.TagsChanged;
                    if (tagsChanged == null)
                    {
                        return;
                    }

                    normalizedSpans = CoalesceSpans(normalizedSpans);

                    // Don't use linq here.  It's a hotspot.
                    foreach (var span in normalizedSpans)
                    {
                        tagsChanged(this, new SnapshotSpanEventArgs(span));
                    }
                }
            }

            internal static NormalizedSnapshotSpanCollection CoalesceSpans(NormalizedSnapshotSpanCollection normalizedSpans)
            {
                var snapshot = normalizedSpans.First().Snapshot;

                // Coalesce the spans if there are a lot of them.
                if (normalizedSpans.Count > CoalesceDifferenceCount)
                {
                    // Spans are normalized.  So to find the whole span we just go from the
                    // start of the first span to the end of the last span.
                    normalizedSpans = new NormalizedSnapshotSpanCollection(snapshot.GetSpanFromBounds(
                        normalizedSpans.First().Start,
                        normalizedSpans.Last().End));
                }

                return normalizedSpans;
            }

            public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection requestedSpans)
                => GetTagsWorker(requestedSpans, accurate: false, cancellationToken: CancellationToken.None);

            public IEnumerable<ITagSpan<TTag>> GetAllTags(NormalizedSnapshotSpanCollection requestedSpans, CancellationToken cancellationToken)
                => GetTagsWorker(requestedSpans, accurate: true, cancellationToken: cancellationToken);

            private IEnumerable<ITagSpan<TTag>> GetTagsWorker(
                NormalizedSnapshotSpanCollection requestedSpans,
                bool accurate,
                CancellationToken cancellationToken)
            {
                if (requestedSpans.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
                }

                var buffer = requestedSpans.First().Snapshot.TextBuffer;
                var tags = accurate
                    ? _tagSource.GetAccurateTagIntervalTreeForBuffer(buffer, cancellationToken)
                    : _tagSource.TryGetTagIntervalTreeForBuffer(buffer);

                if (tags == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
                }

                return tags.GetIntersectingTagSpans(requestedSpans);
            }
        }
    }
}