// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion;

internal sealed class SharedSyntaxContextsWithSpeculativeModel
{
    private readonly Document _document;
    private readonly int _position;

    private readonly ConcurrentDictionary<Document, Task<SyntaxContext>> _cache;
    private readonly Lazy<ImmutableArray<DocumentId>> _lazyRelatedDocumentIds;

    public SharedSyntaxContextsWithSpeculativeModel(Document document, int position)
    {
        _document = document;
        _position = position;
        _cache = new();
        _lazyRelatedDocumentIds = new(_document.GetLinkedDocumentIds, isThreadSafe: true);
    }

    public Task<SyntaxContext> GetSyntaxContextAsync(Document document, CancellationToken cancellationToken)
    {
        if (!_cache.TryGetValue(document, out var contextTask))
        {
            Debug.Assert(_document.Id == document.Id || _lazyRelatedDocumentIds.Value.Contains(document.Id),
                message: "Don't use for document unrelated to the original document");

            contextTask = GetLazySyntaxContextWithSpeculativeModelAsync(document, this, cancellationToken);
        }

        return contextTask;

        // Extract a local function to avoid creating a closure for code path of cache hit.
        static Task<SyntaxContext> GetLazySyntaxContextWithSpeculativeModelAsync(Document document, SharedSyntaxContextsWithSpeculativeModel self, CancellationToken cancellationToken)
            => self._cache.GetOrAdd(document, d => Utilities.CreateSyntaxContextWithExistingSpeculativeModelAsync(d, self._position, cancellationToken));
    }
}
