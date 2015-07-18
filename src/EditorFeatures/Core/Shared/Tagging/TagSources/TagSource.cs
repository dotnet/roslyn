// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    /// <summary>
    /// <para>this is a bare minimum base implementation of TagSource where you can provide your own implementation that
    /// doesn't rely on any other framework such as event source, event producer to participate in async tagger framework</para>
    /// </summary>
    /// <typeparam name="TTag">The type of tag.</typeparam>
    internal abstract partial class TagSource<TTag>
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

        protected readonly ITextBuffer SubjectBuffer;

        /// <summary>
        /// async operation notifier
        /// </summary>
        protected readonly IAsynchronousOperationListener Listener;

        /// <summary>
        /// foreground notification service
        /// </summary>
        private readonly IForegroundNotificationService _notificationService;

        #endregion

        protected TagSource(
            ITextBuffer subjectBuffer,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListener asyncListener)
        {
            this.SubjectBuffer = subjectBuffer;
            _notificationService = notificationService;

            this.Listener = asyncListener;
            this.WorkQueue = new AsynchronousSerialWorkQueue(asyncListener);

            StartInitialRefresh();
        }

        public void RegisterNotification(Action action, int delay, CancellationToken cancellationToken)
        {
            _notificationService.RegisterNotification(action, delay, this.Listener.BeginAsyncOperation("TagSource"), cancellationToken);
        }

        public event Action<ICollection<KeyValuePair<ITextBuffer,NormalizedSnapshotSpanCollection>>> TagsChangedForBuffer;

        public event EventHandler Paused;
        public event EventHandler Resumed;

        /// <summary>
        /// implemented by derived types to return interval tree associated with the buffer
        /// </summary>
        public abstract ITagSpanIntervalTree<TTag> GetTagIntervalTreeForBuffer(ITextBuffer buffer);

        /// <summary>
        /// Implemented by derived types to start recalculate tags
        /// </summary>
        protected abstract void RecomputeTagsForeground();

        /// <summary>
        /// Called by derived types to enqueue tags re-calculation request
        /// </summary>
        protected virtual void RecalculateTagsOnChanged(TaggerEventArgs e)
        {
            RegisterNotification(RecomputeTagsForeground, e.Delay.ComputeTimeDelay(this.SubjectBuffer), this.WorkQueue.CancellationToken);
        }

        protected virtual void Disconnect()
        {
            this.WorkQueue.AssertIsForeground();
            this.WorkQueue.CancelCurrentWork();
        }

        private void StartInitialRefresh()
        {
            this.WorkQueue.AssertIsForeground();

            RecalculateTagsOnChanged(new TaggerEventArgs(PredefinedChangedEventKinds.TaggerCreated, TaggerDelay.Short));
        }

        protected void RaiseTagsChanged(ITextBuffer buffer, NormalizedSnapshotSpanCollection difference)
        {
            if (difference.Count == 0)
            {
                // nothing changed.
                return;
            }

            RaiseTagsChanged(SpecializedCollections.SingletonCollection(
                new KeyValuePair<ITextBuffer, NormalizedSnapshotSpanCollection>(buffer, difference)));
        }

        protected void RaiseTagsChanged(ICollection<KeyValuePair<ITextBuffer, NormalizedSnapshotSpanCollection>> collection)
        {
            var tagsChangedForBuffer = TagsChangedForBuffer;
            if (tagsChangedForBuffer != null)
            {
                tagsChangedForBuffer(collection);
            }
        }

        protected void RaisePaused()
        {
            var paused = this.Paused;
            if (paused != null)
            {
                paused(this, EventArgs.Empty);
            }
        }

        protected void RaiseResumed()
        {
            var resumed = this.Resumed;
            if (resumed != null)
            {
                resumed(this, EventArgs.Empty);
            }
        }

        protected static T NextOrDefault<T>(IEnumerator<T> enumerator)
        {
            return enumerator.MoveNext() ? enumerator.Current : default(T);
        }

        /// <summary>
        /// Return all the spans that appear in only one of "latestSpans" or "previousSpans".
        /// </summary>
        protected static IEnumerable<SnapshotSpan> Difference<T>(IEnumerable<T> latestSpans, IEnumerable<T> previousSpans, IDiffSpanComparer<T> diffComparer)
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

        protected interface IDiffSpanComparer<T>
        {
            bool IsDefault(T t);
            SnapshotSpan GetSpan(T t);
            bool Equals(T t1, T t2);
        }
    }
}
