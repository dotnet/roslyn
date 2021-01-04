// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Utilities
{
    /// <summary>
    /// Wrapper around an IOperationScope to bridge it to an IProgressTracker.
    /// </summary>
    internal class OperationScopeProgressTracker : IProgressTracker
    {
        private readonly IOperationScope _operationScope;

        private readonly object _gate = new();
        private int _completedItems;
        private int _totalItems;

        public OperationScopeProgressTracker(IOperationScope operationScope)
        {
            _operationScope = operationScope;
        }

        public string? Description { get => _operationScope.Description; set => _operationScope.Description = value ?? ""; }

        public int CompletedItems => _completedItems;

        public int TotalItems => _totalItems;

        public void AddItems(int count)
        {
            int completedItems, totalItems;
            lock (_gate)
            {
                _totalItems += count;
                completedItems = _completedItems;
                totalItems = _totalItems;
            }

            _operationScope.Progress.Report(new ProgressInfo(completedItems, totalItems));
        }

        public void ItemCompleted()
        {
            int completedItems, totalItems;
            lock (_gate)
            {
                _completedItems++;
                completedItems = _completedItems;
                totalItems = _totalItems;
            }

            _operationScope.Progress.Report(new ProgressInfo(completedItems, totalItems));
        }

        public void Clear()
        {
            lock (_gate)
            {
                _completedItems = 0;
                _totalItems = 0;
            }

            _operationScope.Progress.Report(new ProgressInfo(0, 0));
        }
    }
}
