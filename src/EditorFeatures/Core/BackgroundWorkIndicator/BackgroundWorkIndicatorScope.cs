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
        private sealed class BackgroundWorkIndicatorScope : IUIThreadOperationScope, IProgress<ProgressInfo>
        {
            private readonly BackgroundWorkIndicatorContext _owner;
            private readonly BackgroundWorkOperationScope _underlyingScope;

            // Mutable state of this scope.  Can be mutated by a client, at which point we'll ask our owning context to
            // update the tooltip accordingly. Must hold _owner.ContextAndScopeDataMutationGate while reading/writing these.

            private string _currentDescription_OnlyAccessUnderLock = null!;

            public ProgressInfo ProgressInfo_OnlyAccessUnderLock;

            public BackgroundWorkIndicatorScope(
                BackgroundWorkIndicatorContext owner,
                BackgroundWorkOperationScope scope,
                string initialDescription)
            {
                _owner = owner;
                _underlyingScope = scope;

                // Ensure we push through the initial description.
                this.Description = initialDescription;
            }

            public IUIThreadOperationContext Context => _owner;
            public IProgress<ProgressInfo> Progress => this;

            void IDisposable.Dispose()
            {
                // Clear out the underlying platform scope.
                _underlyingScope.Dispose();

                // Remove ourselves from our parent context as well. And ensure that any progress showing is updated accordingly.
                _owner.RemoveScopeAndReportTotalProgress(this);
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
                    lock (_owner.ContextAndScopeDataMutationGate)
                        return _currentDescription_OnlyAccessUnderLock;
                }

                set
                {
                    lock (_owner.ContextAndScopeDataMutationGate)
                    {
                        // Nothing to do if the actual value didn't change.
                        if (value == _currentDescription_OnlyAccessUnderLock)
                            return;

                        _currentDescription_OnlyAccessUnderLock = value;
                    }

                    // Pass through the description to the underlying scope to update the UI.
                    _underlyingScope.Description = value;
                }
            }

            void IProgress<ProgressInfo>.Report(ProgressInfo value)
            {
                lock (_owner.ContextAndScopeDataMutationGate)
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
                _owner.ReportTotalProgress();
            }
        }
    }
}
