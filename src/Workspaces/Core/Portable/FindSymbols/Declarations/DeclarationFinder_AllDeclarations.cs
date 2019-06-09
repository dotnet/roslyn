// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public static async Task<ImmutableArray<SymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            Project project, SearchQuery query, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var (succeeded, results) = await TryFindAllDeclarationsWithNormalQueryInRemoteProcessAsync(
                project, query, criteria, cancellationToken).ConfigureAwait(false);

            if (succeeded)
            {
                return results;
            }

            return await FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
                project, query, criteria, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
            Project project, SearchQuery query, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var list = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            if (project.SupportsCompilation)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                // get declarations from the compilation's assembly
                await AddCompilationDeclarationsWithNormalQueryAsync(
                    project, query, criteria, list, cancellationToken).ConfigureAwait(false);

                // get declarations from directly referenced projects and metadata
                foreach (var assembly in compilation.GetReferencedAssemblySymbols())
                {
                    var assemblyProject = project.Solution.GetProject(assembly, cancellationToken);
                    if (assemblyProject != null)
                    {
                        await AddCompilationDeclarationsWithNormalQueryAsync(
                            assemblyProject, query, criteria, list,
                            compilation, assembly, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddMetadataDeclarationsWithNormalQueryAsync(
                            project, assembly, compilation.GetMetadataReference(assembly) as PortableExecutableReference,
                            query, criteria, list, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Make certain all namespace symbols returned by API are from the compilation
                // for the passed in project.
                for (var i = 0; i < list.Count; i++)
                {
                    var symbolAndProjectId = list[i];
                    if (symbolAndProjectId.Symbol is INamespaceSymbol ns)
                    {
                        list[i] = new SymbolAndProjectId(
                            compilation.GetCompilationNamespace(ns),
                            project.Id);
                    }
                }
            }

            return list.ToImmutableAndFree();
        }

        private static async Task<(bool, ImmutableArray<SymbolAndProjectId>)> TryFindAllDeclarationsWithNormalQueryInRemoteProcessAsync(
            Project project, SearchQuery query, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return (false, ImmutableArray<SymbolAndProjectId>.Empty);
            }

            var result = await project.Solution.TryRunCodeAnalysisRemoteAsync<IList<SerializableSymbolAndProjectId>>(
                nameof(IRemoteSymbolFinder.FindAllDeclarationsWithNormalQueryAsync),
                new object[] { project.Id, query.Name, query.Kind, criteria }, cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return (false, ImmutableArray<SymbolAndProjectId>.Empty);
            }

            var rehydrated = await RehydrateAsync(
                project.Solution, result, cancellationToken).ConfigureAwait(false);

            return (true, rehydrated);
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> RehydrateAsync(
            Solution solution, IList<SerializableSymbolAndProjectId> array, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance(array.Count);

            foreach (var dehydrated in array)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rehydrated = await dehydrated.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (rehydrated != null)
                {
                    result.Add(rehydrated.Value);
                }
            }

            return result.ToImmutableAndFree();
        }
    }
}
