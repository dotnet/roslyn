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

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class SharedSyntaxContextsWithSpeculativeModel
    {
        public Document Document { get; }
        public int Position { get; }

        private readonly ConcurrentDictionary<Document, AsyncLazy<SyntaxContext?>> _cache;
        private readonly Lazy<ImmutableArray<DocumentId>> _lazyRelatedDocumentIds;

        public SharedSyntaxContextsWithSpeculativeModel(Document document, int position)
        {
            Document = document;
            Position = position;
            _cache = new();
            _lazyRelatedDocumentIds = new(Document.GetLinkedDocumentIds, isThreadSafe: true);
        }

        public Task<SyntaxContext?> GetSyntaxContextAsync(Document document, CancellationToken cancellationToken)
        {
            if (!_cache.TryGetValue(document, out var lazyContext))
            {
                if (Document.Id != document.Id || !_lazyRelatedDocumentIds.Value.Contains(document.Id))
                    throw new ArgumentException("Don't support getting SyntaxContext for document unrelated to the original document");

                lazyContext = GetLazySyntaxContextWithSpeculativeModel(document, Position);
            }

            return lazyContext.GetValueAsync(cancellationToken);
        }

        private AsyncLazy<SyntaxContext?> GetLazySyntaxContextWithSpeculativeModel(Document document, int position)
        {
            return _cache.GetOrAdd(document, d => AsyncLazy.Create(cancellationToken
                => CompletionHelper.CreateSyntaxContextWithExistingSpeculativeModelAsync(document, position, cancellationToken), cacheResult: true));
        }
    }
}
