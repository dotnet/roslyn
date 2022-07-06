// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            using var _1 = ArrayBuilder<ISymbol>.GetInstance(out var buffer);
            using var _2 = ArrayBuilder<ISymbol>.GetInstance(out var result);

            if (project.SupportsCompilation)
            {
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                // get declarations from the compilation's assembly
                await AddCompilationDeclarationsWithNormalQueryAsync(
                    project, query, criteria, buffer, cancellationToken).ConfigureAwait(false);

                // get declarations from directly referenced projects and metadata
                foreach (var assembly in compilation.GetReferencedAssemblySymbols())
                {
                    var assemblyProject = project.Solution.GetProject(assembly, cancellationToken);
                    if (assemblyProject != null)
                    {
                        await AddCompilationDeclarationsWithNormalQueryAsync(
                            assemblyProject, query, criteria, buffer,
                            compilation, assembly, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddMetadataDeclarationsWithNormalQueryAsync(
                            project, assembly, compilation.GetMetadataReference(assembly) as PortableExecutableReference,
                            query, criteria, buffer, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Make certain all namespace symbols returned by API are from the compilation
                // for the passed in project.
                foreach (var symbol in buffer)
                {
                    if (symbol is INamespaceSymbol ns)
                    {
                        result.AddIfNotNull(compilation.GetCompilationNamespace(ns));
                    }
                    else
                    {
                        result.Add(symbol);
                    }
                }
            }

            return result.ToImmutable();
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
