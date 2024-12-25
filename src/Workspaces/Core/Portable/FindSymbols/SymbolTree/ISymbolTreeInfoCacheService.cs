// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree;

/// <summary>
/// Computes and caches <see cref="SymbolTreeInfo"/> indices for the source symbols in <see cref="Project"/>s and
/// for metadata symbols in <see cref="PortableExecutableReference"/>s.
/// </summary>
internal interface ISymbolTreeInfoCacheService : IWorkspaceService
{
    ValueTask<SymbolTreeInfo?> TryGetPotentiallyStaleSourceSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken);
    ValueTask<SymbolTreeInfo?> TryGetPotentiallyStaleMetadataSymbolTreeInfoAsync(Project project, PortableExecutableReference reference, CancellationToken cancellationToken);
}
