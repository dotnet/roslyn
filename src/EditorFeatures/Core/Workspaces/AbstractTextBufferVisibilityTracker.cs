// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces;

internal abstract class AbstractTextBufferVisibilityTracker<
    TTextView,
    TVisibilityChangedCallback> : ITextBufferVisibilityTracker
    where TTextView : ITextView
    where TVisibilityChangedCallback : System.Delegate
{
    private readonly ITextBufferAssociatedViewService _associatedViewService;
    private readonly IThreadingContext _threadingContext;

    private readonly Dictionary<ITextBuffer, VisibleTrackerData> _subjectBufferToCallbacks = [];

    protected AbstractTextBufferVisibilityTracker(
        ITextBufferAssociatedViewService associatedViewService,
        IThreadingContext threadingContext)
    {
        _associatedViewService = associatedViewService;
        _threadingContext = threadingContext;

        associatedViewService.SubjectBuffersConnected += AssociatedViewService_SubjectBuffersConnected;
        associatedViewService.SubjectBuffersDisconnected += AssociatedViewService_SubjectBuffersDisconnected;
    }

    protected abstract bool IsVisible(TTextView view);
    protected abstract TVisibilityChangedCallback GetVisiblityChangeCallback(VisibleTrackerData visibleTrackerData);
    protected abstract void AddVisibilityChangedCallback(TTextView view, TVisibilityChangedCallback visibilityChangedCallback);
    protected abstract void RemoveVisibilityChangedCallback(TTextView view, TVisibilityChangedCallback visibilityChangedCallback);

    private void AssociatedViewService_SubjectBuffersConnected(object? sender, SubjectBuffersConnectedEventArgs e)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        UpdateAllAssociatedViews(e.SubjectBuffers);
    }

    private void AssociatedViewService_SubjectBuffersDisconnected(object? sender, SubjectBuffersConnectedEventArgs e)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        UpdateAllAssociatedViews(e.SubjectBuffers);
    }

    private void UpdateAllAssociatedViews(ReadOnlyCollection<ITextBuffer> subjectBuffers)
    {
        // Whenever views get attached/detached from buffers, make sure we're hooked up to the appropriate events
        // for them.
        foreach (var buffer in subjectBuffers)
        {
            if (_subjectBufferToCallbacks.TryGetValue(buffer, out var data))
                data.UpdateAssociatedViews();
        }
    }

    public bool IsVisible(ITextBuffer subjectBuffer)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var views = _associatedViewService.GetAssociatedTextViews(subjectBuffer).ToImmutableArrayOrEmpty();

        // If we don't have any views at all, then assume the buffer is visible.
        if (views.Length == 0)
            return true;

        // if any of the views were *not* the right kind of text views, assume the buffer is visible.  We don't know
        // how to determine the visibility of this buffer.  While unlikely to happen, this is possible with VS's
        // extensibility model, which allows for a plugin to host an ITextBuffer in their own impl of an ITextView.
        // For those cases, just assume these buffers are visible.
        if (views.Any(static v => v is not TTextView))
            return true;

        return views.OfType<TTextView>().Any(v => IsVisible(v));
    }

    public void RegisterForVisibilityChanges(ITextBuffer subjectBuffer, Action callback)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (!_subjectBufferToCallbacks.TryGetValue(subjectBuffer, out var data))
        {
            data = new VisibleTrackerData(this, subjectBuffer);
            _subjectBufferToCallbacks.Add(subjectBuffer, data);
        }

        data.AddCallback(callback);
    }

    public void UnregisterForVisibilityChanges(ITextBuffer subjectBuffer, Action callback)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        // Both of these methods must succeed.  Otherwise we're somehow unregistering something we don't know about.
        Contract.ThrowIfFalse(_subjectBufferToCallbacks.TryGetValue(subjectBuffer, out var data));
        Contract.ThrowIfFalse(data.Callbacks.Contains(callback));
        data.RemoveCallback(callback);

        // If we have nothing that wants to listen to information about this buffer anymore, then disconnect it
        // from all events and remove our map.
        if (data.Callbacks.Count == 0)
        {
            data.Dispose();
            _subjectBufferToCallbacks.Remove(subjectBuffer);
        }
    }

    public TestAccessor GetTestAccessor()
        => new(this);

    public readonly struct TestAccessor(AbstractTextBufferVisibilityTracker<TTextView, TVisibilityChangedCallback> visibilityTracker)
    {
        private readonly AbstractTextBufferVisibilityTracker<TTextView, TVisibilityChangedCallback> _visibilityTracker = visibilityTracker;

        public void TriggerCallbacks(ITextBuffer subjectBuffer)
        {
            var data = _visibilityTracker._subjectBufferToCallbacks[subjectBuffer];
            data.TriggerCallbacks();
        }
    }

    protected sealed class VisibleTrackerData : IDisposable
    {
        public readonly HashSet<ITextView> TextViews = [];

        private readonly AbstractTextBufferVisibilityTracker<TTextView, TVisibilityChangedCallback> _tracker;
        private readonly ITextBuffer _subjectBuffer;
        private readonly TVisibilityChangedCallback _visibilityChangedCallback;

        /// <summary>
        /// The callbacks that want to be notified when our <see cref="TextViews"/> change visibility.  Stored as an
        /// <see cref="ImmutableHashSet{T}"/> so we can enumerate it safely without it changing underneath us.
        /// </summary>
        public ImmutableHashSet<Action> Callbacks { get; private set; } = [];

        public VisibleTrackerData(
            AbstractTextBufferVisibilityTracker<TTextView, TVisibilityChangedCallback> tracker,
            ITextBuffer subjectBuffer)
        {
            _tracker = tracker;
            _subjectBuffer = subjectBuffer;
            _visibilityChangedCallback = tracker.GetVisiblityChangeCallback(this);

            UpdateAssociatedViews();
        }

        public void Dispose()
        {
            _tracker._threadingContext.ThrowIfNotOnUIThread();

            // Shouldn't be disposing of this if we still have clients that want to hear about visibility changes.
            Contract.ThrowIfTrue(Callbacks.Count > 0);

            // Clear out all our textviews.  This will disconnect us from any events we have registered with them.
            UpdateTextViews([]);

            Contract.ThrowIfTrue(TextViews.Count > 0);
        }

        public void AddCallback(Action callback)
        {
            _tracker._threadingContext.ThrowIfNotOnUIThread();
            this.Callbacks = this.Callbacks.Add(callback);
        }

        public void RemoveCallback(Action callback)
        {
            _tracker._threadingContext.ThrowIfNotOnUIThread();
            this.Callbacks = this.Callbacks.Remove(callback);
        }

        public void UpdateAssociatedViews()
        {
            _tracker._threadingContext.ThrowIfNotOnUIThread();

            // Update us to whatever the currently associated text views are for this buffer.
            UpdateTextViews(_tracker._associatedViewService.GetAssociatedTextViews(_subjectBuffer));
        }

        private void UpdateTextViews(IEnumerable<ITextView> associatedTextViews)
        {
            var removedViews = TextViews.Except(associatedTextViews);
            var addedViews = associatedTextViews.Except(TextViews);

            // Disconnect from hearing about visibility changes for any views we're no longer associated with.
            foreach (var removedView in removedViews)
            {
                if (removedView is TTextView genericView)
                    _tracker.RemoveVisibilityChangedCallback(genericView, _visibilityChangedCallback);
            }

            // Connect to hearing about visbility changes for any views we are associated with.
            foreach (var addedView in addedViews)
            {
                if (addedView is TTextView genericView)
                    _tracker.AddVisibilityChangedCallback(genericView, _visibilityChangedCallback);
            }

            TextViews.Clear();
            TextViews.AddRange(associatedTextViews);
        }

        public void TriggerCallbacks()
        {
            _tracker._threadingContext.ThrowIfNotOnUIThread();
            foreach (var callback in Callbacks)
                callback();
        }
    }
}
