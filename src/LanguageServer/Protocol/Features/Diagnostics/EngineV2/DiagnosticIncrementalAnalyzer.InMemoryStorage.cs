// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private static class InMemoryStorage
        {
            // the reason using nested map rather than having tuple as key is so that I dont have a gigantic map
            private static readonly ConcurrentDictionary<DiagnosticAnalyzer, ConcurrentDictionary<(ProjectOrDocumentId key, string stateKey), CacheEntry>> s_map =
                new(concurrencyLevel: 2, capacity: 10);

            public static bool TryGetValue(DiagnosticAnalyzer analyzer, (ProjectOrDocumentId key, string stateKey) key, out CacheEntry entry)
            {
                entry = default;
                return s_map.TryGetValue(analyzer, out var analyzerMap) &&
                    analyzerMap.TryGetValue(key, out entry);
            }

            public static void Cache(DiagnosticAnalyzer analyzer, (ProjectOrDocumentId key, string stateKey) key, CacheEntry entry)
            {
                // add new cache entry
                var analyzerMap = s_map.GetOrAdd(analyzer, _ => new ConcurrentDictionary<(ProjectOrDocumentId key, string stateKey), CacheEntry>(concurrencyLevel: 2, capacity: 10));
                analyzerMap[key] = entry;
            }

            public static void Remove(DiagnosticAnalyzer analyzer, (ProjectOrDocumentId key, string stateKey) key)
            {
                // remove the entry
                if (!s_map.TryGetValue(analyzer, out var analyzerMap))
                {
                    return;
                }

                analyzerMap.TryRemove(key, out _);

                if (analyzerMap.IsEmpty)
                {
                    s_map.TryRemove(analyzer, out _);
                }
            }

            public static void DropCache(DiagnosticAnalyzer analyzer)
            {
                // drop any cache related to given analyzer
                s_map.TryRemove(analyzer, out _);
            }
        }

        // in memory cache entry
        private readonly record struct CacheEntry(VersionStamp Version, ImmutableArray<DiagnosticData> Diagnostics);
    }
}
