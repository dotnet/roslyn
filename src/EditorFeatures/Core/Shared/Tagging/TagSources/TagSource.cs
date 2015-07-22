// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal sealed partial class TagSource<TTag> : 
        ForegroundThreadAffinitizedObject
        where TTag : ITag
    {
        #region Fields that can be accessed from either thread

        /// <summary>
        /// The async worker we defer to handle foreground/background thread management for this
        /// tagger. Note: some operations we perform on this must be uncancellable.  Specifically,
        /// once we've updated our internal state we need to *ensure* that the UI eventually gets in
        /// sync with it. As such, we allow cancellation of our tasks *until* we update our state.
        /// From that point on, we must proceed and execute the tasks.
        /// </summary>
        internal readonly AsynchronousSerialWorkQueue WorkQueue;

        private readonly ITextBuffer _subjectBuffer;

        /// <summary>
        /// async operation notifier
        /// </summary>
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>
        /// foreground notification service
        /// </summary>
        private readonly IForegroundNotificationService _notificationService;

        #endregion

        public TagSource(
            ITextView textViewOpt,
            ITextBuffer subjectBuffer,
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            if (dataSource.SpanTrackingMode == SpanTrackingMode.Custom)
            {
                throw new ArgumentException("SpanTrackingMode.Custom not allowed.", "spanTrackingMode");
            }

            _subjectBuffer = subjectBuffer;
            _textViewOpt = textViewOpt;
            _dataSource = dataSource;
            _asyncListener = asyncListener;
            _notificationService = notificationService;
            _tagSpanComparer = new TagSpanComparer<TTag>(this.TagComparer);

            this.WorkQueue = new AsynchronousSerialWorkQueue(asyncListener);
            this.CachedTagTrees = ImmutableDictionary.Create<ITextBuffer, TagSpanIntervalTree<TTag>>();

            StartInitialRefresh();

            _eventSource = dataSource.CreateEventSource(textViewOpt, subjectBuffer);

            AttachEventHandlersAndStart();
        }

        public void RegisterNotification(Action action, int delay, CancellationToken cancellationToken)
        {
            _notificationService.RegisterNotification(action, delay, this._asyncListener.BeginAsyncOperation("TagSource"), cancellationToken);
        }

        public event Action<ICollection<KeyValuePair<ITextBuffer,NormalizedSnapshotSpanCollection>>> TagsChangedForBuffer;

        public event EventHandler Paused;
        public event EventHandler Resumed;

        /// <summary>
        /// Called by derived types to enqueue tags re-calculation request
        /// </summary>
        private void RecalculateTagsOnChanged(TaggerEventArgs e)
        {
            RegisterNotification(RecomputeTagsForeground, e.Delay.ComputeTimeDelayMS(this._subjectBuffer), this.WorkQueue.CancellationToken);
        }

        public void Disconnect()
        {
            this.WorkQueue.AssertIsForeground();
            this.WorkQueue.CancelCurrentWork();

            // Tell the interaction object to stop issuing events.
            _eventSource.Disconnect();

            if (_dataSource.CaretChangeBehavior.HasFlag(TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag))
            {
                this._textViewOpt.Caret.PositionChanged -= OnCaretPositionChanged;
            }

            if (_dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.TrackTextChanges))
            {
                this._subjectBuffer.Changed -= OnSubjectBufferChanged;
            }

            _eventSource.UIUpdatesPaused -= OnUIUpdatesPaused;
            _eventSource.UIUpdatesResumed -= OnUIUpdatesResumed;
            _eventSource.Changed -= OnChanged;
        }

        private void StartInitialRefresh()
        {
            this.WorkQueue.AssertIsForeground();

            RecalculateTagsOnChanged(new TaggerEventArgs(TaggerDelay.Short));
        }

        private void RaiseTagsChanged(ITextBuffer buffer, NormalizedSnapshotSpanCollection difference)
        {
            if (difference.Count == 0)
            {
                // nothing changed.
                return;
            }

            RaiseTagsChanged(SpecializedCollections.SingletonCollection(
                new KeyValuePair<ITextBuffer, NormalizedSnapshotSpanCollection>(buffer, difference)));
        }

        private void RaiseTagsChanged(ICollection<KeyValuePair<ITextBuffer, NormalizedSnapshotSpanCollection>> collection)
        {
            var tagsChangedForBuffer = TagsChangedForBuffer;
            if (tagsChangedForBuffer != null)
            {
                tagsChangedForBuffer(collection);
            }
        }

        private void RaisePaused()
        {
            var paused = this.Paused;
            if (paused != null)
            {
                paused(this, EventArgs.Empty);
            }
        }

        private void RaiseResumed()
        {
            var resumed = this.Resumed;
            if (resumed != null)
            {
                resumed(this, EventArgs.Empty);
            }
        }

        private static T NextOrDefault<T>(IEnumerator<T> enumerator)
        {
            return enumerator.MoveNext() ? enumerator.Current : default(T);
        }

        /// <summary>
        /// Return all the spans that appear in only one of "latestSpans" or "previousSpans".
        /// </summary>
        private static IEnumerable<SnapshotSpan> Difference<T>(IEnumerable<T> latestSpans, IEnumerable<T> previousSpans, IDiffSpanComparer<T> diffComparer)
        {
            var latestEnumerator = latestSpans.GetEnumerator();
            var previousEnumerator = previousSpans.GetEnumerator();
            try
            {
                var latest = NextOrDefault(latestEnumerator);
                var previous = NextOrDefault(previousEnumerator);

                while (!diffComparer.IsDefault(latest) && !diffComparer.IsDefault(previous))
                {
                    var latestSpan = diffComparer.GetSpan(latest);
                    var previousSpan = diffComparer.GetSpan(previous);

                    if (latestSpan.Start < previousSpan.Start)
                    {
                        yield return latestSpan;
                        latest = NextOrDefault(latestEnumerator);
                    }
                    else if (previousSpan.Start < latestSpan.Start)
                    {
                        yield return previousSpan;
                        previous = NextOrDefault(previousEnumerator);
                    }
                    else
                    {
                        // If the starts are the same, but the ends are different, report the larger
                        // region to be conservative.
                        if (previousSpan.End > latestSpan.End)
                        {
                            yield return previousSpan;
                            latest = NextOrDefault(latestEnumerator);
                        }
                        else if (latestSpan.End > previousSpan.End)
                        {
                            yield return latestSpan;
                            previous = NextOrDefault(previousEnumerator);
                        }
                        else
                        {
                            if (!diffComparer.Equals(latest, previous))
                            {
                                yield return latestSpan;
                            }

                            latest = NextOrDefault(latestEnumerator);
                            previous = NextOrDefault(previousEnumerator);
                        }
                    }
                }

                while (!diffComparer.IsDefault(latest))
                {
                    yield return diffComparer.GetSpan(latest);
                    latest = NextOrDefault(latestEnumerator);
                }

                while (!diffComparer.IsDefault(previous))
                {
                    yield return diffComparer.GetSpan(previous);
                    previous = NextOrDefault(previousEnumerator);
                }
            }
            finally
            {
                latestEnumerator.Dispose();
                previousEnumerator.Dispose();
            }
        }

        private interface IDiffSpanComparer<T>
        {
            bool IsDefault(T t);
            SnapshotSpan GetSpan(T t);
            bool Equals(T t1, T t2);
        }
    }
}
