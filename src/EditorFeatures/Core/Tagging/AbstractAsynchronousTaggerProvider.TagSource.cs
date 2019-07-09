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

            private readonly IEqualityComparer<ITagSpan<TTag>> _tagSpanComparer;

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

            public event Action<ICollection<KeyValuePair<ITextBuffer, DiffResult>>, bool> TagsChangedForBuffer;

            public event EventHandler Paused;
            public event EventHandler Resumed;

            /// <summary>
            /// A cancellation source we use for the initial tagging computation.  We only cancel
            /// if our ref count actually reaches 0.  Otherwise, we always try to compute the initial
            /// set of tags for our view/buffer.
            /// </summary>
            private readonly CancellationTokenSource _initialComputationCancellationTokenSource = new CancellationTokenSource();

            /// <summary>
            /// Whether or not we've gotten any change notifications from our <see cref="ITaggerEventSource"/>.
            /// The first time we hear about changes, we fast track getting tags and reporting 
            /// them to the UI.
            /// 
            /// We use an int so we can use <see cref="Interlocked.CompareExchange(ref int, int, int)"/> 
            /// to read/set this.
            /// </summary>
            private int _seenEventSourceChanged;

            public TaggerDelay AddedTagNotificationDelay => _dataSource.AddedTagNotificationDelay;
            public TaggerDelay RemovedTagNotificationDelay => _dataSource.RemovedTagNotificationDelay;

            public TagSource(
                ITextView textViewOpt,
                ITextBuffer subjectBuffer,
                AbstractAsynchronousTaggerProvider<TTag> dataSource,
                IAsynchronousOperationListener asyncListener,
                IForegroundNotificationService notificationService)
                : base(dataSource.ThreadingContext)
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

                _workQueue = new AsynchronousSerialWorkQueue(ThreadingContext, asyncListener);
                this.CachedTagTrees = ImmutableDictionary.Create<ITextBuffer, TagSpanIntervalTree<TTag>>();

                _eventSource = CreateEventSource();

                Connect();

                // Start computing the initial set of tags immediately.  We want to get the UI
                // to a complete state as soon as possible.
                ComputeInitialTags();
            }

            private void ComputeInitialTags()
            {
                // Note: we always kick this off to the new UI pump instead of computing tags right
                // on this thread.  The reason for that is that we may be getting created at a time
                // when the view itself is initializing.  As such the view is not in a state where
                // we want code touching it.
                RegisterNotification(
                    () => RecomputeTagsForeground(initialTags: true),
                    delay: 0,
                    cancellationToken: GetCancellationToken(initialTags: true));
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
                => _notificationService.RegisterNotification(action, delay, _asyncListener.BeginAsyncOperation(typeof(TTag).Name), cancellationToken);

            private void Connect()
            {
                _workQueue.AssertIsForeground();

                _eventSource.Changed += OnEventSourceChanged;
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
                _workQueue.CancelCurrentWork(remainCancelled: true);

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
                _eventSource.Changed -= OnEventSourceChanged;
            }

            private void RaiseTagsChanged(ITextBuffer buffer, DiffResult difference)
            {
                this.AssertIsForeground();
                if (difference.Count == 0)
                {
                    // nothing changed.
                    return;
                }

                RaiseTagsChanged(SpecializedCollections.SingletonCollection(
                    new KeyValuePair<ITextBuffer, DiffResult>(buffer, difference)),
                    initialTags: false);
            }

            private void RaiseTagsChanged(
                ICollection<KeyValuePair<ITextBuffer, DiffResult>> collection, bool initialTags)
            {
                TagsChangedForBuffer?.Invoke(collection, initialTags);
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
                return enumerator.MoveNext() ? enumerator.Current : default;
            }

            /// <summary>
            /// Return all the spans that appear in only one of "latestSpans" or "previousSpans".
            /// </summary>
            private static DiffResult Difference<T>(IEnumerable<ITagSpan<T>> latestSpans, IEnumerable<ITagSpan<T>> previousSpans, IEqualityComparer<T> comparer)
                where T : ITag
            {
                using (var addedPool = SharedPools.Default<List<SnapshotSpan>>().GetPooledObject())
                using (var removedPool = SharedPools.Default<List<SnapshotSpan>>().GetPooledObject())
                using (var latestEnumerator = latestSpans.GetEnumerator())
                using (var previousEnumerator = previousSpans.GetEnumerator())
                {
                    var added = addedPool.Object;
                    var removed = removedPool.Object;

                    var latest = NextOrDefault(latestEnumerator);
                    var previous = NextOrDefault(previousEnumerator);

                    while (latest != null && previous != null)
                    {
                        var latestSpan = latest.Span;
                        var previousSpan = previous.Span;

                        if (latestSpan.Start < previousSpan.Start)
                        {
                            added.Add(latestSpan);
                            latest = NextOrDefault(latestEnumerator);
                        }
                        else if (previousSpan.Start < latestSpan.Start)
                        {
                            removed.Add(previousSpan);
                            previous = NextOrDefault(previousEnumerator);
                        }
                        else
                        {
                            // If the starts are the same, but the ends are different, report the larger
                            // region to be conservative.
                            if (previousSpan.End > latestSpan.End)
                            {
                                removed.Add(previousSpan);
                                latest = NextOrDefault(latestEnumerator);
                            }
                            else if (latestSpan.End > previousSpan.End)
                            {
                                added.Add(latestSpan);
                                previous = NextOrDefault(previousEnumerator);
                            }
                            else
                            {
                                if (!comparer.Equals(latest.Tag, previous.Tag))
                                {
                                    added.Add(latestSpan);
                                }

                                latest = NextOrDefault(latestEnumerator);
                                previous = NextOrDefault(previousEnumerator);
                            }
                        }
                    }

                    while (latest != null)
                    {
                        added.Add(latest.Span);
                        latest = NextOrDefault(latestEnumerator);
                    }

                    while (previous != null)
                    {
                        removed.Add(previous.Span);
                        previous = NextOrDefault(previousEnumerator);
                    }

                    return new DiffResult(added, removed);
                }
            }
        }
    }
}
