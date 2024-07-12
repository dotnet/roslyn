// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion;

internal sealed class SharedSyntaxContextsWithSpeculativeModel
{
    private readonly Document _document;
    private readonly int _position;

    private readonly ConcurrentDictionary<Document, AsyncLazy<SyntaxContext>> _cache;
    private readonly Lazy<ImmutableArray<DocumentId>> _lazyRelatedDocumentIds;

    public SharedSyntaxContextsWithSpeculativeModel(Document document, int position)
    {
        _document = document;
        _position = position;
        _cache = [];
        _lazyRelatedDocumentIds = new(_document.GetLinkedDocumentIds, isThreadSafe: true);
    }

    public Task<SyntaxContext> GetSyntaxContextAsync(Document document, CancellationToken cancellationToken)
    {
        if (!_cache.TryGetValue(document, out var lazyContext))
        {
            if (_document.Id != document.Id && !_lazyRelatedDocumentIds.Value.Contains(document.Id))
                throw new ArgumentException("Don't support getting SyntaxContext for document unrelated to the original document");

            lazyContext = GetLazySyntaxContextWithSpeculativeModel(document, this);
        }

        return lazyContext.GetValueAsync(cancellationToken);

        // Extract a local function to avoid creating a closure for code path of cache hit.
        static AsyncLazy<SyntaxContext> GetLazySyntaxContextWithSpeculativeModel(Document document, SharedSyntaxContextsWithSpeculativeModel self)
        {
            return self._cache.GetOrAdd(document, d => AsyncLazy.Create(asynchronousComputeFunction: static (arg, cancellationToken)
                => Utilities.CreateSyntaxContextWithExistingSpeculativeModelAsync(arg.d, arg._position, cancellationToken), arg: (d, self._position)));
        }
    }
}
