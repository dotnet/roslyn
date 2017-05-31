// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree
{
    internal interface IRemoteSymbolTreeInfoCacheService
    {
        Task<SerializableSymbolAndProjectId[]> TryFindSourceSymbolsAsync(
            ProjectId projectId, SymbolFilter filter, string queryName, SearchKind queryKind);

        Task<SerializableSymbolAndProjectId[]> TryFindMetadataSymbolsAsync(
            Checksum metadataChecksum, ProjectId assemblyProjectId, SymbolFilter filter, string queryName, SearchKind queryKind);
    }
}