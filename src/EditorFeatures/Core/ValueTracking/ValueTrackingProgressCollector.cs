// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    internal class ValueTrackingProgressCollector : IProgress<ValueTrackedItem>
    {
        private readonly object _lock = new();
        private readonly Stack<ValueTrackedItem> _items = new();

        public event EventHandler<ValueTrackedItem>? OnNewItem;

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
                return _items.ToImmutableArray();
            }
        }
    }
}
