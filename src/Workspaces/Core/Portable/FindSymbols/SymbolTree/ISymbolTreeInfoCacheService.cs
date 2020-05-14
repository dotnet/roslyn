// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
