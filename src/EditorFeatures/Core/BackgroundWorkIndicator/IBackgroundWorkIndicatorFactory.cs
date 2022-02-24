// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
{
    /// <summary>
    /// Factory for creating lightweight <see cref="IUIThreadOperationContext"/>s that can sit in the editor in a
    /// unobtrusive fashion unlike the Threaded-Wait-Dialog.  Features can use this to indicate to users that work
    /// is happening in the background while not blocking the user from continuing to work with their code.
    /// </summary>
    internal interface IBackgroundWorkIndicatorFactory
    {
        /// <summary>
        /// Creates a new background work indicator to notify the user that work is happening.  The work always starts
        /// initially cancellable, but this can be overridden by calling <see
        /// cref="IUIThreadOperationContext.AddScope"/> and passing in <see langword="false"/> for <c>allowCancellation</c>.
        /// </summary>
        /// <remarks>
        /// Default cancellation behavior can also be specified through <paramref name="cancelOnEdit"/> and <paramref
        /// name="cancelOnFocusLost"/>. However, this cancellation will only happen if the context is cancellable at the
        /// time those respective events happen.
        /// </remarks>
        IUIThreadOperationContext Create(
            ITextView textView, SnapshotSpan applicableToSpan,
            string description, bool cancelOnEdit = true, bool cancelOnFocusLost = true);
    }

    [Export(typeof(IBackgroundWorkIndicatorFactory)), Shared]
    internal class BackgroundWorkIndicatorFactory : IBackgroundWorkIndicatorFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IToolTipPresenterFactory _toolTipPresenterFactory;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BackgroundWorkIndicatorFactory(
            IThreadingContext threadingContext,
            IToolTipPresenterFactory toolTipPresenterFactory,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _toolTipPresenterFactory = toolTipPresenterFactory;
            _listener = listenerProvider.GetListener(FeatureAttribute.QuickInfo);
        }

        public IUIThreadOperationContext Create(
            ITextView textView,
            SnapshotSpan applicableToSpan,
            string description,
            bool cancelOnEdit,
            bool cancelOnFocusLost)
        {
            // Create the indicator in its default/empty state.
            var indicator = (IUIThreadOperationContext)new BackgroundWorkIndicator(
                this, textView, applicableToSpan, description,
                cancelOnEdit, cancelOnFocusLost);

            // Then add a single scope representing the how the UI should look initially.
            indicator.AddScope(allowCancellation: true, description);
            return indicator;
        }

        private class BackgroundWorkIndicator : IUIThreadOperationContext
        {
            private enum UpdateRequest
            {
                UpdateTooltip,
                DismissTooltip,
            }

            private readonly CancellationTokenSource _cancellationTokenSource = new();

            /// <summary>
            /// Lock controlling mutation of all data (except <see cref="_dismissed"/>) in this indicator, or in any
            /// sub-scopes. Any read/write of mutable data must be protected by this.
            /// </summary>
            public readonly object Gate = new();

            private readonly BackgroundWorkIndicatorFactory _factory;
            private readonly ITextView _textView;
            private readonly ITextBuffer _subjectBuffer;
            private readonly IToolTipPresenter _toolTipPresenter;
            private readonly ITrackingSpan _trackingSpan;
            private readonly string _firstDescription;
            private readonly bool _cancelOnEdit;
            private readonly bool _cancelOnFocusLost;

            /// <summary>
            /// Work queue used to batch up UI update and Dispose requests.  A value of <see langword="true"/> means
            /// just update the tool-tip. A value of <see langword="false"/> means we want to dismiss the tool-tip.
            /// </summary>
            private readonly AsyncBatchingWorkQueue<UpdateRequest> _uiUpdateQueue;

            /// <summary>
            /// Set of scopes we have.  We always start with one (the one created by the initial call to create the work
            /// indicator). However, the client of the background indicator can add more.
            /// </summary>
            private ImmutableArray<BackgroundWorkIndicatorScope> _scopes = ImmutableArray<BackgroundWorkIndicatorScope>.Empty;

            /// <summary>
            /// If we've been dismissed or not.  Once dismissed, we will close the tool-tip showing information.  This
            /// field must only be accessed on the UI thread.
            /// </summary>
            private bool _dismissed = false;

            private IThreadingContext ThreadingContext => _factory._threadingContext;

            public PropertyCollection Properties { get; } = new();
            public CancellationToken UserCancellationToken => _cancellationTokenSource.Token;
            public IEnumerable<IUIThreadOperationScope> Scopes => _scopes;

            public BackgroundWorkIndicator(
                BackgroundWorkIndicatorFactory factory,
                ITextView textView,
                SnapshotSpan applicableToSpan,
                string firstDescription,
                bool cancelOnEdit,
                bool cancelOnFocusLost)
            {
                _factory = factory;
                _textView = textView;
                _subjectBuffer = applicableToSpan.Snapshot.TextBuffer;

                // Create a tool-tip at the requested position.  Turn off all default behavior for it.  We'll be
                // controlling everything ourselves.
                _toolTipPresenter = factory._toolTipPresenterFactory.Create(textView, new ToolTipParameters(
                    trackMouse: false,
                    ignoreBufferChange: true,
                    keepOpenFunc: null,
                    ignoreCaretPositionChange: true,
                    dismissWhenOffscreen: false));

                _trackingSpan = applicableToSpan.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

                _firstDescription = firstDescription;
                _cancelOnEdit = cancelOnEdit;
                _cancelOnFocusLost = cancelOnFocusLost;

                _uiUpdateQueue = new AsyncBatchingWorkQueue<UpdateRequest>(
                    DelayTimeSpan.Short,
                    UpdateUIAsync,
                    EqualityComparer<UpdateRequest>.Default,
                    factory._listener,
                    _cancellationTokenSource.Token);

                if (cancelOnEdit)
                    _subjectBuffer.Changed += OnTextBufferChanged;

                if (cancelOnFocusLost)
                    textView.LostAggregateFocus += OnTextViewLostAggregateFocus;
            }

            public void Dispose()
                => _uiUpdateQueue.AddWork(UpdateRequest.DismissTooltip);

            /// <summary>
            /// Called after anyone consuming us makes a change that should be reflected in the UI.
            /// </summary>
            internal void EnqueueUIUpdate()
                => _uiUpdateQueue.AddWork(UpdateRequest.UpdateTooltip);

            /// <summary>
            /// The same as Dispose.  Anyone taking ownership of this context wants to show their own UI, so we can just
            /// close ours.
            /// </summary>
            public void TakeOwnership()
                => this.Dispose();

            private void OnTextBufferChanged(object? sender, TextContentChangedEventArgs e)
                => OnEditorCancellationEvent();

            private void OnTextViewLostAggregateFocus(object? sender, EventArgs e)
                => OnEditorCancellationEvent();

            private void OnEditorCancellationEvent()
            {
                // Only actually cancel us if that's allowed right now.
                if (this.ReadData().allowCancellation)
                {
                    _cancellationTokenSource.Cancel();
                    this.Dispose();
                }
            }

            private ValueTask UpdateUIAsync(ImmutableArray<UpdateRequest> requests, CancellationToken cancellationToken)
            {
                Contract.ThrowIfTrue(requests.IsDefaultOrEmpty, "We must have gotten an actual request to process.");
                Contract.ThrowIfTrue(requests.Length > 2, "At most we can have two requests in the queue (one to update, one to dismiss).");
                Contract.ThrowIfFalse(
                    requests.Contains(UpdateRequest.DismissTooltip) || requests.Contains(UpdateRequest.UpdateTooltip),
                    "We didn't get an actual event we know about.");

                return requests.Contains(UpdateRequest.DismissTooltip)
                    ? DismissUIAsync()
                    : UpdateUIAsync();

                async ValueTask DismissUIAsync()
                {
                    await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    // Ensure we only dismiss once.
                    if (_dismissed)
                        return;

                    _dismissed = true;

                    // Unhook any event handlers we've setup.
                    if (_cancelOnEdit)
                        _subjectBuffer.Changed -= OnTextBufferChanged;

                    if (_cancelOnFocusLost)
                        _textView.LostAggregateFocus -= OnTextViewLostAggregateFocus;

                    // Finally, dismiss the actual tool-tip.
                    _toolTipPresenter.Dismiss();
                }

                async ValueTask UpdateUIAsync()
                {
                    // Build the current description in the background, then switch to the UI thread to actually update the
                    // tool-tip with it.
                    var data = this.ReadData();

                    await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    // If we've been dismissed already, then no point in continuing.
                    if (_dismissed)
                        return;

                    // Todo: build a richer tool-tip that makes use of things like the progress reported, and perhaps has a
                    // close button.
                    _toolTipPresenter.StartOrUpdate(_trackingSpan, new[] { data.description });
                }
            }

            public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
            {
                var scope = new BackgroundWorkIndicatorScope(this, allowCancellation, description);
                lock (this.Gate)
                {
                    _scopes = _scopes.Add(scope);
                }

                // We changed.  Enqueue work to make sure the UI reflects this.
                this.EnqueueUIUpdate();
                return scope;
            }

            internal void RemoveScope(BackgroundWorkIndicatorScope scope)
            {
                lock (this.Gate)
                {
                    Contract.ThrowIfFalse(_scopes.Contains(scope));
                    _scopes = _scopes.Remove(scope);
                }

                // We changed.  Enqueue work to make sure the UI reflects this.
                this.EnqueueUIUpdate();
            }

            private (bool allowCancellation, string description, ProgressInfo progressInfo) ReadData()
            {
                lock (Gate)
                {
                    var allowCancellation = true;
                    var description = _firstDescription;
                    var progressInfo = new ProgressInfo();

                    foreach (var scope in _scopes)
                    {
                        // We're cancellable if all our scopes are cancellable.
                        allowCancellation = allowCancellation && scope.AllowCancellation;

                        // use the description of the last scope if we have one.  We don't have enough room to show all
                        // the descriptions at once.
                        description = scope.Description;

                        var scopeProgressInfo = scope.ProgressInfo;
                        progressInfo = new ProgressInfo(
                            progressInfo.CompletedItems + scopeProgressInfo.CompletedItems,
                            progressInfo.TotalItems + scopeProgressInfo.TotalItems);
                    }

                    return (allowCancellation, description, progressInfo);
                }
            }

            public bool AllowCancellation => ReadData().allowCancellation;
            public string Description => ReadData().description;
        }

        private class BackgroundWorkIndicatorScope : IUIThreadOperationScope, IProgress<ProgressInfo>
        {
            private readonly BackgroundWorkIndicator _indicator;

            private bool _allowCancellation;
            private string _description;
            public ProgressInfo ProgressInfo;

            public IUIThreadOperationContext Context => _indicator;
            public IProgress<ProgressInfo> Progress => this;

            public BackgroundWorkIndicatorScope(
                BackgroundWorkIndicator indicator, bool allowCancellation, string description)
            {
                _indicator = indicator;
                _allowCancellation = allowCancellation;
                _description = description;
            }

            public void Dispose()
            {
                _indicator.RemoveScope(this);
            }

            public bool AllowCancellation
            {
                get
                {
                    lock (_indicator.Gate)
                        return _allowCancellation;
                }
                set
                {
                    lock (_indicator.Gate)
                    {
                        _allowCancellation = value;
                    }

                    // We changed.  Enqueue work to make sure the UI reflects this.
                    _indicator.EnqueueUIUpdate();
                }
            }

            public string Description
            {
                get
                {
                    lock (_indicator.Gate)
                        return _description;
                }
                set
                {
                    lock (_indicator.Gate)
                    {
                        _description = value;
                    }

                    // We changed.  Enqueue work to make sure the UI reflects this.
                    _indicator.EnqueueUIUpdate();
                }
            }

            public void Report(ProgressInfo value)
            {
                lock (_indicator.Gate)
                {
                    this.ProgressInfo = value;
                }

                // We changed.  Enqueue work to make sure the UI reflects this.
                _indicator.EnqueueUIUpdate();
            }
        }
    }
}
