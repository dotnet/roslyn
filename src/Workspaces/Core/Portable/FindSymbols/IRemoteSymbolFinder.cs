// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IRemoteSymbolFinder
    {
        Task FindReferencesAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg, DocumentId[] documentArgs);
        Task FindLiteralReferencesAsync(object value);

        Task<SerializableSymbolAndProjectId[]> FindAllDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria);

        Task<SerializableSymbolAndProjectId[]> FindSolutionSourceDeclarationsWithNormalQuery(
            string name, bool ignoreCase, SymbolFilter criteria);

        Task<SerializableSymbolAndProjectId[]> FindProjectSourceDeclarationsWithNormalQuery(
            ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria);
    }
}