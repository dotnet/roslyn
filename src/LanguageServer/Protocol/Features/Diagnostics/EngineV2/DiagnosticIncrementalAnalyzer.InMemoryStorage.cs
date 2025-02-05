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
            private static readonly ConcurrentDictionary<DiagnosticAnalyzer, ConcurrentDictionary<(ProjectOrDocumentId key, string stateKey), CacheEntry>> s_map = [];

            public static bool TryGetValue(DiagnosticAnalyzer analyzer, (ProjectOrDocumentId key, string stateKey) key, out CacheEntry entry)
            {
                entry = default;
                return s_map.TryGetValue(analyzer, out var analyzerMap) &&
                    analyzerMap.TryGetValue(key, out entry);
            }

            public static void Cache(DiagnosticAnalyzer analyzer, (ProjectOrDocumentId key, string stateKey) key, CacheEntry entry)
                => s_map.GetOrAdd(analyzer, static_ => [])[key] = entry;
        }

        private readonly struct CacheEntry(Checksum checksum, ImmutableArray<DiagnosticData> diagnostics)
        {
            public readonly Checksum Checksum = checksum;
            public readonly ImmutableArray<DiagnosticData> Diagnostics = diagnostics;
        }
    }
}
