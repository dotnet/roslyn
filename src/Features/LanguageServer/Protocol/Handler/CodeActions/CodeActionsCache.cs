// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    [Export(typeof(CodeActionsCache)), Shared]
    internal class CodeActionsCache
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private readonly int _maxCacheSize = 3;

        private readonly List<CodeActionsCacheItem> _cachedItems;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsCache()
        {
            _cachedItems = new List<CodeActionsCacheItem>();
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
                // remove it and replace with our updated values.
                var previousCacheItem = _cachedItems.Where(c => c.Document == document && c.Range == range);
                if (previousCacheItem.Any())
                {
                    _cachedItems.Remove(previousCacheItem.First());
                }
                // If the cache is full, remove the oldest cached value.
                else if (_cachedItems.Count >= _maxCacheSize)
                {
                    _cachedItems.RemoveAt(0);
                }

                _cachedItems.Add(new CodeActionsCacheItem(document, range, cachedSuggestedActionSets));
            }
        }

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
                        range.Start != cachedItem.Range?.Start && range.End != cachedItem.Range?.End)
                    {
                        return cachedItem.CachedSuggestedActionSets;
                    }
                }

                return null;
            }
        }

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
