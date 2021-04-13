// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Utility class that can be used to track the progress of an operation in a threadsafe manner.
    /// </summary>
    internal sealed class StreamingProgressTracker : IStreamingProgressTracker
    {
        private int _completedItems;
        private int _totalItems;

        private readonly Func<int, int, ValueTask>? _updateAction;

        public StreamingProgressTracker(Func<int, int, ValueTask>? updateAction = null)
            => _updateAction = updateAction;

        public ValueTask AddItemsAsync(int count)
        {
            Interlocked.Add(ref _totalItems, count);
            return UpdateAsync();
        }

        public ValueTask ItemCompletedAsync()
        {
            Interlocked.Increment(ref _completedItems);
            return UpdateAsync();
        }

        private ValueTask UpdateAsync()
        {
            if (_updateAction == null)
            {
                return default;
            }

            return _updateAction(_completedItems, _totalItems);
        }
    }
}
