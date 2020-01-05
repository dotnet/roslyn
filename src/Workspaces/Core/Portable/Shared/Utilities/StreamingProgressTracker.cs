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

        private readonly Func<int, int, Task> _updateActionOpt;

        public StreamingProgressTracker()
            : this(null)
        {
        }

        public StreamingProgressTracker(Func<int, int, Task> updateActionOpt)
        {
            _updateActionOpt = updateActionOpt;
        }

        public Task AddItemsAsync(int count)
        {
            Interlocked.Add(ref _totalItems, count);
            return UpdateAsync();
        }

        public Task ItemCompletedAsync()
        {
            Interlocked.Increment(ref _completedItems);
            return UpdateAsync();
        }

        private Task UpdateAsync()
        {
            if (_updateActionOpt == null)
            {
                return Task.CompletedTask;
            }

            return _updateActionOpt(_completedItems, _totalItems);
        }
    }
}
