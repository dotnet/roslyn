// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private sealed partial class TagSource : ForegroundThreadAffinitizedObject
        {
            #region Fields that can be accessed from either thread

            /// <summary>
            /// The async worker we defer to handle foreground/background thread management for this
            /// tagger. Note: some operations we perform on this must be uncancellable.  Specifically,
            /// once we've updated our internal state we need to *ensure* that the UI eventually gets in
            /// sync with it. As such, we allow cancellation of our tasks *until* we update our state.
            /// From that point on, we must proceed and execute the tasks.
            /// </summary>
            private readonly AsynchronousSerialWorkQueue _workQueue;

            private readonly AbstractAsynchronousTaggerProvider<TTag> _dataSource;

            private IEqualityComparer<ITagSpan<TTag>> _tagSpanComparer;

            /// <summary>
            /// async operation notifier
            /// </summary>
            private readonly IAsynchronousOperationListener _asyncListener;

            /// <summary>
            /// foreground notification service
            /// </summary>
            private readonly IForegroundNotificationService _notificationService;

            #endregion

            #region Fields that can only be accessed from the foreground thread

            private readonly ITextView _textViewOpt;
            private readonly ITextBuffer _subjectBuffer;

            /// <summary>
            /// Our tagger event source that lets us know when we should call into the tag producer for
            /// new tags.
            /// </summary>
            private readonly ITaggerEventSource _eventSource;

            /// <summary>
            /// During the time that we are paused from updating the UI, we will use these tags instead.
            /// </summary>
            private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> _previousCachedTagTrees;

            /// <summary>
            /// accumulated text changes since last tag calculation
            /// </summary>
            private TextChangeRange? _accumulatedTextChanges_doNotAccessDirectly;
            private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> _cachedTagTrees_doNotAccessDirectly;
            private object _state_doNotAccessDirecty;
            private bool _upToDate_doNotAccessDirectly = false;

            #endregion

            public event Action<ICollection<KeyValuePair<ITextBuffer, NormalizedSnapshotSpanCollection>>> TagsChangedForBuffer;

            public event EventHandler Paused;
            public event EventHandler Resumed;

            public TagSource(
                ITextView textViewOpt,
                ITextBuffer subjectBuffer,
                AbstractAsynchronousTaggerProvider<TTag> dataSource,
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
                _tagSpanComparer = new TagSpanComparer(_dataSource.TagComparer);

                DebugRecordInitialStackTrace();

                _workQueue = new AsynchronousSerialWorkQueue(asyncListener);
                this.CachedTagTrees = ImmutableDictionary.Create<ITextBuffer, TagSpanIntervalTree<TTag>>();

                _eventSource = CreateEventSource();

                Connect();

                // Kick off a task to compute the initial set of tags.
                RecalculateTagsOnChanged(new TaggerEventArgs(TaggerDelay.Short));
            }

            private ITaggerEventSource CreateEventSource()
            {
                var eventSource = _dataSource.CreateEventSource(_textViewOpt, _subjectBuffer);

                // If there are any options specified for this tagger, then also hook up event
                // notifications for when those options change.
                var optionChangedEventSources =
                    _dataSource.Options.Concat<IOption>(_dataSource.PerLanguageOptions)
                        .Select(o => TaggerEventSources.OnOptionChanged(_subjectBuffer, o, TaggerDelay.NearImmediate)).ToList();

                if (optionChangedEventSources.Count == 0)
                {
                    // No options specified for this tagger.  So just keep the event source as is.
                    return eventSource;
                }

                optionChangedEventSources.Add(eventSource);
                return TaggerEventSources.Compose(optionChangedEventSources);
            }

            private TextChangeRange? AccumulatedTextChanges
            {
                get
                {
                    _workQueue.AssertIsForeground();
                    return _accumulatedTextChanges_doNotAccessDirectly;
                }

                set
                {
                    _workQueue.AssertIsForeground();
                    _accumulatedTextChanges_doNotAccessDirectly = value;
                }
            }

            private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> CachedTagTrees
            {
                get
                {
                    _workQueue.AssertIsForeground();
                    return _cachedTagTrees_doNotAccessDirectly;
                }

                set
                {
                    _workQueue.AssertIsForeground();
                    _cachedTagTrees_doNotAccessDirectly = value;
                }
            }

            private object State
            {
                get
                {
                    _workQueue.AssertIsForeground();
                    return _state_doNotAccessDirecty;
                }

                set
                {
                    _workQueue.AssertIsForeground();
                    _state_doNotAccessDirecty = value;
                }
            }

            private bool UpToDate
            {
                get
                {
                    _workQueue.AssertIsForeground();
                    return _upToDate_doNotAccessDirectly;
                }

                set
                {
                    _workQueue.AssertIsForeground();
                    _upToDate_doNotAccessDirectly = value;
                }
            }

            public void RegisterNotification(Action action, int delay, CancellationToken cancellationToken)
            {
                _notificationService.RegisterNotification(action, delay, _asyncListener.BeginAsyncOperation("TagSource"), cancellationToken);
            }

            /// <summary>
            /// Called by derived types to enqueue tags re-calculation request
            /// </summary>
            private void RecalculateTagsOnChanged(TaggerEventArgs e)
            {
                // First, cancel any previous requests (either still queued, or started).  We no longer
                // want to continue it if new changes have come in.
                _workQueue.CancelCurrentWork();

                RegisterNotification(RecomputeTagsForeground, (int)e.Delay.ComputeTimeDelay(_subjectBuffer).TotalMilliseconds, _workQueue.CancellationToken);
            }

            private void Connect()
            {
                _workQueue.AssertIsForeground();

                _eventSource.Changed += OnChanged;
                _eventSource.UIUpdatesResumed += OnUIUpdatesResumed;
                _eventSource.UIUpdatesPaused += OnUIUpdatesPaused;

                if (_dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.TrackTextChanges))
                {
                    _subjectBuffer.Changed += OnSubjectBufferChanged;
                }

                if (_dataSource.CaretChangeBehavior.HasFlag(TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag))
                {
                    if (_textViewOpt == null)
                    {
                        throw new ArgumentException(
                            nameof(_dataSource.CaretChangeBehavior) + " can only be specified for an " + nameof(IViewTaggerProvider));
                    }

                    _textViewOpt.Caret.PositionChanged += OnCaretPositionChanged;
                }

                // Tell the interaction object to start issuing events.
                _eventSource.Connect();
            }

            public void Disconnect()
            {
                _workQueue.AssertIsForeground();
                _workQueue.CancelCurrentWork();

                // Tell the interaction object to stop issuing events.
                _eventSource.Disconnect();

                if (_dataSource.CaretChangeBehavior.HasFlag(TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag))
                {
                    _textViewOpt.Caret.PositionChanged -= OnCaretPositionChanged;
                }

                if (_dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.TrackTextChanges))
                {
                    _subjectBuffer.Changed -= OnSubjectBufferChanged;
                }

                _eventSource.UIUpdatesPaused -= OnUIUpdatesPaused;
                _eventSource.UIUpdatesResumed -= OnUIUpdatesResumed;
                _eventSource.Changed -= OnChanged;
            }

            private void RaiseTagsChanged(ITextBuffer buffer, NormalizedSnapshotSpanCollection difference)
            {
                this.AssertIsForeground();
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
                TagsChangedForBuffer?.Invoke(collection);
            }

            private void RaisePaused()
            {
                this.Paused?.Invoke(this, EventArgs.Empty);
            }

            private void RaiseResumed()
            {
                this.Resumed?.Invoke(this, EventArgs.Empty);
            }

            private static T NextOrDefault<T>(IEnumerator<T> enumerator)
            {
                return enumerator.MoveNext() ? enumerator.Current : default(T);
            }

            /// <summary>
            /// Return all the spans that appear in only one of "latestSpans" or "previousSpans".
            /// </summary>
            private static IEnumerable<SnapshotSpan> Difference<T>(IEnumerable<ITagSpan<T>> latestSpans, IEnumerable<ITagSpan<T>> previousSpans, IEqualityComparer<T> comparer)
                where T : ITag
            {
                var latestEnumerator = latestSpans.GetEnumerator();
                var previousEnumerator = previousSpans.GetEnumerator();
                try
                {
                    var latest = NextOrDefault(latestEnumerator);
                    var previous = NextOrDefault(previousEnumerator);

                    while (latest != null && previous != null)
                    {
                        var latestSpan = latest.Span;
                        var previousSpan = previous.Span;

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
                                if (!comparer.Equals(latest.Tag, previous.Tag))
                                {
                                    yield return latestSpan;
                                }

                                latest = NextOrDefault(latestEnumerator);
                                previous = NextOrDefault(previousEnumerator);
                            }
                        }
                    }

                    while (latest != null)
                    {
                        yield return latest.Span;
                        latest = NextOrDefault(latestEnumerator);
                    }

                    while (previous != null)
                    {
                        yield return previous.Span;
                        previous = NextOrDefault(previousEnumerator);
                    }
                }
                finally
                {
                    latestEnumerator.Dispose();
                    previousEnumerator.Dispose();
                }
            }
        }
    }
}
