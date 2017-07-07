// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IRemoteSymbolFinder
    {
        Task FindReferencesAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg, DocumentId[] documentArgs, CancellationToken cancellationToken);
        Task FindLiteralReferencesAsync(object value, TypeCode typeCode, CancellationToken cancellationToken);

        Task<ImmutableArray<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria, CancellationToken cancellationToken);

        Task<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken);

        Task<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken);

        Task<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken);
    }
}
