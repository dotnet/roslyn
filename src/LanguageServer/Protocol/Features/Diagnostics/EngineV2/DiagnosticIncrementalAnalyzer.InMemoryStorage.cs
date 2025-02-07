// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        private sealed class InMemoryStorage
        {
            // the reason using nested map rather than having tuple as key is so that I dont have a gigantic map
            private readonly ConcurrentDictionary<DiagnosticAnalyzer, ConcurrentDictionary<(ProjectOrDocumentId key, string stateKey), CacheEntry>> _map =
                new(concurrencyLevel: 2, capacity: 10);

            public bool TryGetValue(DiagnosticAnalyzer analyzer, (ProjectOrDocumentId key, string stateKey) key, out CacheEntry entry)
            {
                entry = default;
                return _map.TryGetValue(analyzer, out var analyzerMap) &&
                    analyzerMap.TryGetValue(key, out entry);
            }

            public void Cache(DiagnosticAnalyzer analyzer, (ProjectOrDocumentId key, string stateKey) key, CacheEntry entry)
            {
                // add new cache entry
                var analyzerMap = _map.GetOrAdd(analyzer, _ => new ConcurrentDictionary<(ProjectOrDocumentId key, string stateKey), CacheEntry>(concurrencyLevel: 2, capacity: 10));
                analyzerMap[key] = entry;
            }

            public void Remove(DiagnosticAnalyzer analyzer, (ProjectOrDocumentId key, string stateKey) key)
            {
                // remove the entry
                if (!_map.TryGetValue(analyzer, out var analyzerMap))
                {
                    return;
                }

                analyzerMap.TryRemove(key, out _);

                if (analyzerMap.IsEmpty)
                {
                    _map.TryRemove(analyzer, out _);
                }
            }

            public void DropCache(DiagnosticAnalyzer analyzer)
            {
                // drop any cache related to given analyzer
                _map.TryRemove(analyzer, out _);
            }
        }

        // in memory cache entry
        private readonly record struct CacheEntry(VersionStamp Version, ImmutableArray<DiagnosticData> Diagnostics);
    }
}
