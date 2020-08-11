// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    /// <summary>
    /// Caches suggested action sets between calls to <see cref="CodeActionsHandler"/> and
    /// <see cref="CodeActionResolveHandler"/>.
    /// </summary>
    [Export(typeof(CodeActionsCache)), Shared]
    internal class CodeActionsCache
    {
        /// <summary>
        /// Ensures we aren't making concurrent modifications to the list of cached items.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Maximum number of cached items.
        /// </summary>
        private readonly int _maxCacheSize = 3;

        /// <summary>
        /// Current list of cached items.
        /// </summary>
        private readonly List<CodeActionsCacheItem> _cachedItems = new List<CodeActionsCacheItem>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsCache()
        {
        }

        public async Task UpdateCacheAsync(
            Document document,
            LSP.Range range,
            ImmutableArray<UnifiedSuggestedActionSet> cachedSuggestedActionSets,
            CancellationToken cancellationToken)
        {
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // If there's a value in the cache with the same document and range we're searching for,
                // remove and replace it with our updated value if the updated value is different.
                var previousCachedItem = _cachedItems.Where(
                    c => c.Document == document && c.Range.Start == range.Start && c.Range.End == range.End);
                if (previousCachedItem.Any())
                {
                    var item = previousCachedItem.First();
                    if (item.CachedSuggestedActionSets.SequenceEqual(cachedSuggestedActionSets))
                    {
                        return;
                    }

                    _cachedItems.Remove(item);
                }
                // If the cache is full, remove the oldest cached value.
                else if (_cachedItems.Count >= _maxCacheSize)
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
        public async Task<ImmutableArray<UnifiedSuggestedActionSet>?> GetCacheAsync(
            Document document,
            LSP.Range range,
            CancellationToken cancellationToken)
        {
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var cachedItem in _cachedItems)
                {
                    if (document == cachedItem.Document && document.Project.Solution == cachedItem.Document.Project.Solution &&
                        range.Start == cachedItem.Range?.Start && range.End == cachedItem.Range?.End)
                    {
                        return cachedItem.CachedSuggestedActionSets;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the current number of cached items.
        /// </summary>
        public int GetNumCacheItems() => _cachedItems.Count;

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
    }
}
