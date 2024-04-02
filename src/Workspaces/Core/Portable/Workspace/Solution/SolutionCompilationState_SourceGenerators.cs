// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    private sealed record SourceGeneratorMap(
        ImmutableArray<ISourceGenerator> SourceGenerators,
        FrozenDictionary<ISourceGenerator, AnalyzerReference> SourceGeneratorToAnalyzerReference);

    /// <summary>
    /// Cached mapping from language (only C#/VB since those are the only languages that support analyzers) to the lists
    /// of analyzer references (see <see cref="ProjectState.AnalyzerReferences"/>) to all the <see
    /// cref="ISourceGenerator"/>s produced by those references.  This should only be created and cached on the OOP side
    /// of things so that we don't cause source generators to be loaded (and fixed) within VS (which is .net framework
    /// only).
    /// </summary>
    private static readonly ConditionalWeakTable<ProjectState, SourceGeneratorMap> s_projectStateToSourceGeneratorsMap = new();

    /// <summary>
    /// Cached information about if a project has source generators or not.  Note: this is distinct from <see
    /// cref="s_projectStateToSourceGeneratorsMap"/> as we want to be able to compute it by calling over to our OOP
    /// process (if present) and having it make the determination, without the host necessarily loading generators
    /// itself.
    /// </summary>
    private static readonly ConditionalWeakTable<ProjectState, AsyncLazy<bool>> s_hasSourceGeneratorsMap = new();

    /// <summary>
    /// This method should only be called in a .net core host like our out of process server.
    /// </summary>
    private static ImmutableArray<ISourceGenerator> GetSourceGenerators(ProjectState projectState)
    {
        var map = GetSourceGeneratorMap(projectState);
        return map is null ? [] : map.SourceGenerators;
    }

    /// <summary>
    /// This method should only be called in a .net core host like our out of process server.
    /// </summary>
    private static AnalyzerReference GetAnalyzerReference(ProjectState projectState, ISourceGenerator sourceGenerator)
    {
        // We must be talking about a project that supports compilations, since we already got a source generator from it.
        Contract.ThrowIfFalse(projectState.SupportsCompilation);

        var map = GetSourceGeneratorMap(projectState);

        // It should not be possible for this to be null. We have the source generator, as such we must have mapped from
        // the project state to the SG info for it.
        Contract.ThrowIfNull(map);

        return map.SourceGeneratorToAnalyzerReference[sourceGenerator];
    }

    private static SourceGeneratorMap? GetSourceGeneratorMap(ProjectState projectState)
    {
        if (!projectState.SupportsCompilation)
            return null;

        return s_projectStateToSourceGeneratorsMap.GetValue(projectState, ComputeSourceGenerators);

        static SourceGeneratorMap ComputeSourceGenerators(ProjectState projectState)
        {
            using var generators = TemporaryArray<ISourceGenerator>.Empty;
            using var _ = PooledDictionary<ISourceGenerator, AnalyzerReference>.GetInstance(out var generatorToAnalyzerReference);

            foreach (var reference in projectState.AnalyzerReferences)
            {
                foreach (var generator in reference.GetGenerators(projectState.Language).Distinct())
                {
                    generators.Add(generator);
                    generatorToAnalyzerReference.Add(generator, reference);
                }
            }

            return new(generators.ToImmutableAndClear(), generatorToAnalyzerReference.ToFrozenDictionary());
        }
    }

    public async Task<bool> HasSourceGeneratorsAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var projectState = this.SolutionState.GetRequiredProjectState(projectId);

        if (!s_hasSourceGeneratorsMap.TryGetValue(projectState, out var lazy))
        {
            // Extracted into local function to prevent allocations in the case where we find a value in the cache.
            lazy = GetLazy(projectState);
        }

        return await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

        AsyncLazy<bool> GetLazy(ProjectState projectState)
            => s_hasSourceGeneratorsMap.GetValue(
                projectState,
                projectState => AsyncLazy.Create(cancellationToken => ComputeHasSourceGeneratorsAsync(projectState, cancellationToken)));

        async Task<bool> ComputeHasSourceGeneratorsAsync(
            ProjectState projectState, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(this.Services, cancellationToken).ConfigureAwait(false);
            // If in proc, just load the generators and see if we have any.
            if (client is null)
                return GetSourceGenerators(projectState).Any();

            // Out of process, call to the remote to figure this out.
            var result = await client.TryInvokeAsync<IRemoteSourceGenerationService, bool>(
                this,
                projectId,
                (service, solution, cancellationToken) => service.HasGeneratorsAsync(solution, projectId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return result.HasValue && result.Value;
        }
    }
}
