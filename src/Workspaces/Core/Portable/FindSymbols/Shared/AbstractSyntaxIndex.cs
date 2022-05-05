// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal abstract partial class AbstractSyntaxIndex<TIndex>
        where TIndex : AbstractSyntaxIndex<TIndex>
    {
        protected delegate TIndex? IndexReader(StringTable stringTable, ObjectReader reader, Checksum? checksum);
        protected delegate TIndex IndexCreator(Document document, SyntaxNode root, Checksum checksum, CancellationToken cancellationToken);

        private static readonly ConditionalWeakTable<Document, TIndex?> s_documentToIndex = new();
        private static readonly ConditionalWeakTable<DocumentId, TIndex?> s_documentIdToIndex = new();

        protected AbstractSyntaxIndex(Checksum? checksum)
        {
            this.Checksum = checksum;
        }

        protected static async ValueTask<TIndex> GetRequiredIndexAsync(Document document, IndexReader read, IndexCreator create, CancellationToken cancellationToken)
        {
            var index = await GetIndexAsync(document, read, create, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(index);
            return index;
        }

        protected static ValueTask<TIndex?> GetIndexAsync(Document document, IndexReader read, IndexCreator create, CancellationToken cancellationToken)
            => GetIndexAsync(document, loadOnly: false, read, create, cancellationToken);

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        protected static async ValueTask<TIndex?> GetIndexAsync(
            Document document,
            bool loadOnly,
            IndexReader read,
            IndexCreator create,
            CancellationToken cancellationToken)
        {
            if (!document.SupportsSyntaxTree)
                return null;

            // See if we already cached an index with this direct document index.  If so we can just
            // return it with no additional work.
            if (!s_documentToIndex.TryGetValue(document, out var index))
            {
                index = await GetIndexWorkerAsync(document, loadOnly, read, create, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(index != null || loadOnly == true, "Result can only be null if 'loadOnly: true' was passed.");

                if (index == null && loadOnly)
                {
                    return null;
                }

                // Populate our caches with this data.
                s_documentToIndex.GetValue(document, _ => index);
                s_documentIdToIndex.Remove(document.Id);
                s_documentIdToIndex.GetValue(document.Id, _ => index);
            }

            return index;
        }

        private static async Task<TIndex?> GetIndexWorkerAsync(
            Document document,
            bool loadOnly,
            IndexReader read,
            IndexCreator create,
            CancellationToken cancellationToken)
        {
            if (!document.SupportsSyntaxTree)
                return null;

            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            // Check if we have an index for a previous version of this document.  If our
            // checksums match, we can just use that.
            if (s_documentIdToIndex.TryGetValue(document.Id, out var index) &&
                index?.Checksum == checksum)
            {
                // The previous index we stored with this documentId is still valid.  Just
                // return that.
                return index;
            }

            // What we have in memory isn't valid.  Try to load from the persistence service.
            index = await LoadAsync(document, checksum, read, cancellationToken).ConfigureAwait(false);
            if (index != null || loadOnly)
                return index;

            // alright, we don't have cached information, re-calculate them here.
            index = await CreateIndexAsync(document, checksum, create, cancellationToken).ConfigureAwait(false);

            // okay, persist this info
            await index.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            return index;
        }

        private static async Task<TIndex> CreateIndexAsync(
            Document document, Checksum checksum, IndexCreator create, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.SupportsSyntaxTree);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return create(document, root, checksum, cancellationToken);
        }
    }
}
