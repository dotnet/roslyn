// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        /// <summary>
        /// Handles the job of batching up change notifications so that don't spam the editor with too
        /// many update requests at a time.
        /// </summary>
        private class BatchChangeNotifier : ForegroundThreadAffinitizedObject
        {
            private readonly ITextBuffer _subjectBuffer;

            /// <summary>
            /// The worker we use to do work on the appropriate background or foreground thread.
            /// </summary>
            private readonly IAsynchronousOperationListener _listener;
            private readonly IForegroundNotificationService _notificationService;
            private readonly CancellationToken _cancellationToken;

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
            private readonly SortedDictionary<int, NormalizedSnapshotSpanCollection> _snapshotVersionToSpansMap = new();

            private readonly Action<NormalizedSnapshotSpanCollection> _notifyEditorNow;

            public BatchChangeNotifier(
                IThreadingContext threadingContext,
                ITextBuffer subjectBuffer,
                IAsynchronousOperationListener listener,
                IForegroundNotificationService notificationService,
                Action<NormalizedSnapshotSpanCollection> notifyEditorNow,
                CancellationToken cancellationToken)
                : base(threadingContext)
            {
                Contract.ThrowIfNull(notifyEditorNow);
                _subjectBuffer = subjectBuffer;
                _listener = listener;
                _notificationService = notificationService;
                _cancellationToken = cancellationToken;
                _notifyEditorNow = notifyEditorNow;
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
                    _notificationService.RegisterNotification(
                        () =>
                        {
                            AssertIsForeground();

                            // First, clear the flag.  That way any new changes we hear about will enqueue a task
                            // to run at a later point.
                            _notificationRequestEnqueued = false;
                            this.NotifyEditor();
                        },
                        (int)delay.ComputeTimeDelay(_subjectBuffer).TotalMilliseconds,
                        _listener.BeginAsyncOperation("EnqueueNotificationRequest"),
                        _cancellationToken);
                }
            }

            private void NotifyEditor()
            {
                AssertIsForeground();

                using (Logger.LogBlock(FunctionId.Tagger_BatchChangeNotifier_NotifyEditor, CancellationToken.None))
                {
                    // Go through and report the snapshots from oldest to newest.
                    foreach (var snapshotAndSpans in _snapshotVersionToSpansMap)
                    {
                        var snapshot = snapshotAndSpans.Key;
                        var normalizedSpans = snapshotAndSpans.Value;

                        _notifyEditorNow(normalizedSpans);
                    }
                }

                // Finally, clear out the collection so that we don't re-report spans.
                _snapshotVersionToSpansMap.Clear();
                _lastReportTick = Environment.TickCount;
            }
        }
    }
}
