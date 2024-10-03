// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteDependentTypeFinderService : BrokeredServiceBase, IRemoteDependentTypeFinderService
{
    internal sealed class Factory : FactoryBase<IRemoteDependentTypeFinderService>
    {
        protected override IRemoteDependentTypeFinderService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteDependentTypeFinderService(arguments);
    }

    public RemoteDependentTypeFinderService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindTypesAsync(
        Checksum solutionChecksum,
        SerializableSymbolAndProjectId typeAndProjectId,
        ImmutableArray<ProjectId> projectIdsOpt,
        bool transitive,
        DependentTypesKind kind,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var symbol = await typeAndProjectId.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);

            if (symbol is not INamedTypeSymbol namedType)
                return ImmutableArray<SerializableSymbolAndProjectId>.Empty;

            var projects = projectIdsOpt.IsDefault ? null : projectIdsOpt.Select(id => solution.GetRequiredProject(id)).ToImmutableHashSet();

            var types = await DependentTypeFinder.FindTypesInCurrentProcessAsync(namedType, solution, projects, transitive, kind, cancellationToken).ConfigureAwait(false);

            return types.SelectAsArray(
                t => SerializableSymbolAndProjectId.Dehydrate(solution, t, cancellationToken));
        }, cancellationToken);
    }
}
