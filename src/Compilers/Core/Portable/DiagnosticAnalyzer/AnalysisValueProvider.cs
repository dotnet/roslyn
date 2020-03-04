// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class AnalysisValueProvider<TKey, TValue>
        where TKey : class
    {
        private readonly Func<TKey, TValue> _computeValue;

        // This provider holds a weak reference to the key-value pairs, as AnalysisValueProvider might outlive individual compilations.
        // CompilationAnalysisValueProvider, which wraps this provider and lives for the lifetime of specific compilation, holds a strong reference to the key-value pairs, providing an overall performance benefit.
        private readonly ConditionalWeakTable<TKey, WrappedValue> _valueCache;
        private readonly ConditionalWeakTable<TKey, WrappedValue>.CreateValueCallback _valueCacheCallback;

        internal IEqualityComparer<TKey> KeyComparer { get; private set; }

        public AnalysisValueProvider(Func<TKey, TValue> computeValue, IEqualityComparer<TKey> keyComparer)
        {
            _computeValue = computeValue;
            KeyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _valueCache = new ConditionalWeakTable<TKey, WrappedValue>();
            _valueCacheCallback = new ConditionalWeakTable<TKey, WrappedValue>.CreateValueCallback(ComputeValue);
        }

        private sealed class WrappedValue
        {
            public TValue Value { get; set; }
        }

        private WrappedValue ComputeValue(TKey key)
        {
            var value = _computeValue(key);
            return new WrappedValue { Value = value };
        }

        internal bool TryGetValue(TKey key, out TValue value)
        {
            // Catch any exceptions from the computeValue callback, which calls into user code.
            try
            {
                value = _valueCache.GetValue(key, _valueCacheCallback).Value;
                return true;
            }
            catch (Exception)
            {
                value = default(TValue);
                return false;
            }
        }
    }
}
