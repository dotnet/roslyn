// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        /// <summary>
        /// Handles the job of batching up change notifications so that don't spam the editor with too
        /// many update requests at a time.  Updating the editor can even be paused and resumed at a
        /// later point if some feature doesn't want the editor changing while it performs some bit of
        /// work.
        /// </summary>
        private class BatchChangeNotifier : ForegroundThreadAffinitizedObject
        {
            private readonly ITextBuffer _subjectBuffer;

            /// <summary>
            /// If we get more than this many differences, then we just issue it as a single change
            /// notification.  The number has been completely made up without any data to support it.
            /// 
            /// Internal for testing purposes.
            /// </summary>
            internal const int CoalesceDifferenceCount = 10;

            /// <summary>
            /// The worker we use to do work on the appropriate background or foreground thread.
            /// </summary>
            private readonly IAsynchronousOperationListener _listener;
            private readonly IForegroundNotificationService _notificationService;

            /// <summary>
            /// We keep track of the last time we reported a span, so that if things have been idle for
            /// a while, we don't unnecessarily delay the reporting, but if things are busy, we'll start
            /// to throttle the notifications.
            /// </summary>
            private long _lastReportTick;

            // In general, we want IDE services to avoid reporting changes to the editor too rapidly.
            // When we do,  we diminish performance by choking the UI thread with lots of update
            // operations. To help alleviate that, we don't immediately report changes to the UI.  We
            // instead create a timer that will report the changes and we enqueue any pending updates to
            // a list that will be updated all at once the timer actually runs.
            private bool _notificationRequestEnqueued;
            private readonly SortedDictionary<int, NormalizedSnapshotSpanCollection> _snapshotVersionToSpansMap =
                new SortedDictionary<int, NormalizedSnapshotSpanCollection>();

            /// <summary>
            /// True if we are currently suppressing UI updates.  While suppressed we still continue
            /// doing everything as normal, except we do not update the UI.  Then, when we are no longer
            /// suppressed we will issue all pending UI notifications to the editor.  During the time
            /// that we're suppressed we will respond to all GetTags requests with the tags we had
            /// before we were paused.
            /// </summary>
            public bool IsPaused { get; private set; }
            private int _lastPausedTime;

            private readonly Action<SnapshotSpan> _reportChangedSpan;

            public BatchChangeNotifier(
                ITextBuffer subjectBuffer,
                IAsynchronousOperationListener listener,
                IForegroundNotificationService notificationService,
                Action<SnapshotSpan> reportChangedSpan)
            {
                Contract.ThrowIfNull(reportChangedSpan);
                _subjectBuffer = subjectBuffer;
                _listener = listener;
                _notificationService = notificationService;
                _reportChangedSpan = reportChangedSpan;
            }

            public void Pause()
            {
                AssertIsForeground();

                _lastPausedTime = Environment.TickCount;
                this.IsPaused = true;
            }

            public void Resume()
            {
                AssertIsForeground();
                _lastPausedTime = Environment.TickCount;
                this.IsPaused = false;
            }

            private static readonly Func<int, NormalizedSnapshotSpanCollection> s_addFunction =
                _ => new NormalizedSnapshotSpanCollection();

            internal void EnqueueChanges(
                NormalizedSnapshotSpanCollection changedSpans)
            {
                AssertIsForeground();
                if (changedSpans.Count == 0)
                {
                    return;
                }

                var snapshot = changedSpans.First().Snapshot;

                var version = snapshot.Version.VersionNumber;
                var currentSpans = _snapshotVersionToSpansMap.GetOrAdd(version, s_addFunction);
                var allSpans = NormalizedSnapshotSpanCollection.Union(currentSpans, changedSpans);
                _snapshotVersionToSpansMap[version] = allSpans;

                EnqueueNotificationRequest(TaggerDelay.NearImmediate);
            }

            // We may get a flurry of 'Notify' calls if we've enqueued a lot of work and it's now just
            // completed.  Batch up all the notifications so we can tell the editor about them at the
            // same time.
            private void EnqueueNotificationRequest(
                TaggerDelay delay)
            {
                AssertIsForeground();

                if (_notificationRequestEnqueued)
                {
                    // we already have a pending task to update the UI.  No need to do anything at this
                    // point.
                    return;
                }

                var currentTick = Environment.TickCount;
                if (Math.Abs(currentTick - _lastReportTick) > TaggerDelay.NearImmediate.ComputeTimeDelay(_subjectBuffer).TotalMilliseconds)
                {
                    _lastReportTick = currentTick;
                    this.NotifyEditor();
                }
                else
                {
                    // enqueue a task to update the UI with all the changed spans at some time in the
                    // future.
                    _notificationRequestEnqueued = true;

                    // Note: this operation is uncancellable.  We already updated our internal state in
                    // RecomputeTags. We must eventually notify the editor about these changes so that the
                    // UI reaches parity with our internal model.  Also, if we cancel it, then
                    // 'reportTagsScheduled' will stay 'true' forever and we'll never notify the UI.
                    _notificationService.RegisterNotification(() =>
                    {
                        AssertIsForeground();

                        // First, clear the flag.  That way any new changes we hear about will enqueue a task
                        // to run at a later point.
                        _notificationRequestEnqueued = false;
                        this.NotifyEditor();
                    }, (int)delay.ComputeTimeDelay(_subjectBuffer).TotalMilliseconds, _listener.BeginAsyncOperation("EnqueueNotificationRequest"));
                }
            }

            private void NotifyEditor()
            {
                AssertIsForeground();

                // If we're currently suppressed, then just re-enqueue a request to update in the
                // future.
                if (this.IsPaused)
                {
                    // TODO(cyrusn): Do we need to make this delay customizable?  I don't think we do.
                    // Pausing is only used for features we don't want to spam the user with (like
                    // squiggles while the completion list is up.  It's ok to have them appear 1.5
                    // seconds later once we become un-paused.
                    if ((Environment.TickCount - _lastPausedTime) < TaggerConstants.IdleDelay)
                    {
                        EnqueueNotificationRequest(TaggerDelay.OnIdle);
                        return;
                    }
                }

                using (Logger.LogBlock(FunctionId.Tagger_BatchChangeNotifier_NotifyEditor, CancellationToken.None))
                {
                    // Go through and report the snapshots from oldest to newest.
                    foreach (var snapshotAndSpans in _snapshotVersionToSpansMap)
                    {
                        var snapshot = snapshotAndSpans.Key;
                        var normalizedSpans = snapshotAndSpans.Value;

                        this.NotifyEditorNow(normalizedSpans);
                    }
                }

                // Finally, clear out the collection so that we don't re-report spans.
                _snapshotVersionToSpansMap.Clear();
                _lastReportTick = Environment.TickCount;

                // reset paused time
                _lastPausedTime = Environment.TickCount;
            }

            private void NotifyEditorNow(NormalizedSnapshotSpanCollection normalizedSpans)
            {
                this.AssertIsForeground();

                using (Logger.LogBlock(FunctionId.Tagger_BatchChangeNotifier_NotifyEditorNow, CancellationToken.None))
                {
                    if (normalizedSpans.Count == 0)
                    {
                        return;
                    }

                    normalizedSpans = CoalesceSpans(normalizedSpans);

                    // Don't use linq here.  It's a hotspot.
                    foreach (var span in normalizedSpans)
                    {
                        _reportChangedSpan(span);
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
        }
    }
}