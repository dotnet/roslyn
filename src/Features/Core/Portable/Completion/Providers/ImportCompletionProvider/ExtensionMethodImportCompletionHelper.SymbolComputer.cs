// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static partial class ExtensionMethodImportCompletionHelper
    {
        private class SymbolComputer
        {
            private readonly int _position;
            private readonly Document _originatingDocument;
            private readonly SemanticModel _originatingSemanticModel;
            private readonly ITypeSymbol _receiverTypeSymbol;
            private readonly ImmutableArray<string> _receiverTypeNames;
            private readonly ISet<string> _namespaceInScope;
            private readonly IImportCompletionCacheService<ExtensionMethodImportCompletionCacheEntry, object> _cacheService;

            // This dictionary is used as cache among all projects and PE references. 
            // The key is the receiver type as in the extension method declaration (symbol retrived from originating compilation).
            // The value indicates if we can reduce an extension method with this receiver type given receiver type.
            private readonly ConcurrentDictionary<ITypeSymbol, bool> _checkedReceiverTypes = new();

            public SymbolComputer(
                Document document,
                SemanticModel semanticModel,
                ITypeSymbol receiverTypeSymbol,
                int position,
                ISet<string> namespaceInScope)
            {
                _originatingDocument = document;
                _originatingSemanticModel = semanticModel;
                _receiverTypeSymbol = receiverTypeSymbol;
                _position = position;
                _namespaceInScope = namespaceInScope;

                var receiverTypeNames = GetReceiverTypeNames(receiverTypeSymbol);
                _receiverTypeNames = AddComplexTypes(receiverTypeNames);
                _cacheService = GetCacheService(document.Project);
            }

            public static async Task<SymbolComputer> CreateAsync(
                Document document,
                int position,
                ITypeSymbol receiverTypeSymbol,
                ISet<string> namespaceInScope,
                CancellationToken cancellationToken)
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                return new SymbolComputer(
                    document, semanticModel, receiverTypeSymbol, position, namespaceInScope);
            }

            private static IImportCompletionCacheService<ExtensionMethodImportCompletionCacheEntry, object> GetCacheService(Project project)
                => project.Solution.Workspace.Services.GetRequiredService<IImportCompletionCacheService<ExtensionMethodImportCompletionCacheEntry, object>>();

            private static string? GetPEReferenceCacheKey(PortableExecutableReference peReference)
                => peReference.FilePath ?? peReference.Display;

            /// <summary>
            /// Force create/update all relevant indices
            /// </summary>
            public static void QueueCacheWarmUpTask(Project project)
            {
                GetCacheService(project).WorkQueue.AddWork(project);
            }

            public static async ValueTask UpdateCacheAsync(Project project, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cacheService = GetCacheService(project);

                foreach (var relevantProject in GetAllRelevantProjects(project))
                    await GetUpToDateCacheEntryAsync(relevantProject, cacheService, cancellationToken).ConfigureAwait(false);

                foreach (var peReference in GetAllRelevantPeReferences(project))
                    await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(project.Solution, peReference, loadOnly: false, cancellationToken).ConfigureAwait(false);
            }

            public async Task<(ImmutableArray<IMethodSymbol> symbols, bool isPartialResult)> GetExtensionMethodSymbolsAsync(bool forceCacheCreation, bool hideAdvancedMembers, CancellationToken cancellationToken)
            {
                try
                {
                    // Find applicable symbols in parallel
                    using var _1 = ArrayBuilder<Task<ImmutableArray<IMethodSymbol>?>>.GetInstance(out var tasks);

                    foreach (var peReference in GetAllRelevantPeReferences(_originatingDocument.Project))
                    {
                        tasks.Add(Task.Run(() => GetExtensionMethodSymbolsFromPeReferenceAsync(
                            peReference,
                            forceCacheCreation,
                            cancellationToken).AsTask(), cancellationToken));
                    }

                    foreach (var project in GetAllRelevantProjects(_originatingDocument.Project))
                    {
                        tasks.Add(Task.Run(() => GetExtensionMethodSymbolsFromProjectAsync(
                            project,
                            forceCacheCreation,
                            cancellationToken), cancellationToken));
                    }

                    using var _2 = ArrayBuilder<IMethodSymbol>.GetInstance(out var symbols);
                    var isPartialResult = false;

                    var results = await Task.WhenAll(tasks).ConfigureAwait(false);

                    foreach (var result in results)
                    {
                        // `null` indicates we don't have the index ready for the corresponding project/PE.
                        // returns what we have even it means we only show partial results.
                        if (result == null)
                        {
                            isPartialResult = true;
                            continue;
                        }

                        symbols.AddRange(result);
                    }

                    var browsableSymbols = symbols.ToImmutable()
                        .FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, _originatingSemanticModel.Compilation);

                    return (browsableSymbols, isPartialResult);
                }
                finally
                {
                    // If we are not force creating/updating the cache, an update task needs to be queued in background.
                    if (!forceCacheCreation)
                        GetCacheService(_originatingDocument.Project).WorkQueue.AddWork(_originatingDocument.Project);
                }
            }

            // Returns all referenced projects and originating project itself.
            private static ImmutableArray<Project> GetAllRelevantProjects(Project project)
            {
                var graph = project.Solution.GetProjectDependencyGraph();
                var relevantProjectIds = graph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id).Concat(project.Id);
                return relevantProjectIds.Select(id => project.Solution.GetRequiredProject(id)).Where(p => p.SupportsCompilation).ToImmutableArray();
            }

            // Returns all PEs referenced by originating project.
            private static ImmutableArray<PortableExecutableReference> GetAllRelevantPeReferences(Project project)
                => project.MetadataReferences.OfType<PortableExecutableReference>().ToImmutableArray();

            private async Task<ImmutableArray<IMethodSymbol>?> GetExtensionMethodSymbolsFromProjectAsync(
                Project project,
                bool forceCacheCreation,
                CancellationToken cancellationToken)
            {
                ExtensionMethodImportCompletionCacheEntry cacheEntry;
                if (forceCacheCreation)
                {
                    cacheEntry = await GetUpToDateCacheEntryAsync(project, _cacheService, cancellationToken).ConfigureAwait(false);
                }
                else if (!_cacheService.ProjectItemsCache.TryGetValue(project.Id, out cacheEntry))
                {
                    // Use cached data if available, even checksum doesn't match. otherwise, returns null indicating cache not ready.
                    return null;
                }

                if (!cacheEntry.ContainsExtensionMethod)
                {
                    return ImmutableArray<IMethodSymbol>.Empty;
                }

                var originatingAssembly = _originatingSemanticModel.Compilation.Assembly;
                var filter = CreateAggregatedFilter(cacheEntry);
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var assembly = compilation.Assembly;
                var internalsVisible = originatingAssembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

                var matchingMethodSymbols = GetPotentialMatchingSymbolsFromAssembly(
                    compilation.Assembly, filter, internalsVisible, cancellationToken);

                return project == _originatingDocument.Project
                    ? GetExtensionMethodsForSymbolsFromSameCompilation(matchingMethodSymbols, cancellationToken)
                    : GetExtensionMethodsForSymbolsFromDifferentCompilation(matchingMethodSymbols, cancellationToken);
            }

            private async ValueTask<ImmutableArray<IMethodSymbol>?> GetExtensionMethodSymbolsFromPeReferenceAsync(
                PortableExecutableReference peReference,
                bool forceCacheCreation,
                CancellationToken cancellationToken)
            {
                SymbolTreeInfo? symbolInfo;
                if (forceCacheCreation)
                {
                    symbolInfo = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                        _originatingDocument.Project.Solution, peReference, loadOnly: false, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var cachedInfoTask = SymbolTreeInfo.TryGetCachedInfoForMetadataReferenceIgnoreChecksumAsync(peReference, cancellationToken);
                    if (cachedInfoTask.IsCompleted)
                    {
                        // Use cached data if available, even checksum doesn't match. We will update the cache in the background.
                        symbolInfo = await cachedInfoTask.ConfigureAwait(false);
                    }
                    else
                    {
                        // No cached data immediately available, returns null to indicate index not ready
                        return null;
                    }
                }

                if (symbolInfo is null ||
                    !symbolInfo.ContainsExtensionMethod ||
                    _originatingSemanticModel.Compilation.GetAssemblyOrModuleSymbol(peReference) is not IAssemblySymbol assembly)
                {
                    return ImmutableArray<IMethodSymbol>.Empty;
                }

                var filter = CreateAggregatedFilter(symbolInfo);
                var internalsVisible = _originatingSemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

                var matchingMethodSymbols = GetPotentialMatchingSymbolsFromAssembly(assembly, filter, internalsVisible, cancellationToken);

                return GetExtensionMethodsForSymbolsFromSameCompilation(matchingMethodSymbols, cancellationToken);
            }

            private ImmutableArray<IMethodSymbol> GetExtensionMethodsForSymbolsFromDifferentCompilation(
                MultiDictionary<ITypeSymbol, IMethodSymbol> matchingMethodSymbols,
                CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var builder);

                // Matching extension method symbols are grouped based on their receiver type.
                foreach (var (declaredReceiverType, methodSymbols) in matchingMethodSymbols)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var declaredReceiverTypeInOriginatingCompilation = SymbolFinder.FindSimilarSymbols(declaredReceiverType, _originatingSemanticModel.Compilation, cancellationToken).FirstOrDefault();
                    if (declaredReceiverTypeInOriginatingCompilation == null)
                    {
                        // Bug: https://github.com/dotnet/roslyn/issues/45404
                        // SymbolFinder.FindSimilarSymbols would fail if originating and referenced compilation targeting different frameworks say net472 and netstandard respectively.
                        // Here's SymbolKey for System.String from those two framework as an example:
                        //
                        //  {1 (D "String" (N "System" 0 (N "" 0 (U (S "netstandard" 4) 3) 2) 1) 0 0 (% 0) 0)}
                        //  {1 (D "String" (N "System" 0 (N "" 0 (U (S "mscorlib" 4) 3) 2) 1) 0 0 (% 0) 0)}
                        //
                        // Also we don't use the "ignoreAssemblyKey" option for SymbolKey resolution because its perfermance doesn't meet our requirement.
                        continue;
                    }

                    if (_checkedReceiverTypes.TryGetValue(declaredReceiverTypeInOriginatingCompilation, out var cachedResult) && !cachedResult)
                    {
                        // If we already checked an extension method with same receiver type before, and we know it can't be applied
                        // to the receiverTypeSymbol, then no need to proceed methods from this group..
                        continue;
                    }

                    // This is also affected by the symbol resolving issue mentioned above, which means in case referenced projects
                    // are targeting different framework, we will miss extension methods with any framework type in their signature from those projects.
                    var isFirstMethod = true;
                    foreach (var methodInOriginatingCompilation in methodSymbols.Select(s => SymbolFinder.FindSimilarSymbols(s, _originatingSemanticModel.Compilation).FirstOrDefault()).WhereNotNull())
                    {
                        if (isFirstMethod)
                        {
                            isFirstMethod = false;

                            // We haven't seen this receiver type yet. Try to check by reducing one extension method
                            // to the given receiver type and save the result.
                            if (!cachedResult)
                            {
                                // If this is the first symbol we retrived from originating compilation,
                                // try to check if we can apply it to given receiver type, and save result to our cache.
                                // Since method symbols are grouped by their declared receiver type, they are either all matches to the receiver type
                                // or all mismatches. So we only need to call ReduceExtensionMethod on one of them.
                                var reducedMethodSymbol = methodInOriginatingCompilation.ReduceExtensionMethod(_receiverTypeSymbol);
                                cachedResult = reducedMethodSymbol != null;
                                _checkedReceiverTypes[declaredReceiverTypeInOriginatingCompilation] = cachedResult;

                                // Now, cachedResult being false means method doesn't match the receiver type,
                                // stop processing methods from this group.
                                if (!cachedResult)
                                {
                                    break;
                                }
                            }
                        }

                        if (_originatingSemanticModel.IsAccessible(_position, methodInOriginatingCompilation))
                        {
                            builder.Add(methodInOriginatingCompilation);
                        }
                    }
                }

                return builder.ToImmutable();
            }

            private ImmutableArray<IMethodSymbol> GetExtensionMethodsForSymbolsFromSameCompilation(
                MultiDictionary<ITypeSymbol, IMethodSymbol> matchingMethodSymbols,
                CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var builder);

                // Matching extension method symbols are grouped based on their receiver type.
                foreach (var (receiverType, methodSymbols) in matchingMethodSymbols)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // If we already checked an extension method with same receiver type before, and we know it can't be applied
                    // to the receiverTypeSymbol, then no need to proceed further.
                    if (_checkedReceiverTypes.TryGetValue(receiverType, out var cachedResult) && !cachedResult)
                    {
                        continue;
                    }

                    // We haven't seen this type yet. Try to check by reducing one extension method
                    // to the given receiver type and save the result.
                    if (!cachedResult)
                    {
                        var reducedMethodSymbol = methodSymbols.First().ReduceExtensionMethod(_receiverTypeSymbol);
                        cachedResult = reducedMethodSymbol != null;
                        _checkedReceiverTypes[receiverType] = cachedResult;
                    }

                    // Receiver type matches the receiver type of the extension method declaration.
                    // We can add accessible ones to the item builder.
                    if (cachedResult)
                    {
                        foreach (var methodSymbol in methodSymbols)
                        {
                            if (_originatingSemanticModel.IsAccessible(_position, methodSymbol))
                            {
                                builder.Add(methodSymbol);
                            }
                        }
                    }
                }

                return builder.ToImmutable();
            }

            private MultiDictionary<ITypeSymbol, IMethodSymbol> GetPotentialMatchingSymbolsFromAssembly(
                IAssemblySymbol assembly,
                MultiDictionary<string, (string methodName, string receiverTypeName)> extensionMethodFilter,
                bool internalsVisible,
                CancellationToken cancellationToken)
            {
                var builder = new MultiDictionary<ITypeSymbol, IMethodSymbol>();

                // The filter contains all the extension methods that potentially match the receiver type.
                // We use it as a guide to selectively retrive container and member symbols from the assembly.
                foreach (var (fullyQualifiedContainerName, methodInfo) in extensionMethodFilter)
                {
                    // First try to filter out types from already imported namespaces
                    var indexOfLastDot = fullyQualifiedContainerName.LastIndexOf('.');
                    var qualifiedNamespaceName = indexOfLastDot > 0 ? fullyQualifiedContainerName.Substring(0, indexOfLastDot) : string.Empty;

                    if (_namespaceInScope.Contains(qualifiedNamespaceName))
                    {
                        continue;
                    }

                    // Container of extension method (static class in C# and Module in VB) can't be generic or nested.
                    var containerSymbol = assembly.GetTypeByMetadataName(fullyQualifiedContainerName);

                    if (containerSymbol == null
                        || !containerSymbol.MightContainExtensionMethods
                        || !IsAccessible(containerSymbol, internalsVisible))
                    {
                        continue;
                    }

                    // Now we have the container symbol, first try to get member extension method symbols that matches our syntactic filter,
                    // then further check if those symbols matches semantically.
                    foreach (var (methodName, receiverTypeName) in methodInfo)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var methodSymbols = containerSymbol.GetMembers(methodName).OfType<IMethodSymbol>();

                        foreach (var methodSymbol in methodSymbols)
                        {
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

            /// <summary>
            /// Create a filter for extension methods from source.
            /// The filter is a map from fully qualified type name to info of extension methods it contains.
            /// </summary>
            private MultiDictionary<string, (string methodName, string receiverTypeName)> CreateAggregatedFilter(ExtensionMethodImportCompletionCacheEntry syntaxIndex)
            {
                var results = new MultiDictionary<string, (string, string)>();

                foreach (var receiverTypeName in _receiverTypeNames)
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

            /// <summary>
            /// Create filter for extension methods from metadata
            /// The filter is a map from fully qualified type name to info of extension methods it contains.
            /// </summary>
            private MultiDictionary<string, (string methodName, string receiverTypeName)> CreateAggregatedFilter(SymbolTreeInfo symbolInfo)
            {
                var results = new MultiDictionary<string, (string, string)>();

                foreach (var receiverTypeName in _receiverTypeNames)
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

            /// <summary>
            /// Add strings represent complex types (i.e. "" for non-array types and "[]" for array types) to the receiver type, 
            /// so we would include in the filter info about extension methods with complex receiver type.
            /// </summary>
            private static ImmutableArray<string> AddComplexTypes(ImmutableArray<string> receiverTypeNames)
            {
                using var _ = ArrayBuilder<string>.GetInstance(receiverTypeNames.Length + 2, out var receiverTypeNamesBuilder);
                receiverTypeNamesBuilder.AddRange(receiverTypeNames);
                receiverTypeNamesBuilder.Add(FindSymbols.Extensions.ComplexReceiverTypeName);
                receiverTypeNamesBuilder.Add(FindSymbols.Extensions.ComplexArrayReceiverTypeName);

                return receiverTypeNamesBuilder.ToImmutable();
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
        }
    }
}
