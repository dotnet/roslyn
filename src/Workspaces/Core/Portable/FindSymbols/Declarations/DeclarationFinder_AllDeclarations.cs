// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // All the logic for finding all declarations in a given solution/project with some name 
    // is in this file.  

    internal static partial class DeclarationFinder
    {
        public static async Task<ImmutableArray<ISymbol>> FindAllDeclarationsWithNormalQueryAsync(
            Project project, SearchQuery query, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            if (project == null)
                throw new ArgumentNullException(nameof(project));

            Contract.ThrowIfNull(query.Name);
            if (string.IsNullOrWhiteSpace(query.Name))
                return ImmutableArray<ISymbol>.Empty;

            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var solution = project.Solution;

                var result = await client.TryInvokeAsync<IRemoteSymbolFinderService, ImmutableArray<SerializableSymbolAndProjectId>>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.FindAllDeclarationsWithNormalQueryAsync(solutionInfo, project.Id, query.Name, query.Kind, criteria, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                if (!result.HasValue)
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                return await RehydrateAsync(solution, result.Value, cancellationToken).ConfigureAwait(false);
            }

            return await FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
                project, query, criteria, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<ImmutableArray<ISymbol>> FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
            Project project, SearchQuery query, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);

            // Lazily produce the compilation.  We don't want to incur any costs (especially source generators) if there
            // are no results for this query in this project.
            var lazyCompilation = AsyncLazy.Create(project.GetRequiredCompilationAsync);

            if (project.SupportsCompilation)
            {
                await SearchCurrentProjectAsync().ConfigureAwait(false);
                await SearchProjectReferencesAsync().ConfigureAwait(false);
                await SearchMetadataReferencesAsync().ConfigureAwait(false);
            }

            return result.ToImmutable();

            async Task SearchCurrentProjectAsync()
            {
                // Search in the source symbols for this project first.
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var buffer);

                // get declarations from the compilation's assembly
                await AddCompilationSourceDeclarationsWithNormalQueryAsync(
                    project, query, criteria, buffer, cancellationToken).ConfigureAwait(false);

                // No need to map symbols since we're looking in the starting project.
                await AddAllAsync(buffer, mapSymbol: false).ConfigureAwait(false);
            }

            async Task SearchProjectReferencesAsync()
            {
                // get declarations from directly referenced projects
                foreach (var projectReference in project.ProjectReferences)
                {
                    using var _ = ArrayBuilder<ISymbol>.GetInstance(out var buffer);

                    var referencedProject = project.Solution.GetProject(projectReference.ProjectId);
                    if (referencedProject is null)
                        continue;

                    await AddCompilationSourceDeclarationsWithNormalQueryAsync(
                        referencedProject, query, criteria, buffer, cancellationToken).ConfigureAwait(false);

                    // Add all the results.  If they're from a different language, attempt to map them back into the
                    // starting project's language.
                    await AddAllAsync(buffer, mapSymbol: referencedProject.Language != project.Language).ConfigureAwait(false);
                }
            }

            async Task SearchMetadataReferencesAsync()
            {
                // get declarations from directly referenced metadata
                foreach (var peReference in project.MetadataReferences.OfType<PortableExecutableReference>())
                {
                    using var _ = ArrayBuilder<ISymbol>.GetInstance(out var buffer);

                    var lazyAssembly = AsyncLazy.Create(async cancellationToken =>
                    {
                        var compilation = await lazyCompilation.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(peReference) as IAssemblySymbol;
                        return assemblySymbol;
                    });

                    await AddMetadataDeclarationsWithNormalQueryAsync(
                        project, lazyAssembly, peReference,
                        query, criteria, buffer, cancellationToken).ConfigureAwait(false);

                    // No need to map symbols since we're looking in metadata.  They will always be in the language of our starting project.
                    await AddAllAsync(buffer, mapSymbol: false).ConfigureAwait(false);
                }
            }

            async Task AddAllAsync(ArrayBuilder<ISymbol> buffer, bool mapSymbol)
            {
                if (buffer.Count == 0)
                    return;

                var compilation = await lazyCompilation.GetValueAsync(cancellationToken).ConfigureAwait(false);

                // Make certain all namespace symbols returned by API are from the compilation
                // for the passed in project.
                foreach (var symbol in buffer)
                {
                    var mappedSymbol = mapSymbol
                        ? symbol.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol
                        : symbol;

                    result.AddIfNotNull(mappedSymbol is INamespaceSymbol ns
                        ? compilation.GetCompilationNamespace(ns)
                        : mappedSymbol);
                }
            }
        }

        private static async Task<ImmutableArray<ISymbol>> RehydrateAsync(
            Solution solution, IList<SerializableSymbolAndProjectId> array, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<ISymbol>.GetInstance(array.Count);

            foreach (var dehydrated in array)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rehydrated = await dehydrated.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (rehydrated != null)
                {
                    result.Add(rehydrated);
                }
            }

            return result.ToImmutableAndFree();
        }
    }
}
