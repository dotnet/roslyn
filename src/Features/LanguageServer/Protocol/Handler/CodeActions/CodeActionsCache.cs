// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    /// <summary>
    /// Caches suggested action sets between calls to <see cref="CodeActionsHandler"/> and
    /// <see cref="CodeActionResolveHandler"/>.
    /// </summary>
    internal class CodeActionsCache
    {
        /// <summary>
        /// Ensures we aren't making concurrent modifications to the list of cached items.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(1);

        /// <summary>
        /// Maximum number of cached items.
        /// </summary>
        private const int MaxCacheSize = 3;

        /// <summary>
        /// Current list of cached items.
        /// </summary>
        private readonly List<CodeActionsCacheItem> _cachedItems = new();

        public async Task UpdateActionSetsAsync(
            Document document,
            LSP.Range range,
            ImmutableArray<UnifiedSuggestedActionSet> cachedSuggestedActionSets,
            CancellationToken cancellationToken)
        {
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // If there's a value in the cache with the same document and range we're searching for,
                // remove and replace it with our updated value.
                var previousCachedItem = _cachedItems.FirstOrNull(c => IsMatch(document, range, c));
                if (previousCachedItem.HasValue)
                {
                    _cachedItems.Remove(previousCachedItem.Value);
                }
                // If the cache is full, remove the oldest cached value.
                else if (_cachedItems.Count >= MaxCacheSize)
                {
                    _cachedItems.RemoveAt(0);
                }

                _cachedItems.Add(new CodeActionsCacheItem(document, range, cachedSuggestedActionSets));
            }
        }

        /// <summary>
        /// Attempts to retrieve the cached action sets that match the given document and range.
        /// Returns null if no match is found.
        /// </summary>
        public async Task<ImmutableArray<UnifiedSuggestedActionSet>?> GetActionSetsAsync(
            Document document,
            LSP.Range range,
            CancellationToken cancellationToken)
        {
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var cachedItem in _cachedItems)
                {
                    if (IsMatch(document, range, cachedItem))
                    {
                        return cachedItem.CachedSuggestedActionSets;
                    }
                }

                return null;
            }
        }

        private static bool IsMatch(Document document, LSP.Range range, CodeActionsCacheItem cachedItem)
            => document == cachedItem.Document &&
            document.Project.Solution == cachedItem.Document.Project.Solution &&
            range.Start == cachedItem.Range.Start &&
            range.End == cachedItem.Range.End;

        /// <summary>
        /// Contains the necessary information for each cached item.
        /// </summary>
        private readonly struct CodeActionsCacheItem
        {
            public readonly Document Document;
            public readonly LSP.Range Range;
            public readonly ImmutableArray<UnifiedSuggestedActionSet> CachedSuggestedActionSets;

            public CodeActionsCacheItem(
                Document document,
                LSP.Range range,
                ImmutableArray<UnifiedSuggestedActionSet> cachedSuggestedActionSets)
            {
                Document = document;
                Range = range;
                CachedSuggestedActionSets = cachedSuggestedActionSets;
            }
        }

        internal TestAccessor GetTestAccessor() => new(this);

        internal readonly struct TestAccessor
        {
            private readonly CodeActionsCache _codeActionsCache;

            public static int MaximumCacheSize => MaxCacheSize;

            public TestAccessor(CodeActionsCache codeActionsCache)
                => _codeActionsCache = codeActionsCache;

            public List<(Document Document, LSP.Range Range)> GetDocumentsAndRangesInCache()
                => _codeActionsCache._cachedItems.Select(c => (c.Document, c.Range)).ToList();
        }
    }
}
