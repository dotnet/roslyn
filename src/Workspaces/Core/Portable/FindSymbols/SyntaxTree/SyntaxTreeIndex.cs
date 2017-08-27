﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex
    {
        private readonly LiteralInfo _literalInfo;
        private readonly IdentifierInfo _identifierInfo;
        private readonly ContextInfo _contextInfo;
        private readonly DeclarationInfo _declarationInfo;

        private SyntaxTreeIndex(
            Checksum checksum,
            LiteralInfo literalInfo,
            IdentifierInfo identifierInfo,
            ContextInfo contextInfo,
            DeclarationInfo declarationInfo)
        {
            this.Checksum = checksum;
            _literalInfo = literalInfo;
            _identifierInfo = identifierInfo;
            _contextInfo = contextInfo;
            _declarationInfo = declarationInfo;
        }

        private static readonly ConditionalWeakTable<Document, SyntaxTreeIndex> s_documentToIndex = new ConditionalWeakTable<Document, SyntaxTreeIndex>();
        private static readonly ConditionalWeakTable<DocumentId, SyntaxTreeIndex> s_documentIdToIndex = new ConditionalWeakTable<DocumentId, SyntaxTreeIndex>();

        public static async Task PrecalculateAsync(Document document, CancellationToken cancellationToken)
        {
            Contract.Requires(document.IsFromPrimaryBranch());

            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            // Check if we've already created and persisted the index for this document.
            if (await PrecalculatedAsync(document, checksum, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            // If not, create and save the index.
            var data = await CreateIndexAsync(document, checksum, cancellationToken).ConfigureAwait(false);
            await data.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<SyntaxTreeIndex> GetIndexAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            // See if we already cached an index with this direct document index.  If so we can just
            // return it with no additional work.
            if (!s_documentToIndex.TryGetValue(document, out var index))
            {
                index = await GetIndexWorkerAsync(document, cancellationToken).ConfigureAwait(false);

                // Populate our caches with this data.
                s_documentToIndex.GetValue(document, _ => index);
                s_documentIdToIndex.GetValue(document.Id, _ => index);
            }

            return index;
        }

        private static async Task<SyntaxTreeIndex> GetIndexWorkerAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            // Check if we have an index for a previous version of this document.  If our
            // checksums match, we can just use that.
            if (s_documentIdToIndex.TryGetValue(document.Id, out var index) &&
                index.Checksum == checksum)
            {
                // The previous index we stored with this documentId is still valid.  Just
                // return that.
                return index;
            }

            // What we have in memory isn't valid.  Try to load from the persistence service.
            index = await LoadAsync(document, checksum, cancellationToken).ConfigureAwait(false);
            if (index != null)
            {
                return index;
            }

            // alright, we don't have cached information, re-calculate them here.
            index = await CreateIndexAsync(document, checksum, cancellationToken).ConfigureAwait(false);

            // okay, persist this info
            await index.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            return index;
        }
    }
}
