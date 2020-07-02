// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    return namedType.MetadataName;

                case IArrayTypeSymbol arrayType:
                    var elementType = arrayType.ElementType;
                    while (elementType is IArrayTypeSymbol symbol)
                    {
                        elementType = symbol.ElementType;
                    }

                    var elementTypeName = GetReceiverTypeName(elementType);

                    // We do not differentiate array of different kinds sicne they are all represented in the indices as "NonArrayElementTypeName[]"
                    // e.g. int[], int[][], int[,], etc. are all represented as "int[]", whereas array of complex type such as T[] is "[]".
                    return elementTypeName + FindSymbols.Extensions.ArrayReceiverTypeNameSuffix;

                default:
                    // Complex types are represented by "";
                    return FindSymbols.Extensions.ComplexReceiverTypeName;
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

            // This dictionary is used as cache among all projects and PE references. 
            // The key is the receiver type as in the extension method declaration (symbol retrived from current compilation).
            // The value indicates if we can reduce an extension method with this receiver type given receiver type.
            using var _3 = PooledDictionary<ITypeSymbol, bool>.GetInstance(out var checkedReceiverTypes);

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

                if (project == currentProject)
                {
                    GetExtensionMethodItemsForSymbolsFromSameCompilation(
                        position, semanticModel, receiverTypeSymbol, matchingMethodSymbols,
                        completionItemsbuilder, namespaceNameCache, checkedReceiverTypes, cancellationToken);
                }
                else
                {
                    GetExtensionMethodItemsForSymbolsFromDifferentCompilation(
                        position, semanticModel, receiverTypeSymbol, matchingMethodSymbols,
                        completionItemsbuilder, namespaceNameCache, checkedReceiverTypes, cancellationToken);
                }
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

                    GetExtensionMethodItemsForSymbolsFromSameCompilation(
                        position, semanticModel, receiverTypeSymbol, matchingMethodSymbols,
                        completionItemsbuilder, namespaceNameCache,
                        checkedReceiverTypes, cancellationToken);
                }
            }

            return completionItemsbuilder.ToImmutable();

            // Add strings represent complex types (i.e. "" for non-array types and "[]" for array types) to the receiver type, 
            // so we would include in the filter info about extension methods with complex receiver type.
            static ImmutableArray<string> AttachComplexTypes(ImmutableArray<string> receiverTypeNames)
            {
                using var _ = ArrayBuilder<string>.GetInstance(receiverTypeNames.Length + 2, out var receiverTypeNamesBuilder);
                receiverTypeNamesBuilder.AddRange(receiverTypeNames);
                receiverTypeNamesBuilder.Add(FindSymbols.Extensions.ComplexReceiverTypeName);
                receiverTypeNamesBuilder.Add(FindSymbols.Extensions.ComplexArrayReceiverTypeName);

                return receiverTypeNamesBuilder.ToImmutable();
            }
        }

        private static void GetExtensionMethodItemsForSymbolsFromDifferentCompilation(
            int position,
            SemanticModel semanticModel,
            ITypeSymbol receiverTypeSymbol,
            MultiDictionary<ITypeSymbol, IMethodSymbol> matchingMethodSymbols,
            ArrayBuilder<SerializableImportCompletionItem> builder,
            Dictionary<INamespaceSymbol, string> stringCache,
            Dictionary<ITypeSymbol, bool> checkedReceiverTypes,
            CancellationToken cancellationToken)
        {
            // Matching extension method symbols are grouped based on their receiver type.
            foreach (var (declaredReceiverType, methodSymbols) in matchingMethodSymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var declaredReceiverTypeInCurrentCompilation = SymbolFinder.FindSimilarSymbols(declaredReceiverType, semanticModel.Compilation).FirstOrDefault();
                if (declaredReceiverTypeInCurrentCompilation == null)
                {
                    // Bug: https://github.com/dotnet/roslyn/issues/45404
                    // SymbolFinder.FindSimilarSymbols would fail if current and referenced compilation targeting different frameworks say net472 and netstandard respectively.
                    // Here's SymbolKey for System.String from those two framework as an example:
                    //
                    //  {1 (D "String" (N "System" 0 (N "" 0 (U (S "netstandard" 4) 3) 2) 1) 0 0 (% 0) 0)}
                    //  {1 (D "String" (N "System" 0 (N "" 0 (U (S "mscorlib" 4) 3) 2) 1) 0 0 (% 0) 0)}
                    //
                    // Also we don't use the "ignoreAssemblyKey" option for SymbolKey resolution because its perfermance doesn't meet our requirement.
                    continue;
                }

                if (checkedReceiverTypes.TryGetValue(declaredReceiverTypeInCurrentCompilation, out var cachedResult) && !cachedResult)
                {
                    // If we already checked an extension method with same receiver type before, and we know it can't be applied
                    // to the receiverTypeSymbol, then no need to proceed methods from this group..
                    continue;
                }

                // This is also affected by the symbol resolving issue mentioned above, which means in case referenced projects
                // are targeting different framework, we will miss extension methods with any framework type in their signature from those projects.
                var isFirstMethod = true;
                foreach (var methodInCurrentCompilation in methodSymbols.Select(s => SymbolFinder.FindSimilarSymbols(s, semanticModel.Compilation).FirstOrDefault()).WhereNotNull())
                {
                    if (isFirstMethod)
                    {
                        isFirstMethod = false;

                        // We haven't seen this receiver type yet. Try to check by reducing one extension method
                        // to the given receiver type and save the result.
                        if (!cachedResult)
                        {
                            // If this is the first symbol we retrived from current compilation,
                            // try to check if we can apply it to given receiver type, and save result to our cache.
                            // Since method symbols are grouped by their declared receiver type, they are either all matches to the receiver type
                            // or all mismatches. So we only need to call ReduceExtensionMethod on one of them.
                            var reducedMethodSymbol = methodInCurrentCompilation.ReduceExtensionMethod(receiverTypeSymbol);
                            cachedResult = reducedMethodSymbol != null;
                            checkedReceiverTypes[declaredReceiverTypeInCurrentCompilation] = cachedResult;

                            // Now, cachedResult being false means method doesn't match the receiver type,
                            // stop processing methods from this group.
                            if (!cachedResult)
                            {
                                break;
                            }
                        }
                    }

                    if (semanticModel.IsAccessible(position, methodInCurrentCompilation))
                    {
                        CreateAndAddItem(methodInCurrentCompilation, builder, stringCache);
                    }
                }
            }
        }

        private static void GetExtensionMethodItemsForSymbolsFromSameCompilation(
            int position,
            SemanticModel semanticModel,
            ITypeSymbol receiverTypeSymbol,
            MultiDictionary<ITypeSymbol, IMethodSymbol> matchingMethodSymbols,
            ArrayBuilder<SerializableImportCompletionItem> builder,
            Dictionary<INamespaceSymbol, string> stringCache,
            Dictionary<ITypeSymbol, bool> checkedReceiverTypes,
            CancellationToken cancellationToken)
        {
            // Matching extension method symbols are grouped based on their receiver type.
            foreach (var (receiverType, methodSymbols) in matchingMethodSymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // If we already checked an extension method with same receiver type before, and we know it can't be applied
                // to the receiverTypeSymbol, then no need to proceed further.
                if (checkedReceiverTypes.TryGetValue(receiverType, out var cachedResult) && !cachedResult)
                {
                    continue;
                }

                // We haven't seen this type yet. Try to check by reducing one extension method
                // to the given receiver type and save the result.
                if (!cachedResult)
                {
                    var reducedMethodSymbol = methodSymbols.First().ReduceExtensionMethod(receiverTypeSymbol);
                    cachedResult = reducedMethodSymbol != null;
                    checkedReceiverTypes[receiverType] = cachedResult;
                }

                // Receiver type matches the receiver type of the extension method declaration.
                // We can add accessible ones to the item builder.
                if (cachedResult)
                {
                    foreach (var methodSymbol in methodSymbols)
                    {
                        if (semanticModel.IsAccessible(position, methodSymbol))
                        {
                            CreateAndAddItem(methodSymbol, builder, stringCache);
                        }
                    }
                }
            }
        }

        private static void CreateAndAddItem(IMethodSymbol methodSymbol, ArrayBuilder<SerializableImportCompletionItem> builder, Dictionary<INamespaceSymbol, string> stringCache)
            => builder.Add(new SerializableImportCompletionItem(
                    SymbolKey.CreateString(methodSymbol),
                    methodSymbol.Name,
                    methodSymbol.Arity,
                    methodSymbol.GetGlyph(),
                    GetFullyQualifiedNamespaceName(methodSymbol.ContainingNamespace, stringCache)));

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

        private static MultiDictionary<ITypeSymbol, IMethodSymbol> GetPotentialMatchingSymbolsFromAssembly(
            IAssemblySymbol assembly,
            MultiDictionary<string, (string methodName, string receiverTypeName)> extensionMethodFilter,
            ISet<string> namespaceFilter,
            bool internalsVisible,
            StatisticCounter counter,
            CancellationToken cancellationToken)
        {
            var builder = new MultiDictionary<ITypeSymbol, IMethodSymbol>();

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

                        if (MatchExtensionMethod(methodSymbol, receiverTypeName, internalsVisible, out var receiverType))
                        {
                            // Find a potential match.
                            builder.Add(receiverType!, methodSymbol);
                        }
                    }
                }
            }

            return builder;

            static bool MatchExtensionMethod(IMethodSymbol method, string filterReceiverTypeName, bool internalsVisible, out ITypeSymbol? receiverType)
            {
                receiverType = null;
                if (!method.IsExtensionMethod || method.Parameters.IsEmpty || !IsAccessible(method, internalsVisible))
                {
                    return false;
                }

                // We get a match if the receiver type name match. 
                // For complex type, we would check if it matches with filter on whether it's an array.
                if (filterReceiverTypeName.Length > 0 && !string.Equals(filterReceiverTypeName, GetReceiverTypeName(method.Parameters[0].Type), StringComparison.Ordinal))
                {
                    return false;
                }

                receiverType = method.Parameters[0].Type;
                return true;
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
        private static MultiDictionary<string, (string methodName, string receiverTypeName)> CreateAggregatedFilter(ImmutableArray<string> receiverTypeNames, CacheEntry syntaxIndex)
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
        private static MultiDictionary<string, (string methodName, string receiverTypeName)> CreateAggregatedFilter(ImmutableArray<string> receiverTypeNames, SymbolTreeInfo symbolInfo)
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
