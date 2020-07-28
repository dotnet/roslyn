// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteDependentTypeFinder
    {
        private Task<ImmutableArray<SerializableSymbolAndProjectId>> FindTypesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId typeAndProjectId,
            ProjectId[] projectIds,
            Func<INamedTypeSymbol, Solution, ImmutableHashSet<Project>, Task<ImmutableArray<INamedTypeSymbol>>> func,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var symbol = await typeAndProjectId.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);

                    if (!(symbol is INamedTypeSymbol namedType))
                        return ImmutableArray<SerializableSymbolAndProjectId>.Empty;

                    var projects = projectIds?.Select(id => solution.GetProject(id)).ToImmutableHashSet();
                    var types = await func(namedType, solution, projects).ConfigureAwait(false);

                    return types.SelectAsArray(
                        t => SerializableSymbolAndProjectId.Dehydrate(solution, t, cancellationToken));
                }
            }, cancellationToken);
        }

        public Task<ImmutableArray<SerializableSymbolAndProjectId>> FindDerivedClassesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId typeAndProjectId,
            ProjectId[] projectIds,
            bool transitive,
            CancellationToken cancellationToken)
        {
            return FindTypesAsync(
                solutionInfo, typeAndProjectId, projectIds,
                (nt, s, ps) => DependentTypeFinder.FindDerivedClassesAsync(nt, s, ps, transitive, cancellationToken),
                cancellationToken);
        }

        public Task<ImmutableArray<SerializableSymbolAndProjectId>> FindDerivedInterfacesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId typeAndProjectId,
            ProjectId[] projectIds,
            bool transitive,
            CancellationToken cancellationToken)
        {
            return FindTypesAsync(
                solutionInfo, typeAndProjectId, projectIds,
                (nt, s, ps) => DependentTypeFinder.FindDerivedInterfacesAsync(nt, s, ps, transitive, cancellationToken),
                cancellationToken);
        }

        public Task<ImmutableArray<SerializableSymbolAndProjectId>> FindImplementingTypesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId typeAndProjectId,
            ProjectId[] projectIds,
            bool transitive,
            CancellationToken cancellationToken)
        {
            return FindTypesAsync(
                solutionInfo, typeAndProjectId, projectIds,
                (nt, s, ps) => DependentTypeFinder.FindImplementingTypesAsync(nt, s, ps, transitive, cancellationToken),
                cancellationToken);
        }
    }
}
