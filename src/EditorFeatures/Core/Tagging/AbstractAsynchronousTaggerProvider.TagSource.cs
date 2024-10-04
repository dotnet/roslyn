// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal partial class AbstractAsynchronousTaggerProvider<TTag>
{
    /// <summary>
    /// <para>The <see cref="TagSource"/> is the core part of our asynchronous
    /// tagging infrastructure. It is the coordinator between <see cref="ProduceTagsAsync(TaggerContext{TTag}, CancellationToken)"/>s,
    /// <see cref="ITaggerEventSource"/>s, and <see cref="ITagger{T}"/>s.</para>
    /// 
    /// <para>The <see cref="TagSource"/> is the type that actually owns the list of cached tags. When an <see
    /// cref="ITaggerEventSource"/> says tags need to be  recomputed, the tag source starts the computation and calls
    /// <see cref="ProduceTagsAsync(TaggerContext{TTag}, CancellationToken)"/> to build the new list of tags. When
    /// that's done, the tags are stored in <see cref="_cachedTagTrees_mayChangeFromAnyThread"/>. The tagger, when asked
    /// for tags from the editor, then returns the tags that are stored in <see
    /// cref="_cachedTagTrees_mayChangeFromAnyThread"/></para>
    /// 
    /// <para>There is a one-to-many relationship between <see cref="TagSource"/>s
    /// and <see cref="ITagger{T}"/>s. Special cases, like reference highlighting (which processes multiple
    /// subject buffers at once) have their own providers and tag source derivations.</para>
    /// </summary>
    private sealed partial class TagSource
    {
        /// <summary>
        /// If we get more than this many differences, then we just issue it as a single change
        /// notification.  The number has been completely made up without any data to support it.
        /// 
        /// Internal for testing purposes.
        /// </summary>
        private const int CoalesceDifferenceCount = 10;

        private readonly ObjectPool<HashSet<TagSpan<TTag>>> _tagSpanSetPool;

        #region Fields that can be accessed from either thread

        private readonly AbstractAsynchronousTaggerProvider<TTag> _dataSource;

        /// <summary>
        /// Information about what workspace the buffer we're tagging is associated with.
        /// </summary>
        private readonly WorkspaceRegistration _workspaceRegistration;

        /// <summary>
        /// Work queue that collects high priority requests to call TagsChanged with.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<NormalizedSnapshotSpanCollection> _highPriTagsChangedQueue;

        /// <summary>
        /// Work queue that collects normal priority requests to call TagsChanged with.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<NormalizedSnapshotSpanCollection> _normalPriTagsChangedQueue;

        /// <summary>
        /// This queue is used to batch up event change notifications and only dispatch one recomputation every <see
        /// cref="EventChangeDelay"/> to actually produce the latest set of tags.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<TagSourceQueueItem, VoidResult> _eventChangeQueue;

        /// <summary>
        /// For taggers that support tagging frozen and non-frozen snapshots, this cancellation series controls the
        /// non-frozen tagging pass.  We want this to be separately cancellable so that if new events come in that we 
        /// cancel the expensive non-frozen tagging pass (which might be computing skeletons, SG docs, etc.), do the 
        /// next cheap frozen-tagging-pass, and then push the expensive-nonfrozen-tagging-pass to the end again.
        /// </summary>
        private readonly CancellationSeries _nonFrozenComputationCancellationSeries;

        /// <summary>
        /// The last tag trees that we computed per buffer.  Note: this can be read/written from any thread.  Because of
        /// that, we have to use safe operations to actually read or write it.  This includes using looping "compare and
        /// swap" algorithms to make sure that it is consistently moved forward no matter which thread is trying to
        /// mutate it.
        /// </summary>
        private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> _cachedTagTrees_mayChangeFromAnyThread = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;

        #endregion

        #region Mutable state.  Only accessed from _eventChangeQueue

        private object? _state_accessOnlyFromEventChangeQueueCallback;

        #endregion

        #region Fields that can only be accessed from the foreground thread

        /// <summary>
        /// Cancellation token governing all our async work.  Canceled/disposed when we are <see cref="Dispose"/>'d.
        /// </summary>
        private readonly CancellationTokenSource _disposalTokenSource = new();

        private readonly ITextView? _textView;
        private readonly ITextBuffer _subjectBuffer;

        /// <summary>
        /// Callback to us when the visibility of our <see cref="_subjectBuffer"/> changes.
        /// </summary>
        private readonly Action _onVisibilityChanged;

        /// <summary>
        /// Our tagger event source that lets us know when we should call into the tag producer for
        /// new tags.
        /// </summary>
        private readonly ITaggerEventSource _eventSource;

        #region Mutable state.  Can only be accessed from the foreground thread

        /// <summary>
        /// Keep track of if we are processing the first <see cref="ITagger{T}.GetTags"/> request.  If our provider returns 
        /// <see langword="true"/> for <see cref="AbstractAsynchronousTaggerProvider{TTag}.ComputeInitialTagsSynchronously"/>,
        /// then we'll want to synchronously block then and only then for tags.
        /// </summary>
        private bool _firstTagsRequest = true;

        /// <summary>
        /// Whether or not tag generation is paused.  We pause producing tags when documents become non-visible.
        /// See <see cref="ITextBufferVisibilityTracker"/>.
        /// </summary>
        private bool _paused = false;

        #endregion

        #endregion

        public TagSource(
            ITextView? textView,
            ITextBuffer subjectBuffer,
            AbstractAsynchronousTaggerProvider<TTag> dataSource)
        {
            dataSource.ThreadingContext.ThrowIfNotOnUIThread();
            if (dataSource.SpanTrackingMode == SpanTrackingMode.Custom)
                throw new ArgumentException("SpanTrackingMode.Custom not allowed.", "spanTrackingMode");

            _textView = textView;
            _subjectBuffer = subjectBuffer;
            _dataSource = dataSource;
            _nonFrozenComputationCancellationSeries = new(_disposalTokenSource.Token);
            _tagSpanSetPool = new ObjectPool<HashSet<TagSpan<TTag>>>(() => new HashSet<TagSpan<TTag>>(this), trimOnFree: false);

            _workspaceRegistration = Workspace.GetWorkspaceRegistration(subjectBuffer.AsTextContainer());

            // PERF: Use AsyncBatchingWorkQueue<_, VoidResult> instead of AsyncBatchingWorkQueue<_> because the latter
            // has an async state machine that rethrows a very common cancellation exception.
            _eventChangeQueue = new AsyncBatchingWorkQueue<TagSourceQueueItem, VoidResult>(
                dataSource.EventChangeDelay.ComputeTimeDelay(),
                ProcessEventChangeAsync,
                EqualityComparer<TagSourceQueueItem>.Default,
                dataSource.AsyncListener,
                _disposalTokenSource.Token);

            _highPriTagsChangedQueue = new AsyncBatchingWorkQueue<NormalizedSnapshotSpanCollection>(
                TaggerDelay.NearImmediate.ComputeTimeDelay(),
                ProcessTagsChangedAsync,
                equalityComparer: null,
                dataSource.AsyncListener,
                _disposalTokenSource.Token);

            if (_dataSource.AddedTagNotificationDelay == TaggerDelay.NearImmediate)
            {
                // if the tagger wants "added tags" to be reported "NearImmediate"ly, then just reuse
                // the "high pri" queue as that already reports things at that cadence.
                _normalPriTagsChangedQueue = _highPriTagsChangedQueue;
            }
            else
            {
                _normalPriTagsChangedQueue = new AsyncBatchingWorkQueue<NormalizedSnapshotSpanCollection>(
                    _dataSource.AddedTagNotificationDelay.ComputeTimeDelay(),
                    ProcessTagsChangedAsync,
                    equalityComparer: null,
                    dataSource.AsyncListener,
                    _disposalTokenSource.Token);
            }

            DebugRecordInitialStackTrace();

            // Create the tagger-specific events that will cause the tagger to refresh.
            _eventSource = CreateEventSource();

            // Any time visibility changes try to pause us if we're not visible, or resume us if we are.
            _onVisibilityChanged = () =>
            {
                _dataSource.ThreadingContext.ThrowIfNotOnUIThread();
                PauseIfNotVisible();
                ResumeIfVisible();
            };

            // Now hook up this tagger to all interesting events.
            Connect();

            // Now that we're all hooked up to the events we care about, start computing the initial set of tags at
            // high priority.  We want to get the UI to a complete state as soon as possible.
            EnqueueWork(highPriority: true);

            return;

            // Represented as a local function just so we can keep this in sync with Dispose.Disconnect below.
            void Connect()
            {
                _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

                // Register to hear about visibility changes so we can pause/resume this tagger.
                _dataSource.VisibilityTracker?.RegisterForVisibilityChanges(subjectBuffer, _onVisibilityChanged);

                _eventSource.Changed += OnEventSourceChanged;

                if (_dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.RemoveAllTags) ||
                    _dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.RemoveTagsThatIntersectEdits))
                {
                    _subjectBuffer.Changed += OnSubjectBufferChanged;
                }

                if (_dataSource.CaretChangeBehavior.HasFlag(TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag))
                {
                    if (_textView == null)
                    {
                        throw new ArgumentException(
                            nameof(_dataSource.CaretChangeBehavior) + " can only be specified for an " + nameof(IViewTaggerProvider));
                    }

                    _textView.Caret.PositionChanged += OnCaretPositionChanged;
                }

                // Tell the interaction object to start issuing events.
                _eventSource.Connect();
            }
        }

        private void Dispose()
        {
            _disposalTokenSource.Cancel();
            _disposalTokenSource.Dispose();

            _dataSource.RemoveTagSource(_textView, _subjectBuffer);
            GC.SuppressFinalize(this);

            Disconnect();

            return;

            // Keep in sync with TagSource.Connect above (just performing the disconnect operations in the reverse order
            void Disconnect()
            {
                _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

                // Tell the interaction object to stop issuing events.
                _eventSource.Disconnect();

                if (_dataSource.CaretChangeBehavior.HasFlag(TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag))
                {
                    Contract.ThrowIfNull(_textView);
                    _textView.Caret.PositionChanged -= OnCaretPositionChanged;
                }

                if (_dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.RemoveAllTags) ||
                    _dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.RemoveTagsThatIntersectEdits))
                {
                    _subjectBuffer.Changed -= OnSubjectBufferChanged;
                }

                _eventSource.Changed -= OnEventSourceChanged;

                _dataSource.VisibilityTracker?.UnregisterForVisibilityChanges(_subjectBuffer, _onVisibilityChanged);
            }
        }

        private bool IsVisible()
            => _dataSource.VisibilityTracker == null || _dataSource.VisibilityTracker.IsVisible(_subjectBuffer);

        private void PauseIfNotVisible()
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            if (!IsVisible())
            {
                _paused = true;
                _eventSource.Pause();
            }
        }

        private void ResumeIfVisible()
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            // if we're not actually paused, no need to do anything.
            if (!_paused)
                return;

            // If we're not visible, no need to resume.
            if (!IsVisible())
                return;

            // Set us back to running, and kick off work to compute tags now that we're visible again.
            _paused = false;
            _eventSource.Resume();

            // We just transitioned to being visible, compute our tags at high priority so the view is updated as
            // quickly as possible.
            EnqueueWork(highPriority: true);
        }

        private ITaggerEventSource CreateEventSource()
        {
            Contract.ThrowIfTrue(_dataSource.Options.Any(o => o is not Option2<bool> and not PerLanguageOption2<bool>), "All options must be Option2<bool> or PerLanguageOption2<bool>");

            var eventSource = _dataSource.CreateEventSource(_textView, _subjectBuffer);

            // If there are any options specified for this tagger, then also hook up event
            // notifications for when those options change.
            if (_dataSource.Options.IsEmpty && _dataSource.FeatureOptions.IsEmpty)
            {
                return eventSource;
            }

            return TaggerEventSources.Compose(
                eventSource,
                TaggerEventSources.OnGlobalOptionChanged(_dataSource.GlobalOptions, option =>
                    _dataSource.Options.Contains(option) || _dataSource.FeatureOptions.Contains(option)));
        }

        private void RaiseTagsChanged(ITextBuffer buffer, DiffResult difference)
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();
            if (difference.Count == 0)
            {
                // nothing changed.
                return;
            }

            OnTagsChangedForBuffer(
                [KeyValuePairUtil.Create(buffer, difference)],
                highPriority: false);
        }
    }
}
