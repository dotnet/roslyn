// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces
{
    [Export(typeof(ITextBufferVisibilityTracker))]
    internal sealed class WpfTextBufferVisibilityTracker : ITextBufferVisibilityTracker
    {
        private readonly ITextBufferAssociatedViewService _associatedViewService;
        private readonly IThreadingContext _threadingContext;

        private readonly Dictionary<ITextBuffer, VisibleTrackerData> _subjectBufferToCallbacks = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WpfTextBufferVisibilityTracker(
            ITextBufferAssociatedViewService associatedViewService,
            IThreadingContext threadingContext)
        {
            _associatedViewService = associatedViewService;
            _threadingContext = threadingContext;

            associatedViewService.SubjectBuffersConnected += AssociatedViewService_SubjectBuffersConnected;
            associatedViewService.SubjectBuffersDisconnected += AssociatedViewService_SubjectBuffersDisconnected;
        }

        private void AssociatedViewService_SubjectBuffersConnected(object sender, SubjectBuffersConnectedEventArgs e)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);
            UpdateAllAssociatedViews(e.SubjectBuffers);
        }

        private void AssociatedViewService_SubjectBuffersDisconnected(object sender, SubjectBuffersConnectedEventArgs e)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);
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
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);

            var views = _associatedViewService.GetAssociatedTextViews(subjectBuffer).ToImmutableArrayOrEmpty();

            // if any of the views were *not* wpf text views, assume the buffer is visible.  We don't know how to
            // determine the visibility of this buffer.  While unlikely to happen, this is possible with VS's
            // extensibility model, which allows for a plugin to host an ITextBuffer in their own impl of an ITextView.
            // For those cases, just assume these buffers are visible.
            if (views.Any(v => v is not IWpfTextView))
                return true;

            return views.OfType<IWpfTextView>().Any(v => v.VisualElement.IsVisible);
        }

        public void RegisterForVisibilityChanges(ITextBuffer subjectBuffer, ITextBufferVisibilityChangedCallback callback)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);
            if (!_subjectBufferToCallbacks.TryGetValue(subjectBuffer, out var data))
            {
                data = new VisibleTrackerData(this, subjectBuffer);
                _subjectBufferToCallbacks.Add(subjectBuffer, data);
            }

            data.Callbacks.Add(callback);
        }

        public void UnregisterForVisibilityChanges(ITextBuffer subjectBuffer, ITextBufferVisibilityChangedCallback callback)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);

            // Both of these methods must succeed.  Otherwise we're somehow unregistering something we don't know about.
            Contract.ThrowIfFalse(_subjectBufferToCallbacks.TryGetValue(subjectBuffer, out var data));
            Contract.ThrowIfFalse(data.Callbacks.Remove(callback));

            // If we have nothing that wants to listen to information about this buffer anymore, then disconnect it
            // from all events and remove our map.
            if (data.Callbacks.Count == 0)
            {
                data.Dispose();
                _subjectBufferToCallbacks.Remove(subjectBuffer);
            }
        }

        private sealed class VisibleTrackerData : IDisposable
        {
            public readonly HashSet<ITextBufferVisibilityChangedCallback> Callbacks = new();
            public readonly HashSet<ITextView> TextViews = new();

            private readonly WpfTextBufferVisibilityTracker _tracker;
            private readonly ITextBuffer _subjectBuffer;

            public VisibleTrackerData(
                WpfTextBufferVisibilityTracker tracker,
                ITextBuffer subjectBuffer)
            {
                _tracker = tracker;
                _subjectBuffer = subjectBuffer;
                UpdateAssociatedViews();
            }

            public void Dispose()
            {
                Contract.ThrowIfFalse(_tracker._threadingContext.HasMainThread);

                // Shouldn't be disposing of this if we still have clients that want to hear about visibility changes.
                Contract.ThrowIfTrue(Callbacks.Count > 0);

                // Clear out all our textviews.  This will disconnect us from any events we have registered with them.
                UpdateTextViews(Array.Empty<ITextView>());

                Contract.ThrowIfTrue(TextViews.Count > 0);
            }

            public void UpdateAssociatedViews()
            {
                Contract.ThrowIfFalse(_tracker._threadingContext.HasMainThread);

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
                    if (removedView is IWpfTextView removedWpfView)
                        removedWpfView.VisualElement.IsVisibleChanged -= VisualElement_IsVisibleChanged;
                }

                // Connect to hearing about visbility changes for any views we are associated with.
                foreach (var addedView in addedViews)
                {
                    if (addedView is IWpfTextView addedWpfView)
                        addedWpfView.VisualElement.IsVisibleChanged += VisualElement_IsVisibleChanged;
                }

                TextViews.Clear();
                TextViews.AddRange(associatedTextViews);
            }

            private void VisualElement_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
            {
                Contract.ThrowIfFalse(_tracker._threadingContext.HasMainThread);
                foreach (var callback in Callbacks)
                    callback.OnTextBufferVisibilityChanged();
            }
        }
    }
}
