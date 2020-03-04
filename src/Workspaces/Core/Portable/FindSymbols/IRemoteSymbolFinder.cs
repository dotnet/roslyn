// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IRemoteSymbolFinder
    {
        Task FindReferencesAsync(PinnedSolutionInfo solutionInfo, SerializableSymbolAndProjectId symbolAndProjectIdArg, DocumentId[] documentArgs,
            SerializableFindReferencesSearchOptions options, CancellationToken cancellationToken);

        Task FindLiteralReferencesAsync(PinnedSolutionInfo solutionInfo, object value, TypeCode typeCode, CancellationToken cancellationToken);

        Task<IList<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria, CancellationToken cancellationToken);

        Task<IList<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken);

        Task<IList<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken);

        Task<IList<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithPatternAsync(
            PinnedSolutionInfo solutionInfo, string pattern, SymbolFilter criteria, CancellationToken cancellationToken);

        Task<IList<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken);
    }
}
