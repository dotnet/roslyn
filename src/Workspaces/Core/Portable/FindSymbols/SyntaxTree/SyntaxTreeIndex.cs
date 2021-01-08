﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
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
        private readonly ExtensionMethodInfo _extensionMethodInfo;
        private readonly Lazy<HashSet<DeclaredSymbolInfo>> _declaredSymbolInfoSet;

        private SyntaxTreeIndex(
            Checksum checksum,
            LiteralInfo literalInfo,
            IdentifierInfo identifierInfo,
            ContextInfo contextInfo,
            DeclarationInfo declarationInfo,
            ExtensionMethodInfo extensionMethodInfo)
        {
            this.Checksum = checksum;
            _literalInfo = literalInfo;
            _identifierInfo = identifierInfo;
            _contextInfo = contextInfo;
            _declarationInfo = declarationInfo;
            _extensionMethodInfo = extensionMethodInfo;
            _declaredSymbolInfoSet = new(() => new(this.DeclaredSymbolInfos));
        }

        private static readonly ConditionalWeakTable<Document, SyntaxTreeIndex> s_documentToIndex = new();
        private static readonly ConditionalWeakTable<DocumentId, SyntaxTreeIndex> s_documentIdToIndex = new();

        public static async Task PrecalculateAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SyntaxTreeIndex_Precalculate, cancellationToken))
            {
                Debug.Assert(document.IsFromPrimaryBranch());

                var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

                // Check if we've already created and persisted the index for this document.
                if (await PrecalculatedAsync(document, checksum, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                using (Logger.LogBlock(FunctionId.SyntaxTreeIndex_Precalculate_Create, cancellationToken))
                {
                    // If not, create and save the index.
                    var data = await CreateIndexAsync(document, checksum, cancellationToken).ConfigureAwait(false);
                    await data.SaveAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public static ValueTask<SyntaxTreeIndex> GetIndexAsync(Document document, CancellationToken cancellationToken)
            => GetIndexAsync(document, loadOnly: false, cancellationToken);

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        public static async ValueTask<SyntaxTreeIndex> GetIndexAsync(
            Document document,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            // See if we already cached an index with this direct document index.  If so we can just
            // return it with no additional work.
            if (!s_documentToIndex.TryGetValue(document, out var index))
            {
                index = await GetIndexWorkerAsync(document, loadOnly, cancellationToken).ConfigureAwait(false);
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

        private static async Task<SyntaxTreeIndex> GetIndexWorkerAsync(
            Document document,
            bool loadOnly,
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
            if (index != null || loadOnly)
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
