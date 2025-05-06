// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

internal partial class WpfBackgroundWorkIndicatorFactory
{
    /// <summary>
    /// Implementation of an <see cref="IUIThreadOperationContext"/> for the background work indicator.
    /// </summary>
    private sealed partial class BackgroundWorkIndicatorContext : IBackgroundWorkIndicatorContext
    {
        /// <summary>
        /// Cancellation token exposed to clients through <see cref="UserCancellationToken"/>.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly WpfBackgroundWorkIndicatorFactory _factory;
        private readonly string _firstDescription;

        private readonly IBackgroundWorkIndicator _backgroundWorkIndicator;

        /// <summary>
        /// Lock controlling mutation of all data in this indicator, or in any sub-scopes. Any read/write of mutable
        /// data must be protected by this.
        /// </summary>
        public readonly object ContextAndScopeDataMutationGate = new();

        /// <summary>
        /// Set of scopes we have.  We always start with one (the one created by the initial call to create the work
        /// indicator). However, the client of the background indicator can add more.
        /// </summary>
        private ImmutableArray<BackgroundWorkIndicatorScope> _scopes_onlyAccessUnderLock;

        public BackgroundWorkIndicatorContext(
            WpfBackgroundWorkIndicatorFactory factory,
            ITextView textView,
            SnapshotSpan applicableToSpan,
            string firstDescription,
            bool cancelOnEdit,
            bool cancelOnFocusLost)
        {
            _factory = factory;
            _firstDescription = firstDescription;

            _backgroundWorkIndicator = factory._backgroundWorkIndicatorService.Create(
                textView, applicableToSpan, firstDescription, new()
                {
                    CancelOnEdit = cancelOnEdit,
                    CancelOnFocusLost = cancelOnFocusLost,
                });

            // Create a linked token around both the underlying indicator token (which triggers if edits/navigation
            // cause it to be dismissed), and so we can explicitly cancel (if another feature attempts to open a new
            // background work indicator).
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_backgroundWorkIndicator.CancellationToken);

            // Add a single scope representing the how the UI should look initially.
            AddScope(allowCancellation: true, firstDescription);
        }

        public PropertyCollection Properties { get; } = new();

        public CancellationToken UserCancellationToken => _cancellationTokenSource.Token;

        public IEnumerable<IUIThreadOperationScope> Scopes
        {
            get
            {
                lock (ContextAndScopeDataMutationGate)
                    return _scopes_onlyAccessUnderLock;
            }
        }

        public void CancelAndDispose()
        {
            _cancellationTokenSource.Cancel();
            this.Dispose();
        }

        public void Dispose()
        {
            _backgroundWorkIndicator.Dispose();
            _factory.OnContextDisposed(this);
        }

        /// <summary>
        /// The same as Dispose.  Anyone taking ownership of this context wants to show their own UI, so we can just
        /// close ours.
        /// </summary>
        public void TakeOwnership()
            => this.Dispose();

        public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
        {
            lock (this.ContextAndScopeDataMutationGate)
            {
                var scope = new BackgroundWorkIndicatorScope(this, _backgroundWorkIndicator.AddScope(description), description);
                _scopes_onlyAccessUnderLock = _scopes_onlyAccessUnderLock.Add(scope);
                return scope;
            }
        }

        private void RemoveScopeAndReportTotalProgress(BackgroundWorkIndicatorScope scope)
        {
            lock (this.ContextAndScopeDataMutationGate)
            {
                Contract.ThrowIfFalse(_scopes_onlyAccessUnderLock.Contains(scope));
                _scopes_onlyAccessUnderLock = _scopes_onlyAccessUnderLock.Remove(scope);
            }

            ReportTotalProgress();
        }

        private void ReportTotalProgress()
        {
            // Lightup the UI if it supports IProgress
            if (_backgroundWorkIndicator is IProgress<ProgressInfo> underlyingProgress)
                underlyingProgress.Report(GetTotalProgress());

            ProgressInfo GetTotalProgress()
            {
                lock (ContextAndScopeDataMutationGate)
                {
                    var progressInfo = new ProgressInfo();

                    foreach (var scope in _scopes_onlyAccessUnderLock)
                    {
                        var scopeProgressInfo = scope.ProgressInfo_OnlyAccessUnderLock;
                        progressInfo = new ProgressInfo(
                            progressInfo.CompletedItems + scopeProgressInfo.CompletedItems,
                            progressInfo.TotalItems + scopeProgressInfo.TotalItems);
                    }

                    return progressInfo;
                }
            }
        }

        string IUIThreadOperationContext.Description
        {
            get
            {
                // use the description of the last scope if we have one.  We don't have enough room to show all
                // the descriptions at once.
                lock (this.ContextAndScopeDataMutationGate)
                    return _scopes_onlyAccessUnderLock.LastOrDefault()?.Description ?? _firstDescription;
            }
        }

        bool IUIThreadOperationContext.AllowCancellation => true;

        public IDisposable SuppressAutoCancel()
            => _backgroundWorkIndicator.SuppressAutoCancel();
    }
}
