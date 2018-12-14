// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Like ConcurrentDictionary, but single threaded valueFactory execution in GetOrAdd.
    /// </summary>
    /// <remarks>Useful for long running valueFactory functions, like say performing 
    /// dataflow analysis.  This way DFA is invoked only once per key, even if multiple
    /// threads simultaneously request the same key.</remarks>
    internal class SingleThreadedConcurrentDictionary<TKey, TValue>
    {
        /// <summary>
        /// Holds the real values.
        /// </summary>
        private ConcurrentDictionary<TKey, TValue> BackingDictionary = new ConcurrentDictionary<TKey, TValue>();

        /// <summary>
        /// Holds the locks for when creating a value to be inserted.
        /// </summary>
        private ConcurrentDictionary<TKey, object> LockDictionary = new ConcurrentDictionary<TKey, object>();

        /// <summary>
        /// Adds a key/value pair using the specified function if the key does not already exist.  Returns the new value, or the existing value if the key exists.
        /// </summary>
        /// <param name="key">Key to add.</param>
        /// <param name="valueFactory">Function to be invoked to generate the key, if necessary.</param>
        /// <returns>Value of the key, which will either be the existing value, or new value if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (this.BackingDictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }

            // The return value of ConcurrentDictionary.GetOrAdd() is
            // consistent, i.e. the same instance no matter how many times
            // this valueFactory gets executed.  So for a given key, we'll
            // always get back the same lockObject instance.
            object lockObject = this.LockDictionary.GetOrAdd(key, (_) => new object());
            lock (lockObject)
            {
                if (this.BackingDictionary.TryGetValue(key, out value))
                {
                    return value;
                }

                value = valueFactory(key);

                TValue getOrAddedValue = this.BackingDictionary.GetOrAdd(key, value);
                Debug.Assert(Object.ReferenceEquals(getOrAddedValue, value), "Unexpected race condition");
                return getOrAddedValue;
            }
        }
    }
}
