// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation
{
    internal class UIThreadOperationContextProgressTracker : IProgressTracker
    {
        private readonly IUIThreadOperationScope _scope;

        private readonly object _gate = new();

        public UIThreadOperationContextProgressTracker(IUIThreadOperationScope scope)
        {
            Contract.ThrowIfNull(scope);
            _scope = scope;
        }

        public string? Description { get => _scope.Description; set => _scope.Description = value; }

        public int CompletedItems { get; private set; }

        public int TotalItems { get; private set; }

        public void AddItems(int count)
        {
            ProgressInfo progressInfo;
            lock (_gate)
            {
                TotalItems += count;
                progressInfo = new ProgressInfo(CompletedItems, TotalItems);
            }

            _scope.Progress.Report(progressInfo);
        }

        public void ItemCompleted()
        {
            ProgressInfo progressInfo;
            lock (_gate)
            {
                CompletedItems++;
                progressInfo = new ProgressInfo(CompletedItems, TotalItems);
            }

            _scope.Progress.Report(progressInfo);
        }

        public void Clear()
        {
            lock (_gate)
            {
                CompletedItems = 0;
                TotalItems = 0;
            }

            _scope.Progress.Report(new ProgressInfo());
        }
    }
}
