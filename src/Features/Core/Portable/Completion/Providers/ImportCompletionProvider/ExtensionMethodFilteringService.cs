// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class ExtensionMethodFilteringService
    {
        private static async Task<RemoteHostClient?> TryGetRemoteHostClientAsync(Project project, CancellationToken cancellationToken)
        {
            // This service is only defined for C# and VB, but we'll be a bit paranoid.
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return null;
            }

            return await project.Solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<MultiDictionary<string, string>?> GetPossibleExtensionMethodMatchesAsync(Project project, ImmutableHashSet<string> targetTypeNames, bool loadOnly, CancellationToken cancellationToken)
        {
            var client = await TryGetRemoteHostClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetPossibleExtensionMethodMatchesInCurrentProcessAsync(
                    project, targetTypeNames, loadOnly, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await GetPossibleExtensionMethodMatchesInRemoteProcessAsync(
                    client, project, targetTypeNames, loadOnly, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<MultiDictionary<string, string>?> GetPossibleExtensionMethodMatchesInRemoteProcessAsync(RemoteHostClient client, Project project, ImmutableHashSet<string> targetTypeNames, bool loadOnly, CancellationToken cancellationToken)
        {
            var remoteResults = await client.TryRunCodeAnalysisRemoteAsync<IEnumerable<(string, IEnumerable<string>)>>(
                project.Solution,
                nameof(IRemoteExtensionMethodFilteringService.GetPossibleExtensionMethodMatchesAsync),
                new object[] { project.Id, targetTypeNames.ToArray(), loadOnly },
                cancellationToken).ConfigureAwait(false);

            if (!remoteResults.Any())
            {
                return null;
            }

            // Reconstruct the dictionary from remote results.
            var results = new MultiDictionary<string, string>();

            foreach (var pair in remoteResults)
            {
                foreach (var value in pair.Item2)
                {
                    results.Add(pair.Item1, value);
                }
            }

            return results;
        }

        private static async Task<MultiDictionary<string, string>?> GetPossibleExtensionMethodMatchesInCurrentProcessAsync(Project currentProject, ImmutableHashSet<string> targetTypeNames, bool loadOnly, CancellationToken cancellationToken)
        {
            var solution = currentProject.Solution;
            var graph = currentProject.Solution.GetProjectDependencyGraph();
            var relevantProjectIds = graph.GetProjectsThatThisProjectTransitivelyDependsOn(currentProject.Id)
                                        .Concat(currentProject.Id);

            using var syntaxDisposer = ArrayBuilder<SyntaxTreeIndex>.GetInstance(out var syntaxTreeIndexBuilder);
            using var symbolDisposer = ArrayBuilder<SymbolTreeInfo>.GetInstance(out var symbolTreeInfoBuilder);
            var peReferences = PooledHashSet<PortableExecutableReference>.GetInstance();

            try
            {
                foreach (var projectId in relevantProjectIds.Concat(currentProject.Id))
                {
                    // Alway create indices for documents in current project if they don't exist.
                    loadOnly &= projectId != currentProject.Id;

                    var project = solution.GetProject(projectId);
                    if (project == null || !project.SupportsCompilation)
                    {
                        continue;
                    }

                    // Transitively get all the PE references
                    peReferences.AddRange(project.MetadataReferences.OfType<PortableExecutableReference>());
                    foreach (var document in project.Documents)
                    {
                        // Don't look for extension methods in generated code.
                        if (document.State.Attributes.IsGenerated)
                        {
                            continue;
                        }

                        var info = await document.GetSyntaxTreeIndexAsync(loadOnly, cancellationToken).ConfigureAwait(false);

                        // Don't provide anyting if we don't have all the required SyntaxTreeIndex created.
                        if (info == null)
                        {
                            return null;
                        }

                        if (info.ContainsExtensionMethod)
                        {
                            syntaxTreeIndexBuilder.Add(info);
                        }
                    }
                }

                foreach (var peReference in peReferences)
                {
                    var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                        solution, peReference, loadOnly, cancellationToken).ConfigureAwait(false);

                    // Don't provide anyting if we don't have all the required SymbolTreeInfo created.
                    if (info == null)
                    {
                        return null;
                    }

                    if (info.ContainsExtensionMethod)
                    {
                        symbolTreeInfoBuilder.Add(info);
                    }
                }

                var results = new MultiDictionary<string, string>();

                // Find matching extension methods from source.
                foreach (var info in syntaxTreeIndexBuilder)
                {
                    // Add simple extension methods with matching target type name
                    foreach (var targetTypeName in targetTypeNames)
                    {
                        if (!info.SimpleExtensionMethodInfo.TryGetValue(targetTypeName, out var methodInfoIndices))
                        {
                            continue;
                        }

                        foreach (var index in methodInfoIndices)
                        {
                            if (info.TryGetDeclaredSymbolInfo(index, out var methodInfo))
                            {
                                results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                            }
                        }
                    }

                    // Add all complex extension methods, we will need to completely rely on symbols to match them.
                    foreach (var index in info.ComplexExtensionMethodInfo)
                    {
                        if (info.TryGetDeclaredSymbolInfo(index, out var methodInfo))
                        {
                            results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                        }
                    }
                }

                // Find matching extension methods from metadata
                foreach (var info in symbolTreeInfoBuilder)
                {
                    foreach (var targetTypeName in targetTypeNames)
                    {
                        foreach (var methodInfo in info.GetMatchingExtensionMethodInfo(targetTypeName))
                        {
                            results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                        }
                    }
                }

                return results;
            }
            finally
            {
                peReferences.Free();
            }
        }
    }
}
