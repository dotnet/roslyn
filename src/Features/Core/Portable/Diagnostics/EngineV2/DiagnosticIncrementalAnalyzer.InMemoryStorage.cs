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
            private readonly static ConcurrentDictionary<DiagnosticAnalyzer, ConcurrentDictionary<object, CacheEntry>> s_map =
                new ConcurrentDictionary<DiagnosticAnalyzer, ConcurrentDictionary<object, CacheEntry>>(concurrencyLevel: 2, capacity: 10);

            public static bool TryGetValue(DiagnosticAnalyzer analyzer, object key, out CacheEntry entry)
            {
                AssertKey(key);

                entry = default(CacheEntry);

                ConcurrentDictionary<object, CacheEntry> analyzerMap;
                if (!s_map.TryGetValue(analyzer, out analyzerMap) ||
                    !analyzerMap.TryGetValue(key, out entry))
                {
                    return false;
                }

                return true;
            }

            public static void Cache(DiagnosticAnalyzer analyzer, object key, CacheEntry entry)
            {
                AssertKey(key);

                // add new cache entry
                var analyzerMap = s_map.GetOrAdd(analyzer, _ => new ConcurrentDictionary<object, CacheEntry>(concurrencyLevel: 2, capacity: 10));
                analyzerMap[key] = entry;
            }

            public static void Remove(DiagnosticAnalyzer analyzer, object key)
            {
                AssertKey(key);

                // remove the entry
                ConcurrentDictionary<object, CacheEntry> analyzerMap;
                if (!s_map.TryGetValue(analyzer, out analyzerMap))
                {
                    return;
                }

                CacheEntry entry;
                analyzerMap.TryRemove(key, out entry);

                if (analyzerMap.IsEmpty)
                {
                    s_map.TryRemove(analyzer, out analyzerMap);
                }
            }

            public static void DropCache(DiagnosticAnalyzer analyzer)
            {
                // drop any cache related to given analyzer
                ConcurrentDictionary<object, CacheEntry> analyzerMap;
                s_map.TryRemove(analyzer, out analyzerMap);
            }

            // make sure key is either documentId or projectId
            private static void AssertKey(object key)
            {
                Contract.ThrowIfFalse(key is DocumentId || key is ProjectId);
            }
        }

        // in memory cache entry
        private struct CacheEntry
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
