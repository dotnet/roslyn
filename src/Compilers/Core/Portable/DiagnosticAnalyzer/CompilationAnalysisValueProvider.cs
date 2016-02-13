// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Wrapper over the core <see cref="AnalysisValueProvider{TKey, TValue}"/> which holds a strong reference to key-value pairs for the lifetime of a compilation that this provider is associated with.
    /// This ensures that values are never re-computed for equivalent keys while analyzing each compilation, improving overall analyzer performance.
    /// </summary>
    internal sealed class CompilationAnalysisValueProvider<TKey, TValue>
        where TKey : class
    {
        private readonly AnalysisValueProvider<TKey, TValue> _analysisValueProvider;
        private readonly Dictionary<TKey, TValue> _valueMap;

        public CompilationAnalysisValueProvider(AnalysisValueProvider<TKey, TValue> analysisValueProvider)
        {
            _analysisValueProvider = analysisValueProvider;
            _valueMap = new Dictionary<TKey, TValue>(analysisValueProvider.KeyComparer);
        }

        internal bool TryGetValue(TKey key, out TValue value)
        {
            // First try to get the cached value for this compilation.
            lock (_valueMap)
            {
                if (_valueMap.TryGetValue(key, out value))
                {
                    return true;
                }
            }

            // Ask the core analysis value provider for the value.
            // We do it outside the lock statement as this may call into user code which can be a long running operation.
            if (!_analysisValueProvider.TryGetValue(key, out value))
            {
                value = default(TValue);
                return false;
            }

            // Store the value for the lifetime of the compilation.
            lock (_valueMap)
            {
                // Check if another thread already stored the computed value.
                TValue storedValue;
                if (_valueMap.TryGetValue(key, out storedValue))
                {
                    // If so, we return the stored value.
                    value = storedValue;
                }
                else
                {
                    // Otherwise, store the value computed here.
                    _valueMap.Add(key, value);
                }
            }

            return true;
        }
    }
}
