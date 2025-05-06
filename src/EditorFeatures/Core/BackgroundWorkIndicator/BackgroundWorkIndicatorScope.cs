// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

internal partial class WpfBackgroundWorkIndicatorFactory
{
    private sealed partial class BackgroundWorkIndicatorContext
    {
        /// <summary>
        /// Implementation of an <see cref="IUIThreadOperationScope"/> for the background work indicator. Allows for
        /// features to create nested work with descriptions/progress that will update the all-up indicator tool-tip
        /// shown to the user.
        /// </summary>
        private sealed class BackgroundWorkIndicatorScope(
            BackgroundWorkIndicatorContext indicator,
            BackgroundWorkOperationScope scope,
            string initialDescription) : IUIThreadOperationScope, IProgress<ProgressInfo>
        {
            private readonly BackgroundWorkIndicatorContext _context = indicator;
            private readonly BackgroundWorkOperationScope _scope = scope;

            // Mutable state of this scope.  Can be mutated by a client, at which point we'll ask our owning context to
            // update the tooltip accordingly.

            private string _currentDescription = initialDescription;
            public ProgressInfo ProgressInfo_OnlyAccessUnderLock;

            public IUIThreadOperationContext Context => _context;
            public IProgress<ProgressInfo> Progress => this;

            void IDisposable.Dispose()
            {
                // Clear out the underlying platform scope.
                _scope.Dispose();

                // Remove ourselves from our parent context as well. And ensure that any progress showing is updated accordingly.
                _context.RemoveScopeAndReportTotalProgress(this);
            }

            bool IUIThreadOperationScope.AllowCancellation
            {
                get => true;
                set { }
            }

            public string Description
            {
                get
                {
                    lock (_context.ContextAndScopeDataMutationGate)
                        return _currentDescription;
                }
                set
                {
                    lock (_context.ContextAndScopeDataMutationGate)
                    {
                        // Nothing to do if the actual value didn't change.
                        if (value == _currentDescription)
                            return;

                        _currentDescription = value;
                    }

                    // Pass through the description to the underlying scope to update the UI.
                    _scope.Description = value;
                }
            }

            void IProgress<ProgressInfo>.Report(ProgressInfo value)
            {
                lock (_context.ContextAndScopeDataMutationGate)
                {
                    // Nothing to do if the actual value didn't change.
                    if (value.TotalItems == ProgressInfo_OnlyAccessUnderLock.TotalItems &&
                        value.CompletedItems == ProgressInfo_OnlyAccessUnderLock.CompletedItems)
                    {
                        return;
                    }

                    ProgressInfo_OnlyAccessUnderLock = value;
                }

                // Now update the UI with the total progress so far of all the scopes.
                _context.ReportTotalProgress();
            }
        }
    }
}
