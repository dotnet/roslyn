// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ValueTracking;

internal sealed class ValueTrackingProgressCollector : IProgress<ValueTrackedItem>
{
    private readonly object _lock = new();
    private readonly Stack<ValueTrackedItem> _items = new();

    public event EventHandler<ValueTrackedItem>? OnNewItem;

    internal ValueTrackedItem? Parent { get; set; }

    public void Report(ValueTrackedItem item)
    {
        lock (_lock)
        {
            _items.Push(item);
        }

        OnNewItem?.Invoke(null, item);
    }

    public ImmutableArray<ValueTrackedItem> GetItems()
    {
        lock (_lock)
        {
            return [.. _items];
        }
    }

    internal async Task<bool> TryReportAsync(Solution solution, Location location, ISymbol symbol, CancellationToken cancellationToken = default)
    {
        var item = await ValueTrackedItem.TryCreateAsync(solution, location, symbol, Parent, cancellationToken).ConfigureAwait(false);
        if (item is not null)
        {
            Report(item);
            return true;
        }

        return false;
    }
}
