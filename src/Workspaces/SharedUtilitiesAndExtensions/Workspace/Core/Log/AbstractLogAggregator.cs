// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// helper class to aggregate some numeric value log in client side
    /// </summary>
    internal abstract class AbstractLogAggregator<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _map = new(concurrencyLevel: 2, capacity: 2);
        private readonly Func<TKey, TValue> _createCounter;

        protected AbstractLogAggregator()
        {
            _createCounter = _ => CreateCounter();
        }

        protected abstract TValue CreateCounter();

        public bool IsEmpty => _map.IsEmpty;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => _map.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1279909", AllowCaptures = false)]
        protected TValue GetCounter(TKey key)
            => _map.GetOrAdd(key, _createCounter);

        protected bool TryGetCounter(TKey key, out TValue counter)
        {
            if (_map.TryGetValue(key, out counter))
            {
                return true;
            }

            return false;
        }
    }
}
