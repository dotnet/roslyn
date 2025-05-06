// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private sealed class BackgroundWorkIndicatorContext : IBackgroundWorkIndicatorContext
    {
        /// <summary>
        /// Lock controlling mutation of all data in this indicator, or in any sub-scopes. Any read/write of mutable
        /// data must be protected by this.
        /// </summary>
        public readonly object Gate = new();

        private readonly IBackgroundWorkIndicator _backgroundWorkIndicator;
        private readonly string _firstDescription;

        /// <summary>
        /// Set of scopes we have.  We always start with one (the one created by the initial call to create the work
        /// indicator). However, the client of the background indicator can add more.
        /// </summary>
        private ImmutableArray<BackgroundWorkIndicatorScope> _scopes = [];

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
            lock (this.Gate)
            {
                var scope = new BackgroundWorkIndicatorScope(this, _backgroundWorkIndicator.AddScope(description), description);
                _scopes = _scopes.Add(scope);
                return scope;
            }
        }

        public void RemoveScope(BackgroundWorkIndicatorScope scope)
        {
            lock (this.Gate)
            {
                Contract.ThrowIfFalse(_scopes.Contains(scope));
                _scopes = _scopes.Remove(scope);
            }
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

        public IDisposable SuppressAutoCancel()
            => _backgroundWorkIndicator.SuppressAutoCancel();
    }
}
