// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Workspaces
{
    [Export(typeof(ITextBufferVisibilityTracker))]
    internal sealed class CocoaTextBufferVisibilityTracker
        : AbstractTextBufferVisibilityTracker<ICocoaTextView, EventHandler>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CocoaTextBufferVisibilityTracker(
            ITextBufferAssociatedViewService associatedViewService,
            IThreadingContext threadingContext)
            : base(associatedViewService, threadingContext)
        {
        }

        protected override bool IsVisible(ICocoaTextView view)
            => view.IsVisible;

        protected override EventHandler GetVisiblityChangeCallback(VisibleTrackerData visibleTrackerData)
            => (sender, args) => visibleTrackerData.TriggerCallbacks();

        protected override void AddVisibilityChangedCallback(ICocoaTextView view, EventHandler visibilityChangedCallback)
            => view.IsVisibleChanged += visibilityChangedCallback;

        protected override void RemoveVisibilityChangedCallback(ICocoaTextView view, EventHandler visibilityChangedCallback)
            => view.IsVisibleChanged -= visibilityChangedCallback;
    }
}
