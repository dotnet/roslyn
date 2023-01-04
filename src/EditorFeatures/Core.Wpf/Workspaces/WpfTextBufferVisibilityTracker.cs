// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Workspaces
{
    [Export(typeof(ITextBufferVisibilityTracker))]
    internal sealed class WpfTextBufferVisibilityTracker
        : AbstractTextBufferVisibilityTracker<IWpfTextView, DependencyPropertyChangedEventHandler>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WpfTextBufferVisibilityTracker(
            ITextBufferAssociatedViewService associatedViewService,
            IThreadingContext threadingContext)
            : base(associatedViewService, threadingContext)
        {
        }

        protected override bool IsVisible(IWpfTextView view)
            => view.VisualElement.IsVisible;

        protected override DependencyPropertyChangedEventHandler GetVisiblityChangeCallback(VisibleTrackerData visibleTrackerData)
            => (sender, args) => visibleTrackerData.TriggerCallbacks();

        protected override void AddVisibilityChangedCallback(IWpfTextView view, DependencyPropertyChangedEventHandler visibilityChangedCallback)
            => view.VisualElement.IsVisibleChanged += visibilityChangedCallback;

        protected override void RemoveVisibilityChangedCallback(IWpfTextView view, DependencyPropertyChangedEventHandler visibilityChangedCallback)
            => view.VisualElement.IsVisibleChanged -= visibilityChangedCallback;

    }
}
