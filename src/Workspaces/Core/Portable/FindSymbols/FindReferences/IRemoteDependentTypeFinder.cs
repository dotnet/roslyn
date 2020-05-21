// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IRemoteDependentTypeFinder
    {
        Task<ImmutableArray<SerializableSymbolAndProjectId>> FindDerivedClassesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId type,
            ProjectId[] projects,
            bool transitive,
            CancellationToken cancellationToken);

        Task<ImmutableArray<SerializableSymbolAndProjectId>> FindDerivedInterfacesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId type,
            ProjectId[] projects,
            bool transitive,
            CancellationToken cancellationToken);

        Task<ImmutableArray<SerializableSymbolAndProjectId>> FindImplementingTypesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId type,
            ProjectId[] projects,
            bool transitive,
            CancellationToken cancellationToken);
    }
}
