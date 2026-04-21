// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed partial class IncrementalMemberEditAnalyzer
    {
        /// <summary>
        /// Per-document cache of diagnostic analysis results and member spans. Each document
        /// (which may represent the same file in different projects) has its own independent
        /// cache entry. Entries are automatically evicted when not accessed for <see cref="s_evictionTimeout"/>.
        /// Read access via <see cref="TryGetValue"/> automatically refreshes the entry's last-access timestamp.
        /// </summary>
        private sealed class DiagnosticCache
        {
            private static readonly TimeSpan s_evictionTimeout = TimeSpan.FromMinutes(1);

            private readonly ConcurrentDictionary<DocumentId, Entry> _entries = [];

            public DiagnosticCache(CancellationToken cancellationToken)
            {
                _ = EvictStaleEntriesLoopAsync(cancellationToken).ReportNonFatalErrorAsync();
            }

            /// <summary>
            /// Stores or replaces the cached snapshot for the given document.
            /// </summary>
            public void Update(Document document, VersionStamp version, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> diagnostics, ImmutableArray<TextSpan> memberSpans)
                => _entries[document.Id] = new Entry(version, document, diagnostics, memberSpans);

            /// <summary>
            /// Attempts to retrieve the cached snapshot for <paramref name="documentId"/>.
            /// On a successful lookup, the entry's last-access timestamp is automatically refreshed.
            /// </summary>
            public bool TryGetValue(DocumentId documentId, [NotNullWhen(true)] out Entry? entry)
            {
                if (_entries.TryGetValue(documentId, out entry))
                {
                    entry.ResetLastAccess();
                    return true;
                }

                entry = null;
                return false;
            }

            private async Task EvictStaleEntriesLoopAsync(CancellationToken cancellationToken)
            {
                using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var keysToRemove);

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(s_evictionTimeout, cancellationToken).ConfigureAwait(false);

                        foreach (var kvp in _entries)
                        {
                            if (kvp.Value.LastAccess.Elapsed > s_evictionTimeout)
                                keysToRemove.Add(kvp.Key);
                        }

                        foreach (var key in keysToRemove)
                            _entries.TryRemove(key, out _);

                        keysToRemove.Clear();
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }

            /// <summary>
            /// Cached state for a single document: the document snapshot from the last analysis,
            /// the per-analyzer diagnostic results, and the member spans used for incremental splicing.
            /// </summary>
            internal sealed record Entry(
                VersionStamp Version,
                Document Document,
                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> Diagnostics,
                ImmutableArray<TextSpan> MemberSpans)
            {
                /// <summary>
                /// Tracks when this entry was last read or written, for time-based eviction.
                /// </summary>
                public SharedStopwatch LastAccess { get; private set; } = SharedStopwatch.StartNew();

                public void ResetLastAccess()
                    => LastAccess = SharedStopwatch.StartNew();
            }
        }
    }
}
