// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly VersionStamp _version;

        private readonly IdentifierInfo _identifierInfo;
        private readonly ContextInfo _contextInfo;
        private readonly DeclarationInfo _declarationInfo;

        private SyntaxTreeIndex(
            VersionStamp version,
            IdentifierInfo identifierInfo,
            ContextInfo contextInfo,
            DeclarationInfo declarationInfo)
        {
            Version = version;
            _identifierInfo = identifierInfo;
            _contextInfo = contextInfo;
            _declarationInfo = declarationInfo;
        }

        /// <summary>
        /// snapshot based cache to guarantee same info is returned without re-calculating for 
        /// same solution snapshot.
        /// 
        /// since document will be re-created per new solution, this should go away as soon as 
        /// there is any change on workspace.
        /// </summary>
        private static readonly ConditionalWeakTable<Document, SyntaxTreeIndex> s_infoCache
            = new ConditionalWeakTable<Document, SyntaxTreeIndex>();

        public static async Task PrecalculateAsync(Document document, CancellationToken cancellationToken)
        {
            Contract.Requires(document.IsFromPrimaryBranch());

            // we already have information. move on
            if (await PrecalculatedAsync(document, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var data = await CreateInfoAsync(document, cancellationToken).ConfigureAwait(false);
            await data.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<SyntaxTreeIndex> GetIndexAsync(
            Document document,
            ConditionalWeakTable<Document, SyntaxTreeIndex> cache,
            Func<Document, CancellationToken, Task<SyntaxTreeIndex>> generator,
            CancellationToken cancellationToken)
        {
            if (cache.TryGetValue(document, out var info))
            {
                return info;
            }

            info = await generator(document, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return cache.GetValue(document, _ => info);
            }

            // alright, we don't have cached information, re-calculate them here.
            var data = await CreateInfoAsync(document, cancellationToken).ConfigureAwait(false);

            // okay, persist this info
            await data.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            return cache.GetValue(document, _ => data);
        }

        public static Task<SyntaxTreeIndex> GetIndexAsync(Document document, CancellationToken cancellationToken)
            => GetIndexAsync(document, s_infoCache, s_loadAsync, cancellationToken);

        private static Func<Document, CancellationToken, Task<SyntaxTreeIndex>> s_loadAsync = LoadAsync;
    }
}