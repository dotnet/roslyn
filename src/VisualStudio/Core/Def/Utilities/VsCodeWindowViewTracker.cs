// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Utilities;

/// <summary>
/// A helper class that makes getting the "current" position of the cursor in an <see cref="IVsCodeWindow"/> easier to do. This is necessary
/// because a <see cref="IVsCodeWindow"/> can have more than one view if there's a split involved. Watching for cursor changes also requires
/// tracking the lifetime of the views as appropriate.
/// </summary>
/// <remarks>
/// All members of this class are UI thread affinitized, including the constructor.
/// </remarks>
internal sealed class VsCodeWindowViewTracker : IDisposable, IVsCodeWindowEvents
{
    private readonly IVsCodeWindow _codeWindow;
    private readonly IThreadingContext _threadingContext;
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

    private readonly ComEventSink _codeWindowEventsSink;

    /// <summary>
    /// The map from <see cref="IVsTextView"/> and corresponding <see cref="ITextView"/> for views that we are currently watching for caret movements.
    /// </summary>
    private readonly Dictionary<IVsTextView, ITextView> _trackedTextViews = [];

    public VsCodeWindowViewTracker(IVsCodeWindow codeWindow, IThreadingContext threadingContext, IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
    {
        _codeWindow = codeWindow;
        _threadingContext = threadingContext;
        _editorAdaptersFactoryService = editorAdaptersFactoryService;

        _threadingContext.ThrowIfNotOnUIThread();

        _codeWindowEventsSink = ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);

        if (ErrorHandler.Succeeded(_codeWindow.GetPrimaryView(out var pTextView)) && pTextView != null)
            StartTrackingView(pTextView);

        // If there is no secondary view, GetSecondaryView will return null
        if (ErrorHandler.Succeeded(_codeWindow.GetSecondaryView(out pTextView)) && pTextView != null)
            StartTrackingView(pTextView);
    }

    private void StartTrackingView(IVsTextView pTextView)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (!_trackedTextViews.ContainsKey(pTextView))
        {
            var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(pTextView);

            if (wpfTextView != null)
            {
                _trackedTextViews.Add(pTextView, wpfTextView);
                wpfTextView.Caret.PositionChanged += OnCaretPositionChanged;
                wpfTextView.GotAggregateFocus += OnViewGotAggregateFocus;
            }
        }
    }

    private void StopTrackingView(IVsTextView pView)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (_trackedTextViews.TryGetValue(pView, out var view))
        {
            view.Caret.PositionChanged -= OnCaretPositionChanged;
            view.GotAggregateFocus -= OnViewGotAggregateFocus;

            _trackedTextViews.Remove(pView);
        }
    }

    public ITextView GetActiveView()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        ErrorHandler.ThrowOnFailure(_codeWindow.GetLastActiveView(out var pView));
        Contract.ThrowIfNull(pView, $"{nameof(IVsCodeWindow.GetLastActiveView)} returned success, but did not provide a view.");
        var view = _editorAdaptersFactoryService.GetWpfTextView(pView);
        Contract.ThrowIfNull(view, "The active view should be initialized.");
        return view;
    }

    int IVsCodeWindowEvents.OnNewView(IVsTextView pView)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        StartTrackingView(pView);

        return VSConstants.S_OK;
    }

    int IVsCodeWindowEvents.OnCloseView(IVsTextView pView)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        StopTrackingView(pView);

        return VSConstants.S_OK;
    }

    /// <summary>
    /// Raised when the a caret has moved in a view, or when the current view has changed.
    /// </summary>
    /// <remarks>
    /// This is combined into one event since in practice consumers need to respond to either the same way, by refreshing what the current
    /// symbol or token or whatever is.
    /// </remarks>
    public event EventHandler<EventArgs>? CaretMovedOrActiveViewChanged;

    private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        => CaretMovedOrActiveViewChanged?.Invoke(this, e);

    private void OnViewGotAggregateFocus(object sender, EventArgs e)
        => CaretMovedOrActiveViewChanged?.Invoke(this, e);

    public void Dispose()
    {
        // StopTrackingView will update _trackedTextViews; the ToList() avoids modification during enumeration
        foreach (var view in _trackedTextViews.Keys.ToList())
        {
            StopTrackingView(view);
        }

        Debug.Assert(_trackedTextViews.Count == 0);

        _codeWindowEventsSink.Unadvise();
    }
}
