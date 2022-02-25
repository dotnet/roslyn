// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

internal partial class WpfBackgroundWorkIndicatorFactory
{
    /// <summary>
    /// Implementation of an <see cref="IUIThreadOperationScope"/> for the background work indicator. Allows for
    /// features to create nested work with descriptions/progress that will update the all-up indicator tool-tip
    /// shown to the user.
    /// </summary>
    private sealed class BackgroundWorkIndicatorScope : IUIThreadOperationScope, IProgress<ProgressInfo>
    {
        private readonly BackgroundWorkIndicatorContext _context;

        // Mutable state of this scope.  Can be mutated by a client, at which point we'll ask our owning context to
        // update the tooltip accordingly.

        private string _description;
        private ProgressInfo _progressInfo;

        public IUIThreadOperationContext Context => _context;
        public IProgress<ProgressInfo> Progress => this;

        public BackgroundWorkIndicatorScope(
            BackgroundWorkIndicatorContext indicator, string description)
        {
            _context = indicator;
            _description = description;
        }

        /// <summary>
        /// Retrieves a threadsafe snapshot of our data for our owning context to use to build the tooltip ui.
        /// </summary>
        public (string description, ProgressInfo progressInfo) ReadData_MustBeCalledUnderLock()
        {
            Contract.ThrowIfFalse(Monitor.IsEntered(_context.Gate));
            return (_description, _progressInfo);
        }

        /// <summary>
        /// On disposal, just remove ourselves from our parent context.  It will update the UI accordingly.
        /// </summary>
        void IDisposable.Dispose()
            => _context.RemoveScope(this);

        bool IUIThreadOperationScope.AllowCancellation
        {
            get => true;
            set { }
        }

        string IUIThreadOperationScope.Description
        {
            get
            {
                lock (_context.Gate)
                    return _description;
            }
            set
            {
                lock (_context.Gate)
                {
                    _description = value;
                }

                // We changed.  Enqueue work to make sure the UI reflects this.
                _context.EnqueueUIUpdate();
            }
        }

        void IProgress<ProgressInfo>.Report(ProgressInfo value)
        {
            lock (_context.Gate)
            {
                _progressInfo = value;
            }

            // We changed.  Enqueue work to make sure the UI reflects this.
            _context.EnqueueUIUpdate();
        }
    }
}
