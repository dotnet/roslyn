// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Like ConcurrentDictionary, but single threaded valueFactory execution in GetOrAdd.
    /// </summary>
    /// <remarks>Useful for long running valueFactory functions, like say performing 
    /// dataflow analysis.  This way DFA is invoked only once per key, even if multiple
    /// threads simultaneously request the same key.</remarks>
#pragma warning disable CA1812    // SingleThreadedConcurrentDictionary is too used.
    internal class SingleThreadedConcurrentDictionary<TKey, TValue>
        where TValue : class
#pragma warning restore CA1812
    {
        /// <summary>
        /// An Entry itself serves a lock object, and contains the real value.
        /// </summary>
        private class Entry
        {
            public TValue? Value { get; set; }
        }

        /// <summary>
        /// Holds entries, which contain the actual values.
        /// </summary>
        private readonly ConcurrentDictionary<TKey, Entry> BackingDictionary = new ConcurrentDictionary<TKey, Entry>();

        /// <summary>
        /// Adds a key/value pair using the specified function if the key does not already exist.  Returns the new value, or the existing value if the key exists.
        /// </summary>
        /// <param name="key">Key to add.</param>
        /// <param name="valueFactory">Function to be invoked to generate the key, if necessary.</param>
        /// <returns>Value of the key, which will either be the existing value, or new value if the key was not in the dictionary.</returns>
        public TValue? GetOrAdd(TKey key, Func<TKey, TValue?> valueFactory)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            // The return value of ConcurrentDictionary.GetOrAdd() is
            // consistent, i.e. the same instance no matter how many times
            // this valueFactory gets executed.  So for a given key, we'll
            // always get back the same Entry instance.
            Entry entry = this.BackingDictionary.GetOrAdd(key, (_) => new Entry());
            if (entry.Value != null)
            {
                return entry.Value;
            }

            lock (entry)
            {
                if (entry.Value == null)
                {
                    entry.Value = valueFactory(key);
                }
            }

            return entry.Value;
        }
    }
}
