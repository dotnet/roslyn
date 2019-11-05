// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    /// <summary>
    /// Provides completion items for extension methods from unimported namespace.
    /// </summary>
    /// <remarks>It runs out-of-proc if it's enabled</remarks>
    internal static partial class ExtensionMethodImportCompletionHelper
    {
        private static readonly char[] s_dotSeparator = new char[] { '.' };

        private static readonly object s_gate = new object();
        private static Task s_indexingTask = Task.CompletedTask;

        public static async Task<ImmutableArray<SerializableImportCompletionItem>> GetUnimportedExtensionMethodsAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            var ticks = Environment.TickCount;
            var project = document.Project;

            // This service is only defined for C# and VB, but we'll be a bit paranoid.
            var client = RemoteSupportedLanguages.IsSupported(project.Language)
                ? await project.Solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false)
                : null;

            var (serializableItems, counter) = client == null
                ? await GetUnimportedExtensionMethodsInCurrentProcessAsync(document, position, receiverTypeSymbol, namespaceInScope, forceIndexCreation, cancellationToken).ConfigureAwait(false)
                : await GetUnimportedExtensionMethodsInRemoteProcessAsync(client, document, position, receiverTypeSymbol, namespaceInScope, forceIndexCreation, cancellationToken).ConfigureAwait(false);

            counter.TotalTicks = Environment.TickCount - ticks;
            counter.TotalExtensionMethodsProvided = serializableItems.Length;
            counter.Report();

            return serializableItems;
        }

        public static async Task<(ImmutableArray<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportedExtensionMethodsInRemoteProcessAsync(
            RemoteHostClient client,
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var (serializableItems, counter) = await client.TryRunCodeAnalysisRemoteAsync<(IList<SerializableImportCompletionItem>, StatisticCounter)>(
                project.Solution,
                nameof(IRemoteExtensionMethodImportCompletionService.GetUnimportedExtensionMethodsAsync),
                new object[] { document.Id, position, SymbolKey.CreateString(receiverTypeSymbol), namespaceInScope.ToArray(), forceIndexCreation },
                cancellationToken).ConfigureAwait(false);

            return (serializableItems.ToImmutableArray(), counter);
        }

        public static async Task<(ImmutableArray<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportedExtensionMethodsInCurrentProcessAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            var counter = new StatisticCounter();
            var ticks = Environment.TickCount;

            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Get the metadata name of all the base types and interfaces this type derived from.
            using var _ = PooledHashSet<string>.GetInstance(out var allTypeNamesBuilder);
            allTypeNamesBuilder.Add(receiverTypeSymbol.MetadataName);
            allTypeNamesBuilder.AddRange(receiverTypeSymbol.GetBaseTypes().Select(t => t.MetadataName));
            allTypeNamesBuilder.AddRange(receiverTypeSymbol.GetAllInterfacesIncludingThis().Select(t => t.MetadataName));

            // interface doesn't inherit from object, but is implicitly convertable to object type.
            if (receiverTypeSymbol.IsInterfaceType())
            {
                allTypeNamesBuilder.Add(nameof(Object));
            }

            var allTypeNames = allTypeNamesBuilder.ToImmutableArray();
            var indicesResult = await TryGetIndicesAsync(
                document.Project, forceIndexCreation, cancellationToken).ConfigureAwait(false);

            // Don't show unimported extension methods if the index isn't ready.
            if (!indicesResult.HasResult)
            {
                // We use a very simple approach to build the cache in the background:
                // queue a new task only if the previous task is completed, regardless of what
                // that task is.
                lock (s_gate)
                {
                    if (s_indexingTask.IsCompleted)
                    {
                        s_indexingTask = Task.Run(() => TryGetIndicesAsync(document.Project, forceIndexCreation: true, CancellationToken.None));
                    }
                }

                return (ImmutableArray<SerializableImportCompletionItem>.Empty, counter);
            }

            var matchedMethods = CreateAggregatedFilter(allTypeNames, indicesResult.SyntaxIndices, indicesResult.SymbolInfos);

            counter.GetFilterTicks = Environment.TickCount - ticks;
            counter.NoFilter = !indicesResult.HasResult;

            ticks = Environment.TickCount;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var items = GetExtensionMethodItems(compilation.GlobalNamespace, receiverTypeSymbol,
                semanticModel!, position, namespaceInScope, matchedMethods, counter, cancellationToken);

            counter.GetSymbolTicks = Environment.TickCount - ticks;

            return (items, counter);
        }

        private static async Task<GetIndicesResult> TryGetIndicesAsync(
            Project currentProject,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            var solution = currentProject.Solution;
            var cacheService = GetCacheService(solution.Workspace);
            var graph = currentProject.Solution.GetProjectDependencyGraph();
            var relevantProjectIds = graph.GetProjectsThatThisProjectTransitivelyDependsOn(currentProject.Id)
                                        .Concat(currentProject.Id);

            using var syntaxDisposer = ArrayBuilder<CacheEntry>.GetInstance(out var syntaxBuilder);
            using var symbolDisposer = ArrayBuilder<SymbolTreeInfo>.GetInstance(out var symbolBuilder);

            foreach (var projectId in relevantProjectIds)
            {
                var project = solution.GetProject(projectId);
                if (project == null || !project.SupportsCompilation)
                {
                    continue;
                }

                // By default, don't trigger index creation except for documents in current project.
                var loadOnly = !forceIndexCreation && projectId != currentProject.Id;
                var cacheEntry = await GetCacheEntryAsync(project, loadOnly, cacheService, cancellationToken).ConfigureAwait(false);

                if (cacheEntry == null)
                {
                    // Don't provide anything if we don't have all the required SyntaxTreeIndex created.
                    return GetIndicesResult.NoneResult;
                }

                syntaxBuilder.Add(cacheEntry.Value);
            }

            // Search through all direct PE references.
            foreach (var peReference in currentProject.MetadataReferences.OfType<PortableExecutableReference>())
            {
                var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                    solution, peReference, loadOnly: !forceIndexCreation, cancellationToken).ConfigureAwait(false);

                if (info == null)
                {
                    // Don't provide anything if we don't have all the required SymbolTreeInfo created.
                    return GetIndicesResult.NoneResult;
                }

                if (info.ContainsExtensionMethod)
                {
                    symbolBuilder.Add(info);
                }
            }

            var syntaxIndices = syntaxBuilder.ToImmutable();
            var symbolInfos = symbolBuilder.ToImmutable();

            return new GetIndicesResult(hasResult: true, syntaxIndices, symbolInfos);
        }

        private static MultiDictionary<string, string> CreateAggregatedFilter(ImmutableArray<string> targetTypeNames, ImmutableArray<CacheEntry> syntaxIndices, ImmutableArray<SymbolTreeInfo> symbolInfos)
        {
            var results = new MultiDictionary<string, string>();

            // Find matching extension methods from source.
            foreach (var index in syntaxIndices)
            {
                // Add simple extension methods with matching target type name
                foreach (var targetTypeName in targetTypeNames)
                {
                    var methodInfos = index.SimpleExtensionMethodInfo[targetTypeName];
                    if (methodInfos.Count == 0)
                    {
                        continue;
                    }

                    foreach (var methodInfo in methodInfos)
                    {
                        results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                    }
                }

                // Add all complex extension methods, we will need to completely rely on symbols to match them.
                foreach (var methodInfo in index.ComplexExtensionMethodInfo)
                {
                    results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                }
            }

            // Find matching extension methods from metadata
            foreach (var info in symbolInfos)
            {
                var methodInfos = info.GetMatchingExtensionMethodInfo(targetTypeNames);
                foreach (var methodInfo in methodInfos)
                {
                    results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                }
            }

            return results;
        }

        private static ImmutableArray<SerializableImportCompletionItem> GetExtensionMethodItems(
            INamespaceSymbol rootNamespaceSymbol,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel semanticModel,
            int position,
            ISet<string> namespaceFilter,
            MultiDictionary<string, string> methodNameFilter,
            StatisticCounter counter,
            CancellationToken cancellationToken)
        {
            var compilation = semanticModel.Compilation;
            using var _ = ArrayBuilder<SerializableImportCompletionItem>.GetInstance(out var builder);

            using var conflictTypeRootNode = new ConflictNameNode(name: string.Empty);

            foreach (var (fullyQualifiedContainerName, methodNames) in methodNameFilter)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var indexOfLastDot = fullyQualifiedContainerName.LastIndexOf('.');
                var qualifiedNamespaceName = indexOfLastDot > 0 ? fullyQualifiedContainerName.Substring(0, indexOfLastDot) : string.Empty;

                if (namespaceFilter.Contains(qualifiedNamespaceName))
                {
                    continue;
                }

                // Container of extension method (static class in C# and Module in VB) can't be generic or nested.
                // Note that we might incorrectly ignore valid types, because, for example, calling `GetTypeByMetadataName` 
                // would return null if we have multiple definitions of a type even though only one is accessible from here
                // (e.g. an internal type declared inside a shared document).
                var containerSymbol = compilation.GetTypeByMetadataName(fullyQualifiedContainerName);

                if (containerSymbol != null)
                {
                    GetItemsFromTypeContainsPotentialMatches(containerSymbol, qualifiedNamespaceName, methodNames, receiverTypeSymbol, semanticModel, position, counter, builder);
                }
                else
                {
                    conflictTypeRootNode.Add(fullyQualifiedContainerName, (qualifiedNamespaceName, methodNames));
                }
            }

            var ticks = Environment.TickCount;

            GetItemsFromConflictingTypes(rootNamespaceSymbol, conflictTypeRootNode, builder, receiverTypeSymbol, semanticModel, position, counter);

            counter.GetSymbolExtraTicks = Environment.TickCount - ticks;

            return builder.ToImmutable();
        }

        private static void GetItemsFromTypeContainsPotentialMatches(
            INamedTypeSymbol containerSymbol,
            string qualifiedNamespaceName,
            MultiDictionary<string, string>.ValueSet methodNames,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel semanticModel,
            int position,
            StatisticCounter counter,
            ArrayBuilder<SerializableImportCompletionItem> builder)
        {
            counter.TotalTypesChecked++;

            if (containerSymbol == null ||
                !containerSymbol.MightContainExtensionMethods ||
                !IsSymbolAccessible(containerSymbol, position, semanticModel))
            {
                return;
            }

            foreach (var methodName in methodNames)
            {
                var methodSymbols = containerSymbol.GetMembers(methodName).OfType<IMethodSymbol>();

                foreach (var methodSymbol in methodSymbols)
                {
                    counter.TotalExtensionMethodsChecked++;
                    IMethodSymbol? reducedMethodSymbol = null;

                    if (methodSymbol.IsExtensionMethod &&
                        IsSymbolAccessible(methodSymbol, position, semanticModel))
                    {
                        reducedMethodSymbol = methodSymbol.ReduceExtensionMethod(receiverTypeSymbol);
                    }

                    if (reducedMethodSymbol != null)
                    {
                        var symbolKeyData = SymbolKey.CreateString(reducedMethodSymbol);
                        builder.Add(new SerializableImportCompletionItem(
                            symbolKeyData,
                            reducedMethodSymbol.Name,
                            reducedMethodSymbol.Arity,
                            reducedMethodSymbol.GetGlyph(),
                            qualifiedNamespaceName));
                    }
                }
            }
        }

        private static void GetItemsFromConflictingTypes(
            INamespaceSymbol containingNamespaceSymbol,
            ConflictNameNode conflictTypeNodes,
            ArrayBuilder<SerializableImportCompletionItem> builder,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel semanticModel,
            int position,
            StatisticCounter counter)
        {
            Debug.Assert(!conflictTypeNodes.NamespaceAndMethodNames.HasValue);

            foreach (var child in conflictTypeNodes.Children.Values)
            {
                if (child.NamespaceAndMethodNames == null)
                {
                    var childNamespace = containingNamespaceSymbol.GetMembers(child.Name).OfType<INamespaceSymbol>().FirstOrDefault();
                    if (childNamespace != null)
                    {
                        GetItemsFromConflictingTypes(childNamespace, child, builder, receiverTypeSymbol, semanticModel, position, counter);
                    }
                }
                else
                {
                    var types = containingNamespaceSymbol.GetMembers(child.Name).OfType<INamedTypeSymbol>();
                    foreach (var type in types)
                    {
                        var (namespaceName, methodNames) = child.NamespaceAndMethodNames.Value;
                        GetItemsFromTypeContainsPotentialMatches(type, namespaceName, methodNames, receiverTypeSymbol, semanticModel, position, counter, builder);
                    }
                }
            }
        }

        // We only call this when the containing symbol is accessible, 
        // so being declared as public means this symbol is also accessible.
        private static bool IsSymbolAccessible(ISymbol symbol, int position, SemanticModel semanticModel)
            => symbol.DeclaredAccessibility == Accessibility.Public || semanticModel.IsAccessible(position, symbol);

        /// <summary>
        /// The purpose of this is to help us keeping track of conflicting types with data required to create 
        /// corresponding items in a tree, which is easy to use while navigating symbol tree recursively.
        /// For example, two internal classes with identical fully qualified name but declared in two different
        /// projects would be a conflict, even if only one is accessible from project that triggered the completion.
        /// </summary>
        private class ConflictNameNode : IDisposable
        {
            /// <summary>
            /// Holds the name of either a namespace name or a type (which is causing conflict).
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Child nodes. Only used when this node is a namespace node.
            /// </summary>
            public PooledDictionary<string, ConflictNameNode> Children { get; }

            /// <summary>
            /// Data needed to create a completion item based on a symbol. Not null only when this node is a type node.
            /// </summary>
            public (string namespaceName, MultiDictionary<string, string>.ValueSet methodNames)? NamespaceAndMethodNames { get; private set; }

            public ConflictNameNode(string name)
            {
                Name = name;
                Children = PooledDictionary<string, ConflictNameNode>.GetInstance();
            }

            public void Add(string fullyQualifiedContainerName, (string namespaceName, MultiDictionary<string, string>.ValueSet methodNames) namespaceAndMethodNames)
            {
                var parts = fullyQualifiedContainerName.Split(s_dotSeparator);

                var current = this;
                foreach (var part in parts)
                {
                    if (!current.Children.TryGetValue(part, out var child))
                    {
                        child = new ConflictNameNode(part);
                        current.Children.Add(part, child);
                    }

                    current = child;
                }

                // Type and Namespace can't have identical name
                Debug.Assert(current.Children.Count == 0);
                current.NamespaceAndMethodNames = namespaceAndMethodNames;
            }

            public void Dispose()
            {
                foreach (var childNode in Children.Values)
                {
                    childNode.Dispose();
                }

                Children.Free();
            }
        }

        private readonly struct GetIndicesResult
        {
            public bool HasResult { get; }
            public ImmutableArray<CacheEntry> SyntaxIndices { get; }
            public ImmutableArray<SymbolTreeInfo> SymbolInfos { get; }

            public GetIndicesResult(bool hasResult, ImmutableArray<CacheEntry> syntaxIndices = default, ImmutableArray<SymbolTreeInfo> symbolInfos = default)
            {
                HasResult = hasResult;
                SyntaxIndices = syntaxIndices;
                SymbolInfos = symbolInfos;
            }

            public static GetIndicesResult NoneResult => new GetIndicesResult(hasResult: false);
        }
    }

    internal sealed class StatisticCounter
    {
        public bool NoFilter;
        public int TotalTicks;
        public int TotalExtensionMethodsProvided;
        public int GetFilterTicks;
        public int GetSymbolTicks;
        public int GetSymbolExtraTicks;
        public int TotalTypesChecked;
        public int TotalExtensionMethodsChecked;

        public void Report()
        {
            if (NoFilter)
            {
                CompletionProvidersLogger.LogExtensionMethodCompletionSuccess();
            }
            else
            {
                CompletionProvidersLogger.LogExtensionMethodCompletionTicksDataPoint(TotalTicks);
                CompletionProvidersLogger.LogExtensionMethodCompletionMethodsProvidedDataPoint(TotalExtensionMethodsProvided);
                CompletionProvidersLogger.LogExtensionMethodCompletionGetFilterTicksDataPoint(GetFilterTicks);
                CompletionProvidersLogger.LogExtensionMethodCompletionGetSymbolTicksDataPoint(GetSymbolTicks);
                CompletionProvidersLogger.LogExtensionMethodCompletionTypesCheckedDataPoint(TotalTypesChecked);
                CompletionProvidersLogger.LogExtensionMethodCompletionMethodsCheckedDataPoint(TotalExtensionMethodsChecked);
            }
        }
    }
}
