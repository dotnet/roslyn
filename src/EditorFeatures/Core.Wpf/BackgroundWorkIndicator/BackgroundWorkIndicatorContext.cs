// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

internal partial class WpfBackgroundWorkIndicatorFactory
{
    /// <summary>
    /// Implementation of an <see cref="IUIThreadOperationContext"/> for the background work indicator.
    /// </summary>
    private sealed class BackgroundWorkIndicatorContext : IUIThreadOperationContext
    {
        /// <summary>
        /// What sort of UI update request we've enqueued to <see cref="_uiUpdateQueue"/>.
        /// </summary>
        private enum UIUpdateRequest
        {
            UpdateTooltip,
            DismissTooltip,
        }

        /// <summary>
        /// Cancellation token exposed to clients through <see cref="UserCancellationToken"/>.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Lock controlling mutation of all data (except <see cref="_dismissed"/>) in this indicator, or in any
        /// sub-scopes. Any read/write of mutable data must be protected by this.
        /// </summary>
        public readonly object Gate = new();

        private readonly WpfBackgroundWorkIndicatorFactory _factory;
        private readonly ITextView _textView;
        private readonly ITextBuffer _subjectBuffer;
        private readonly IToolTipPresenter _toolTipPresenter;
        private readonly ITrackingSpan _trackingSpan;
        private readonly string _firstDescription;

        /// <summary>
        /// Work queue used to batch up UI update and Dispose requests.  A value of <see langword="true"/> means
        /// just update the tool-tip. A value of <see langword="false"/> means we want to dismiss the tool-tip.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<UIUpdateRequest> _uiUpdateQueue;

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

        public BackgroundWorkIndicatorContext(
            WpfBackgroundWorkIndicatorFactory factory,
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

            _uiUpdateQueue = new AsyncBatchingWorkQueue<UIUpdateRequest>(
                DelayTimeSpan.Medium,
                UpdateUIAsync,
                EqualityComparer<UIUpdateRequest>.Default,
                factory._listener,
                this.ThreadingContext.DisposalToken);

            if (cancelOnEdit)
                _subjectBuffer.Changed += OnTextBufferChanged;

            //if (cancelOnFocusLost)
            //    textView.LostAggregateFocus += OnTextViewLostAggregateFocus;
        }

        public void Dispose()
            => _uiUpdateQueue.AddWork(UIUpdateRequest.DismissTooltip);

        /// <summary>
        /// Called after anyone consuming us makes a change that should be reflected in the UI.
        /// </summary>
        internal void EnqueueUIUpdate()
            => _uiUpdateQueue.AddWork(UIUpdateRequest.UpdateTooltip);

        /// <summary>
        /// The same as Dispose.  Anyone taking ownership of this context wants to show their own UI, so we can just
        /// close ours.
        /// </summary>
        public void TakeOwnership()
            => this.Dispose();

        private void OnTextBufferChanged(object? sender, TextContentChangedEventArgs e)
            => CancelAndDispose();

        private void OnTextViewLostAggregateFocus(object? sender, EventArgs e)
            => CancelAndDispose();

        public void CancelAndDispose()
        {
            _cancellationTokenSource.Cancel();
            this.Dispose();
        }

        private ValueTask UpdateUIAsync(ImmutableArray<UIUpdateRequest> requests, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(requests.IsDefaultOrEmpty, "We must have gotten an actual request to process.");
            Contract.ThrowIfTrue(requests.Length > 2, "At most we can have two requests in the queue (one to update, one to dismiss).");
            Contract.ThrowIfFalse(
                requests.Contains(UIUpdateRequest.DismissTooltip) || requests.Contains(UIUpdateRequest.UpdateTooltip),
                "We didn't get an actual event we know about.");

            return requests.Contains(UIUpdateRequest.DismissTooltip)
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
                _subjectBuffer.Changed -= OnTextBufferChanged;
                _textView.LostAggregateFocus -= OnTextViewLostAggregateFocus;

                // Finally, dismiss the actual tool-tip.
                _toolTipPresenter.Dismiss();

                // Let our factory know that we were disposed so it can let go of us as well.
                _factory.OnContextDisposed(this);
            }

            async ValueTask UpdateUIAsync()
            {
                // Build the current description in the background, then switch to the UI thread to actually update the
                // tool-tip with it.
                var data = this.BuildData();

                await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // If we've been dismissed already, then no point in continuing.
                if (_dismissed)
                    return;

                // Todo: build a richer tool-tip that makes use of things like the progress reported, and perhaps has a
                // close button.
                _toolTipPresenter.StartOrUpdate(
                    _trackingSpan, new[] { string.Format(EditorFeaturesResources._0_Esc_to_cancel, data.description) });
            }
        }

        public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
        {
            var scope = new BackgroundWorkIndicatorScope(this, description);
            lock (this.Gate)
            {
                _scopes = _scopes.Add(scope);
            }

            // We changed.  Enqueue work to make sure the UI reflects this.
            this.EnqueueUIUpdate();
            return scope;
        }

        public void RemoveScope(BackgroundWorkIndicatorScope scope)
        {
            lock (this.Gate)
            {
                Contract.ThrowIfFalse(_scopes.Contains(scope));
                _scopes = _scopes.Remove(scope);
            }

            // We changed.  Enqueue work to make sure the UI reflects this.
            this.EnqueueUIUpdate();
        }

        private (string description, ProgressInfo progressInfo) BuildData()
        {
            lock (Gate)
            {
                var description = _firstDescription;
                var progressInfo = new ProgressInfo();

                foreach (var scope in _scopes)
                {
                    var scopeData = scope.ReadData_MustBeCalledUnderLock();

                    // use the description of the last scope if we have one.  We don't have enough room to show all
                    // the descriptions at once.
                    description = scopeData.description;

                    var scopeProgressInfo = scopeData.progressInfo;
                    progressInfo = new ProgressInfo(
                        progressInfo.CompletedItems + scopeProgressInfo.CompletedItems,
                        progressInfo.TotalItems + scopeProgressInfo.TotalItems);
                }

                return (description, progressInfo);
            }
        }

        string IUIThreadOperationContext.Description => BuildData().description;

        bool IUIThreadOperationContext.AllowCancellation => true;
    }
}
