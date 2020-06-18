// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            async Task<(ImmutableArray<SerializableImportCompletionItem>, StatisticCounter)> GetItemsAsync()
            {
                var project = document.Project;

                var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var result = await client.RunRemoteAsync<(IList<SerializableImportCompletionItem> items, StatisticCounter counter)>(
                        WellKnownServiceHubService.CodeAnalysis,
                        nameof(IRemoteExtensionMethodImportCompletionService.GetUnimportedExtensionMethodsAsync),
                        project.Solution,
                        new object[] { document.Id, position, SymbolKey.CreateString(receiverTypeSymbol, cancellationToken), namespaceInScope.ToArray(), forceIndexCreation },
                        callbackTarget: null,
                        cancellationToken).ConfigureAwait(false);

                    return (result.items.ToImmutableArray(), result.counter);
                }

                return await GetUnimportedExtensionMethodsInCurrentProcessAsync(document, position, receiverTypeSymbol, namespaceInScope, forceIndexCreation, cancellationToken).ConfigureAwait(false);
            }

            var ticks = Environment.TickCount;

            var (items, counter) = await GetItemsAsync().ConfigureAwait(false);

            counter.TotalTicks = Environment.TickCount - ticks;
            counter.TotalExtensionMethodsProvided = items.Length;
            counter.Report();

            return items;
        }

        /// <summary>
        /// Get the metadata name of all the base types and interfaces this type derived from.
        /// </summary>
        private static ImmutableArray<string> GetReceiverTypeNames(ITypeSymbol receiverTypeSymbol)
        {
            using var _ = PooledHashSet<string>.GetInstance(out var allTypeNamesBuilder);
            AddNamesForTypeWorker(receiverTypeSymbol, allTypeNamesBuilder);
            return allTypeNamesBuilder.ToImmutableArray();

            static void AddNamesForTypeWorker(ITypeSymbol receiverTypeSymbol, PooledHashSet<string> builder)
            {
                if (receiverTypeSymbol is ITypeParameterSymbol typeParameter)
                {
                    foreach (var constraintType in typeParameter.ConstraintTypes)
                    {
                        AddNamesForTypeWorker(constraintType, builder);
                    }
                }
                else
                {
                    builder.Add(GetReceiverTypeName(receiverTypeSymbol));
                    builder.AddRange(receiverTypeSymbol.GetBaseTypes().Select(t => t.MetadataName));
                    builder.AddRange(receiverTypeSymbol.GetAllInterfacesIncludingThis().Select(t => t.MetadataName));

                    // interface doesn't inherit from object, but is implicitly convertible to object type.
                    if (receiverTypeSymbol.IsInterfaceType())
                    {
                        builder.Add(nameof(Object));
                    }
                }
            }
        }

        private static string GetReceiverTypeName(ITypeSymbol typeSymbol)
        {
            switch (typeSymbol)
            {
                case INamedTypeSymbol namedType:
                    return namedType.IsTupleType ? string.Empty : namedType.MetadataName;

                case IArrayTypeSymbol arrayType:
                    var elementType = arrayType.ElementType;
                    while (elementType is IArrayTypeSymbol symbol)
                    {
                        elementType = symbol.ElementType;
                    }

                    var elementTypeName = GetReceiverTypeName(elementType);

                    // We do not differentiate array of different kinds sicne they are all represented in the indices as "NonArrayElementTypeName[]"
                    // e.g. int[], int[][], int[,], etc. are all represented as "int[]", whereas array of complex type such as T[] is "[]".
                    return elementTypeName + "[]";

                default:
                    // Complex types are represented by "";
                    return string.Empty;
            }
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

            counter.GetFilterTicks = Environment.TickCount - ticks;
            counter.NoFilter = !indicesResult.HasResult;

            ticks = Environment.TickCount;

            var items = await GetExtensionMethodItemsAsync(
                document, receiverTypeSymbol, GetReceiverTypeNames(receiverTypeSymbol), indicesResult, position,
                namespaceInScope, counter, cancellationToken).ConfigureAwait(false);

            counter.GetSymbolTicks = Environment.TickCount - ticks;

            return (items, counter);
        }

        private static async Task<ImmutableArray<SerializableImportCompletionItem>> GetExtensionMethodItemsAsync(
            Document document,
            ITypeSymbol receiverTypeSymbol,
            ImmutableArray<string> receiverTypeNames,
            GetIndicesResult indices,
            int position,
            ISet<string> namespaceFilter,
            StatisticCounter counter,
            CancellationToken cancellationToken)
        {
            if (!indices.HasResult)
            {
                return ImmutableArray<SerializableImportCompletionItem>.Empty;
            }

            var currentProject = document.Project;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var currentCompilation = semanticModel.Compilation;
            var currentAssembly = currentCompilation.Assembly;

            using var _1 = ArrayBuilder<SerializableImportCompletionItem>.GetInstance(out var completionItemsbuilder);
            using var _2 = PooledDictionary<INamespaceSymbol, string>.GetInstance(out var namespaceNameCache);

            receiverTypeNames = AttachComplexTypes(receiverTypeNames);

            // Get extension method items from source
            foreach (var (project, syntaxIndex) in indices.SyntaxIndices!)
            {
                var filter = CreateAggregatedFilter(receiverTypeNames, syntaxIndex);
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var assembly = compilation.Assembly;

                var internalsVisible = currentAssembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

                var matchingMethodSymbols = GetPotentialMatchingSymbolsFromAssembly(
                    compilation.Assembly, filter, namespaceFilter, internalsVisible,
                    counter, cancellationToken);

                var isSymbolFromCurrentCompilation = project == currentProject;
                GetExtensionMethodItemsWorker(
                    position, semanticModel, receiverTypeSymbol, matchingMethodSymbols, isSymbolFromCurrentCompilation,
                    completionItemsbuilder, namespaceNameCache, cancellationToken);
            }

            // Get extension method items from PE
            foreach (var (peReference, symbolInfo) in indices.SymbolInfos!)
            {
                var filter = CreateAggregatedFilter(receiverTypeNames, symbolInfo);
                if (currentCompilation.GetAssemblyOrModuleSymbol(peReference) is IAssemblySymbol assembly)
                {
                    var internalsVisible = currentAssembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

                    var matchingMethodSymbols = GetPotentialMatchingSymbolsFromAssembly(
                        assembly, filter, namespaceFilter, internalsVisible,
                        counter, cancellationToken);

                    GetExtensionMethodItemsWorker(
                        position, semanticModel, receiverTypeSymbol, matchingMethodSymbols,
                        isSymbolFromCurrentCompilation: true, completionItemsbuilder, namespaceNameCache, cancellationToken);
                }
            }

            return completionItemsbuilder.ToImmutable();

            // Add string represent complex types (i.e. "" and "[]") to the receiver type, so we would include in the filter
            // info about extension methods with complex receiver type.
            static ImmutableArray<string> AttachComplexTypes(ImmutableArray<string> receiverTypeNames)
            {
                using var _ = ArrayBuilder<string>.GetInstance(receiverTypeNames.Length + 2, out var receiverTypeNamesBuilder);
                receiverTypeNamesBuilder.AddRange(receiverTypeNames);
                receiverTypeNamesBuilder.Add(string.Empty);
                receiverTypeNamesBuilder.Add("[]");

                return receiverTypeNamesBuilder.ToImmutable();
            }
        }

        private static void GetExtensionMethodItemsWorker(
            int position,
            SemanticModel semanticModel,
            ITypeSymbol receiverTypeSymbol,
            ImmutableArray<IMethodSymbol> matchingMethodSymbols,
            bool isSymbolFromCurrentCompilation,
            ArrayBuilder<SerializableImportCompletionItem> builder,
            Dictionary<INamespaceSymbol, string> stringCache,
            CancellationToken cancellationToken)
        {
            foreach (var methodSymbol in matchingMethodSymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Symbols could be from a different compilation.
                // Need to find the matching one in current compilation
                // before any further checks is done.
                var methodSymbolInCurrentCompilation = isSymbolFromCurrentCompilation
                                                        ? methodSymbol
                                                        : SymbolFinder.FindSimilarSymbols(methodSymbol, semanticModel.Compilation).FirstOrDefault();

                if (methodSymbolInCurrentCompilation == null
                    || !semanticModel.IsAccessible(position, methodSymbolInCurrentCompilation))
                {
                    continue;
                }

                var reducedMethodSymbol = methodSymbolInCurrentCompilation.ReduceExtensionMethod(receiverTypeSymbol);
                if (reducedMethodSymbol != null)
                {
                    var symbolKeyData = SymbolKey.CreateString(reducedMethodSymbol);
                    builder.Add(new SerializableImportCompletionItem(
                        symbolKeyData,
                        reducedMethodSymbol.Name,
                        reducedMethodSymbol.Arity,
                        reducedMethodSymbol.GetGlyph(),
                        GetFullyQualifiedNamespaceName(reducedMethodSymbol.ContainingNamespace, stringCache)));
                }
            }
        }

        private static string GetFullyQualifiedNamespaceName(INamespaceSymbol symbol, Dictionary<INamespaceSymbol, string> stringCache)
        {
            if (symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace)
            {
                return symbol.Name;
            }

            if (stringCache.TryGetValue(symbol, out var name))
            {
                return name;
            }

            name = GetFullyQualifiedNamespaceName(symbol.ContainingNamespace, stringCache) + "." + symbol.Name;
            stringCache[symbol] = name;
            return name;
        }

        private static ImmutableArray<IMethodSymbol> GetPotentialMatchingSymbolsFromAssembly(
            IAssemblySymbol assembly,
            MultiDictionary<string, (string, string)> extensionMethodFilter,
            ISet<string> namespaceFilter,
            bool internalsVisible,
            StatisticCounter counter,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var builder);

            foreach (var (fullyQualifiedContainerName, methodNames) in extensionMethodFilter)
            {
                // First try to filter out types from already imported namespaces
                var indexOfLastDot = fullyQualifiedContainerName.LastIndexOf('.');
                var qualifiedNamespaceName = indexOfLastDot > 0 ? fullyQualifiedContainerName.Substring(0, indexOfLastDot) : string.Empty;

                if (namespaceFilter.Contains(qualifiedNamespaceName))
                {
                    continue;
                }

                counter.TotalTypesChecked++;

                // Container of extension method (static class in C# and Module in VB) can't be generic or nested.
                var containerSymbol = assembly.GetTypeByMetadataName(fullyQualifiedContainerName);

                if (containerSymbol == null
                    || !containerSymbol.MightContainExtensionMethods
                    || !IsAccessible(containerSymbol, internalsVisible))
                {
                    continue;
                }

                foreach (var (methodName, receiverTypeName) in methodNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var methodSymbols = containerSymbol.GetMembers(methodName).OfType<IMethodSymbol>();

                    foreach (var methodSymbol in methodSymbols)
                    {
                        counter.TotalExtensionMethodsChecked++;

                        if (MatchExtensionMethod(methodSymbol, receiverTypeName, internalsVisible))
                        {
                            // Find a potential match.
                            builder.Add(methodSymbol);
                        }
                    }
                }
            }

            return builder.ToImmutable();

            static bool MatchExtensionMethod(IMethodSymbol method, string filterReceiverTypeName, bool internalsVisible)
            {
                if (!method.IsExtensionMethod || method.Parameters.IsEmpty || !IsAccessible(method, internalsVisible))
                {
                    return false;
                }

                // We get a match if the receiver type name match. 
                // For complex type, we would check if it matches with filter on whether it's an array.
                return filterReceiverTypeName.Length == 0 ||
                       string.Equals(filterReceiverTypeName, GetReceiverTypeName(method.Parameters[0].Type), StringComparison.Ordinal);
            }

            // An quick accessibility check based on declared accessibility only, a semantic based check is still required later.
            // Since we are dealing with extension methods and their container (top level static class and modules), only public,
            // internal and private modifiers are in play here. 
            // Also, this check is called for a method symbol only when the container was checked and is accessible.
            static bool IsAccessible(ISymbol symbol, bool internalsVisible) =>
                symbol.DeclaredAccessibility == Accessibility.Public ||
                (symbol.DeclaredAccessibility == Accessibility.Internal && internalsVisible);
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

            var syntaxIndices = new Dictionary<Project, CacheEntry>();
            var symbolInfos = new Dictionary<PortableExecutableReference, SymbolTreeInfo>();

            foreach (var projectId in relevantProjectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                syntaxIndices.Add(project, cacheEntry.Value);
            }

            // Search through all direct PE references.
            foreach (var peReference in currentProject.MetadataReferences.OfType<PortableExecutableReference>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                    solution, peReference, loadOnly: !forceIndexCreation, cancellationToken).ConfigureAwait(false);

                if (info == null)
                {
                    // Don't provide anything if we don't have all the required SymbolTreeInfo created.
                    return GetIndicesResult.NoneResult;
                }

                if (info.ContainsExtensionMethod)
                {
                    symbolInfos.Add(peReference, info);
                }
            }

            return new GetIndicesResult(syntaxIndices, symbolInfos);
        }

        // Create filter for extension methods from source.
        private static MultiDictionary<string, (string, string)> CreateAggregatedFilter(ImmutableArray<string> receiverTypeNames, CacheEntry syntaxIndex)
        {
            var results = new MultiDictionary<string, (string, string)>();

            foreach (var receiverTypeName in receiverTypeNames)
            {
                var methodInfos = syntaxIndex.ReceiverTypeNameToExtensionMethodMap[receiverTypeName];
                if (methodInfos.Count == 0)
                {
                    continue;
                }

                foreach (var methodInfo in methodInfos)
                {
                    results.Add(methodInfo.FullyQualifiedContainerName, (methodInfo.Name, receiverTypeName));
                }
            }

            return results;
        }

        // Create filter for extension methods from metadata
        private static MultiDictionary<string, (string, string)> CreateAggregatedFilter(ImmutableArray<string> receiverTypeNames, SymbolTreeInfo symbolInfo)
        {
            var results = new MultiDictionary<string, (string, string)>();

            foreach (var receiverTypeName in receiverTypeNames)
            {
                var methodInfos = symbolInfo.GetExtensionMethodInfoForReceiverType(receiverTypeName);
                if (methodInfos.Count == 0)
                {
                    continue;
                }

                foreach (var methodInfo in methodInfos)
                {
                    results.Add(methodInfo.FullyQualifiedContainerName, (methodInfo.Name, receiverTypeName));
                }
            }

            return results;
        }

        private readonly struct GetIndicesResult
        {
            public bool HasResult { get; }
            public Dictionary<Project, CacheEntry>? SyntaxIndices { get; }
            public Dictionary<PortableExecutableReference, SymbolTreeInfo>? SymbolInfos { get; }

            public GetIndicesResult(Dictionary<Project, CacheEntry> syntaxIndices, Dictionary<PortableExecutableReference, SymbolTreeInfo> symbolInfos)
            {
                HasResult = true;
                SyntaxIndices = syntaxIndices;
                SymbolInfos = symbolInfos;
            }

            public static GetIndicesResult NoneResult => default;
        }
    }

    internal sealed class StatisticCounter
    {
        public bool NoFilter;
        public int TotalTicks;
        public int TotalExtensionMethodsProvided;
        public int GetFilterTicks;
        public int GetSymbolTicks;
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
