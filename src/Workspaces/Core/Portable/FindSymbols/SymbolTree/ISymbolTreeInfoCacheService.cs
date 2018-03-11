// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree
{
    internal interface ISymbolTreeInfoCacheService : IWorkspaceService
    {
        /// <summary>
        /// Returns null if the info cannot be retrieved from the cache.
        /// </summary>
        Task<SymbolTreeInfo> TryGetSourceSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken);

        /// <summary>
        /// Returns null if the info cannot be retrieved from the cache.
        /// </summary>
        Task<SymbolTreeInfo> TryGetMetadataSymbolTreeInfoAsync(Solution solution, PortableExecutableReference reference, CancellationToken cancellationToken);
    }
}
