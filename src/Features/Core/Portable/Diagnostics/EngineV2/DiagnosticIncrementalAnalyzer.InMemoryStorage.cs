// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private static class InMemoryStorage
        {
            // the reason using nested map rather than having tuple as key is so that I dont have a gigantic map
            private readonly static ConcurrentDictionary<DiagnosticAnalyzer, ConcurrentDictionary<(object key, string stateKey), CacheEntry>> s_map =
                new ConcurrentDictionary<DiagnosticAnalyzer, ConcurrentDictionary<(object key, string stateKey), CacheEntry>>(concurrencyLevel: 2, capacity: 10);

            public static bool TryGetValue(DiagnosticAnalyzer analyzer, (object key, string stateKey) key, out CacheEntry entry)
            {
                AssertKey(key);

                entry = default;
                if (!s_map.TryGetValue(analyzer, out var analyzerMap) ||
                    !analyzerMap.TryGetValue(key, out entry))
                {
                    return false;
                }

                return true;
            }

            public static void Cache(DiagnosticAnalyzer analyzer, (object key, string stateKey) key, CacheEntry entry)
            {
                AssertKey(key);

                // add new cache entry
                var analyzerMap = s_map.GetOrAdd(analyzer, _ => new ConcurrentDictionary<(object key, string stateKey), CacheEntry>(concurrencyLevel: 2, capacity: 10));
                analyzerMap[key] = entry;
            }

            public static void Remove(DiagnosticAnalyzer analyzer, (object key, string stateKey) key)
            {
                AssertKey(key);
                // remove the entry
                if (!s_map.TryGetValue(analyzer, out var analyzerMap))
                {
                    return;
                }

                analyzerMap.TryRemove(key, out var entry);

                if (analyzerMap.IsEmpty)
                {
                    s_map.TryRemove(analyzer, out analyzerMap);
                }
            }

            public static void DropCache(DiagnosticAnalyzer analyzer)
            {
                // drop any cache related to given analyzer
                s_map.TryRemove(analyzer, out var analyzerMap);
            }

            // make sure key is either documentId or projectId
            private static void AssertKey((object key, string stateKey) key)
            {
                Contract.ThrowIfFalse(key.key is DocumentId || key.key is ProjectId);
            }
        }

        // in memory cache entry
        private readonly struct CacheEntry
        {
            public readonly VersionStamp Version;
            public readonly ImmutableArray<DiagnosticData> Diagnostics;

            public CacheEntry(VersionStamp version, ImmutableArray<DiagnosticData> diagnostics)
            {
                Version = version;
                Diagnostics = diagnostics;
            }
        }
    }
}
