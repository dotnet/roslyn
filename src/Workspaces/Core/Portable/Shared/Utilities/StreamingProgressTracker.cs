// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

/// <summary>
/// Utility class that can be used to track the progress of an operation in a threadsafe manner.
/// </summary>
internal sealed class StreamingProgressTracker(Func<int, int, CancellationToken, ValueTask>? updateAction = null) : IStreamingProgressTracker
{
    private int _completedItems;
    private int _totalItems;

    public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
    {
        Interlocked.Add(ref _totalItems, count);
        return UpdateAsync(cancellationToken);
    }

    public ValueTask ItemsCompletedAsync(int count, CancellationToken cancellationToken)
    {
        Interlocked.Add(ref _completedItems, count);
        return UpdateAsync(cancellationToken);
    }

    private ValueTask UpdateAsync(CancellationToken cancellationToken)
        => updateAction?.Invoke(Volatile.Read(ref _completedItems), Volatile.Read(ref _totalItems), cancellationToken) ?? default;
}
