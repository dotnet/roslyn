// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Analyzer.Utilities
{
    /// <summary>
    /// Provides bounded cache for analyzers.
    /// Acts as a good alternative to <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/>
    /// when the cached value has a cyclic reference to the key preventing early garbage collection of entries.
    /// </summary>
    internal class BoundedCache<TKey, TValue> : BoundedCacheWithFactory<TKey, TValue>
        where TKey : class
        where TValue : new()
    {
        public TValue GetOrCreateValue(TKey key)
        {
            return GetOrCreateValue(key, CreateDefaultValue);

            // Local functions.
            static TValue CreateDefaultValue(TKey _)
                => new TValue();
        }
    }
}
