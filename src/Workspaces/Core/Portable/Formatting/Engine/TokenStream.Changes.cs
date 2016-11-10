// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class TokenStream
    {
        /// <summary>
        /// Thread-safe collection that holds onto changes
        /// </summary>
        private struct Changes
        {
            public const int BeginningOfTreeKey = -1;
            public const int EndOfTreeKey = -2;

            // Created lazily
            private ConcurrentDictionary<int, TriviaData> _map;

            public bool TryRemove(int pairIndex)
            {
                TriviaData temp;
                return _map?.TryRemove(pairIndex, out temp) ?? false;
            }

            public void AddOrReplace(int key, TriviaData triviaInfo)
            {
                // PERF: Set the concurrency level to 1 because, while the dictionary has to be thread-safe,
                // there is very little contention in formatting. A lower concurrency level reduces object
                // allocations which are used internally by ConcurrentDictionary for locking.
                var map = LazyInitialization.EnsureInitialized(ref _map, () => new ConcurrentDictionary<int, TriviaData>(concurrencyLevel: 1, capacity: 8));
                map[key] = triviaInfo;
            }

            public bool TryGet(int key, out TriviaData triviaInfo)
            {
                triviaInfo = null;
                return _map?.TryGetValue(key, out triviaInfo) ?? false;
            }
        }
    }
}
