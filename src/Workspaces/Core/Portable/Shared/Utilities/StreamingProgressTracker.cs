// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Utility class that can be used to track the progress of an operation in a threadsafe manner.
    /// </summary>
    internal class StreamingProgressTracker : IStreamingProgressTracker
    {
        private int _completedItems;
        private int _totalItems;

        private readonly Func<int, int, CancellationToken, Task> _updateActionOpt;

        public StreamingProgressTracker()
            : this(null)
        {
        }

        public StreamingProgressTracker(Func<int, int, CancellationToken, Task> updateActionOpt)
        {
            _updateActionOpt = updateActionOpt;
        }

        public Task AddItemsAsync(int count, CancellationToken cancellationToken)
        {
            Interlocked.Add(ref _totalItems, count);
            return UpdateAsync(cancellationToken);
        }

        public Task ItemCompletedAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _completedItems);
            return UpdateAsync(cancellationToken);
        }

        private Task UpdateAsync(CancellationToken cancellationToken)
        {
            if (_updateActionOpt == null)
            {
                return SpecializedTasks.EmptyTask;
            }

            return _updateActionOpt(_completedItems, _totalItems, cancellationToken);
        }
    }
}