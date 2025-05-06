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
        /// Lock controlling mutation of all data in this indicator, or in any sub-scopes. Any read/write of mutable
        /// data must be protected by this.
        /// </summary>
        public readonly object ContextAndScopeMutationGate = new();

        private readonly IBackgroundWorkIndicator _backgroundWorkIndicator;
        private readonly string _firstDescription;

        /// <summary>
        /// Set of scopes we have.  We always start with one (the one created by the initial call to create the work
        /// indicator). However, the client of the background indicator can add more.
        /// </summary>
        private ImmutableArray<BackgroundWorkIndicatorScope> _scopes;

        public PropertyCollection Properties { get; } = new();

        public CancellationToken UserCancellationToken => _backgroundWorkIndicator.CancellationToken;
        public IEnumerable<IUIThreadOperationScope> Scopes => _scopes;

        public BackgroundWorkIndicatorContext(
            WpfBackgroundWorkIndicatorFactory factory,
            ITextView textView,
            SnapshotSpan applicableToSpan,
            string firstDescription,
            bool cancelOnEdit,
            bool cancelOnFocusLost)
        {
            _backgroundWorkIndicator = factory._backgroundWorkIndicatorService.Create(
                textView, applicableToSpan, firstDescription, new()
                {
                    CancelOnEdit = cancelOnEdit,
                    CancelOnFocusLost = cancelOnFocusLost,
                });

            _firstDescription = firstDescription;

            // Add a single scope representing the how the UI should look initially.
            AddScope(allowCancellation: true, firstDescription);
        }

        public void Dispose()
            => _backgroundWorkIndicator.Dispose();

        /// <summary>
        /// The same as Dispose.  Anyone taking ownership of this context wants to show their own UI, so we can just
        /// close ours.
        /// </summary>
        public void TakeOwnership()
            => this.Dispose();

        public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
        {
            lock (this.ContextAndScopeMutationGate)
            {
                var scope = new BackgroundWorkIndicatorScope(this, _backgroundWorkIndicator.AddScope(description), description);
                _scopes = _scopes.Add(scope);
                return scope;
            }
        }

        private void RemoveScopeAndReportTotalProgress(BackgroundWorkIndicatorScope scope)
        {
            lock (this.ContextAndScopeMutationGate)
            {
                Contract.ThrowIfFalse(_scopes.Contains(scope));
                _scopes = _scopes.Remove(scope);
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
                lock (ContextAndScopeMutationGate)
                {
                    var progressInfo = new ProgressInfo();

                    foreach (var scope in _scopes)
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
                lock (this.ContextAndScopeMutationGate)
                    return _scopes.LastOrDefault()?.Description ?? _firstDescription;
            }
        }

        bool IUIThreadOperationContext.AllowCancellation => true;

        public IDisposable SuppressAutoCancel()
            => _backgroundWorkIndicator.SuppressAutoCancel();
    }
}
