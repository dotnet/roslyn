// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal static partial class ExtensionMemberImportCompletionHelper
{
    private sealed partial class SymbolComputer
    {
        private readonly int _position;
        private readonly Document _originatingDocument;

        /// <summary>
        /// The semantic model provided for <see cref="_originatingDocument"/>. This may be a speculative semantic
        /// model with limited validity based on the context surrounding <see cref="_position"/>.
        /// </summary>
        private readonly SemanticModel _originatingSemanticModel;
        private readonly ITypeSymbol _receiverTypeSymbol;
        private readonly ImmutableArray<string> _receiverTypeNames;
        private readonly ISet<string> _namespaceInScope;

        // This dictionary is used as cache among all projects and PE references. The key is the receiver type as in the
        // extension member declaration (symbol retrieved from originating compilation). The value indicates if we can
        // reduce an extension member with this receiver type given receiver type.
        private readonly ConcurrentDictionary<ITypeSymbol, bool> _checkedReceiverTypes = [];

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
        }

        private static IImportCompletionCacheService<ExtensionMemberImportCompletionCacheEntry, object> GetCacheService(Project project)
            => project.Solution.Services.GetRequiredService<IImportCompletionCacheService<ExtensionMemberImportCompletionCacheEntry, object>>();

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

            foreach (var relevantProject in GetAllRelevantProjects(project))
                await GetUpToDateCacheEntryAsync(relevantProject, cancellationToken).ConfigureAwait(false);

            foreach (var peReference in GetAllRelevantPeReferences(project))
                await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(project.Solution, peReference, checksum: null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<ISymbol>> GetExtensionMemberSymbolsAsync(
            bool forceCacheCreation, bool hideAdvancedMembers, bool isStatic, CancellationToken cancellationToken)
        {
            try
            {
                // Find applicable symbols in parallel
                var peReferenceMemberSymbolsTask = ProducerConsumer<ISymbol?>.RunParallelAsync(
                    source: GetAllRelevantPeReferences(_originatingDocument.Project),
                    produceItems: static (peReference, callback, args, cancellationToken) =>
                        args.@this.GetExtensionMemberSymbolsFromPeReferenceAsync(peReference, callback, args.forceCacheCreation, cancellationToken),
                    args: (@this: this, forceCacheCreation),
                    cancellationToken);

                var projectMemberSymbolsTask = ProducerConsumer<ISymbol?>.RunParallelAsync(
                    source: GetAllRelevantProjects(_originatingDocument.Project),
                    produceItems: static (project, callback, args, cancellationToken) =>
                        args.@this.GetExtensionMemberSymbolsFromProjectAsync(project, callback, args.forceCacheCreation, cancellationToken),
                    args: (@this: this, forceCacheCreation),
                    cancellationToken);

                var results = await Task.WhenAll(peReferenceMemberSymbolsTask, projectMemberSymbolsTask).ConfigureAwait(false);

                using var _ = ArrayBuilder<ISymbol>.GetInstance(results[0].Length + results[1].Length, out var symbols);
                foreach (var memberArray in results)
                {
                    foreach (var member in memberArray)
                    {
                        if (MatchesStatic(member, isStatic))
                            symbols.Add(member);
                    }
                }

                var browsableSymbols = symbols
                    .ToImmutable()
                    .FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, _originatingSemanticModel.Compilation, inclusionFilter: static s => true);

                return browsableSymbols;
            }
            finally
            {
                // If we are not force creating/updating the cache, an update task needs to be queued in background.
                if (!forceCacheCreation)
                    GetCacheService(_originatingDocument.Project).WorkQueue.AddWork(_originatingDocument.Project);
            }

            static bool MatchesStatic([NotNullWhen(true)] ISymbol? symbol, bool isStatic)
            {
                if (symbol is null)
                    return false;

                if (symbol is IPropertySymbol propertySymbol)
                    return propertySymbol.IsStatic == isStatic;

                if (symbol is IMethodSymbol method)
                {
                    // Classic Extension methods are always instance methods.
                    if (method.IsExtensionMethod)
                        return !isStatic;

                    // Modern extension methods can be static or instance methods.
                    return method.IsStatic == isStatic;
                }

                throw ExceptionUtilities.UnexpectedValue(symbol.GetType());
            }
        }

        // Returns all referenced projects and originating project itself.
        private static ImmutableArray<Project> GetAllRelevantProjects(Project project)
        {
            var graph = project.Solution.GetProjectDependencyGraph();
            var relevantProjectIds = graph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id).Concat(project.Id);
            return [.. relevantProjectIds.Select(project.Solution.GetRequiredProject).Where(p => p.SupportsCompilation)];
        }

        // Returns all PEs referenced by originating project.
        private static ImmutableArray<PortableExecutableReference> GetAllRelevantPeReferences(Project project)
            => [.. project.MetadataReferences.OfType<PortableExecutableReference>()];

        private async Task GetExtensionMemberSymbolsFromProjectAsync(
            Project project,
            Action<ISymbol?> callback,
            bool forceCacheCreation,
            CancellationToken cancellationToken)
        {
            ExtensionMemberImportCompletionCacheEntry? cacheEntry;
            if (forceCacheCreation)
            {
                cacheEntry = await GetUpToDateCacheEntryAsync(project, cancellationToken).ConfigureAwait(false);
            }
            else if (!s_projectItemsCache.TryGetValue(project.Id, out cacheEntry))
            {
                // Use cached data if available, even checksum doesn't match. otherwise, returns null indicating cache not ready.
                callback(null);
                return;
            }

            if (!cacheEntry.ContainsExtensionMember)
                return;

            var originatingAssembly = _originatingSemanticModel.Compilation.Assembly;
            var filter = CreateAggregatedFilter(cacheEntry);

            // Avoid recalculating a compilation for the originating document, particularly for the case where the
            // provided semantic model is a speculative semantic model.
            var compilation = project == _originatingDocument.Project
                ? _originatingSemanticModel.Compilation
                : await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var assembly = compilation.Assembly;
            var internalsVisible = originatingAssembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

            var matchingMemberSymbols = GetPotentialMatchingSymbolsFromAssembly(
                compilation.Assembly, filter, internalsVisible, cancellationToken);

            if (project == _originatingDocument.Project)
            {
                GetExtensionMembersForSymbolsFromSameCompilation(matchingMemberSymbols, callback, cancellationToken);
            }
            else
            {
                GetExtensionMembersForSymbolsFromDifferentCompilation(matchingMemberSymbols, callback, cancellationToken);
            }
        }

        private async Task GetExtensionMemberSymbolsFromPeReferenceAsync(
            PortableExecutableReference peReference,
            Action<ISymbol?> callback,
            bool forceCacheCreation,
            CancellationToken cancellationToken)
        {
            SymbolTreeInfo? symbolInfo;
            if (forceCacheCreation)
            {
                symbolInfo = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                    _originatingDocument.Project.Solution, peReference, checksum: null, cancellationToken).ConfigureAwait(false);
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
                    callback(null);
                    return;
                }
            }

            if (symbolInfo is null ||
                !symbolInfo.ContainsExtensionMember ||
                _originatingSemanticModel.Compilation.GetAssemblyOrModuleSymbol(peReference) is not IAssemblySymbol assembly)
            {
                return;
            }

            var filter = CreateAggregatedFilter(symbolInfo);
            var internalsVisible = _originatingSemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

            var matchingMemberSymbols = GetPotentialMatchingSymbolsFromAssembly(assembly, filter, internalsVisible, cancellationToken);

            GetExtensionMembersForSymbolsFromSameCompilation(matchingMemberSymbols, callback, cancellationToken);
        }

        private void GetExtensionMembersForSymbolsFromDifferentCompilation(
            MultiDictionary<ITypeSymbol, ISymbol> matchingMemberSymbols,
            Action<ISymbol?> callback,
            CancellationToken cancellationToken)
        {
            // Matching extension member symbols are grouped based on their receiver type.
            foreach (var (declaredReceiverType, memberSymbols) in matchingMemberSymbols)
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
                    // If we already checked an extension member with same receiver type before, and we know it can't be applied
                    // to the receiverTypeSymbol, then no need to proceed members from this group..
                    continue;
                }

                // This is also affected by the symbol resolving issue mentioned above, which means in case referenced projects
                // are targeting different framework, we will miss extension members with any framework type in their signature from those projects.
                var isFirstMember = true;
                foreach (var memberInOriginatingCompilation in memberSymbols.Select(s => SymbolFinder.FindSimilarSymbols(s, _originatingSemanticModel.Compilation).FirstOrDefault()).WhereNotNull())
                {
                    if (isFirstMember)
                    {
                        isFirstMember = false;

                        // We haven't seen this receiver type yet. Try to check by reducing one extension member to the
                        // given receiver type and save the result.
                        if (!cachedResult)
                        {
                            // If this is the first symbol we retrieved from originating compilation, try to check if we
                            // can apply it to given receiver type, and save result to our cache. Since member symbols
                            // are grouped by their declared receiver type, they are either all matches to the receiver
                            // type or all mismatches. So we only need to call ReduceExtensionMember on one of them.
                            var reducedMemberSymbol = TryReduceExtensionMember(memberInOriginatingCompilation);
                            cachedResult = reducedMemberSymbol != null;
                            _checkedReceiverTypes[declaredReceiverTypeInOriginatingCompilation] = cachedResult;

                            // Now, cachedResult being false means member doesn't match the receiver type,
                            // stop processing members from this group.
                            if (!cachedResult)
                            {
                                break;
                            }
                        }
                    }

                    if (_originatingSemanticModel.IsAccessible(_position, memberInOriginatingCompilation))
                        callback(memberInOriginatingCompilation);
                }
            }

            ISymbol? TryReduceExtensionMember(ISymbol memberSymbol)
                => memberSymbol.ReduceExtensionMember(_receiverTypeSymbol) ??
                   (memberSymbol as IMethodSymbol)?.ReduceExtensionMethod(_receiverTypeSymbol);
        }

        private void GetExtensionMembersForSymbolsFromSameCompilation(
            MultiDictionary<ITypeSymbol, ISymbol> matchingMemberSymbols,
            Action<ISymbol?> callback,
            CancellationToken cancellationToken)
        {
            // Matching extension member symbols are grouped based on their receiver type.
            foreach (var (receiverType, memberSymbols) in matchingMemberSymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // If we already checked an extension member with same receiver type before, and we know it can't be applied
                // to the receiverTypeSymbol, then no need to proceed further.
                if (_checkedReceiverTypes.TryGetValue(receiverType, out var cachedResult) && !cachedResult)
                    continue;

                // We haven't seen this type yet. Try to check by reducing one extension member
                // to the given receiver type and save the result.
                if (!cachedResult)
                {
                    var reducedMemberSymbol = TryReduceExtensionMember(memberSymbols.First(), _receiverTypeSymbol);
                    cachedResult = reducedMemberSymbol != null;
                    _checkedReceiverTypes[receiverType] = cachedResult;
                }

                // Receiver type matches the receiver type of the extension member declaration.
                // We can add accessible ones to the item builder.
                if (cachedResult)
                {
                    foreach (var memberSymbol in memberSymbols)
                    {
                        if (_originatingSemanticModel.IsAccessible(_position, memberSymbol))
                            callback(memberSymbol);
                    }
                }
            }
        }

        private static ISymbol? TryReduceExtensionMember(ISymbol memberSymbol, ITypeSymbol receiverTypeSymbol)
        {
            // Try modern extension member first.
            var reduced = memberSymbol.ReduceExtensionMember(receiverTypeSymbol);
            if (reduced != null)
                return reduced;

            if (memberSymbol is IMethodSymbol methodSymbol)
            {
                // Then fall back to classic extension method reduction.

                // First defer to compiler to try to reduce this.
                reduced = methodSymbol.ReduceExtensionMethod(receiverTypeSymbol);
                if (reduced is null)
                    return null;

                // Compiler is sometimes lenient with reduction, especially in cases of generic.  Do another pass ourselves
                // to see if we should filter this out.
                if (methodSymbol.Parameters is [var extensionParameter, ..] &&
                    extensionParameter.Type is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } typeParameter)
                {
                    if (!CheckConstraints(receiverTypeSymbol, typeParameter))
                        return null;
                }

                return reduced;
            }

            return null;
        }

        private MultiDictionary<ITypeSymbol, ISymbol> GetPotentialMatchingSymbolsFromAssembly(
            IAssemblySymbol assembly,
            MultiDictionary<string, (string memberName, string receiverTypeName)> extensionMemberFilter,
            bool internalsVisible,
            CancellationToken cancellationToken)
        {
            var builder = new MultiDictionary<ITypeSymbol, ISymbol>();

            // The filter contains all the extension members that potentially match the receiver type.
            // We use it as a guide to selectively retrieve container and member symbols from the assembly.
            foreach (var (fullyQualifiedContainerName, memberInfo) in extensionMemberFilter)
            {
                var extensionDotIndex = Math.Max(
                    fullyQualifiedContainerName.LastIndexOf(".extension<"),
                    fullyQualifiedContainerName.LastIndexOf(".extension("));
                if (extensionDotIndex < 0)
                {
                    // Classic extension method. 

                    var extensionStaticClass = TryGetViableExtensionStaticClass(fullyQualifiedContainerName);

                    // Now we have the container symbol, first try to get member extension method symbols directive
                    // inside of it that matches our syntactic filter, then further check if those symbols matches
                    // semantically.
                    AddExtensionMembers(extensionStaticClass, examineExtensionGroups: false, memberInfo);
                }
                else
                {
                    // Modern extension member.

                    var extensionStaticClass = TryGetViableExtensionStaticClass(fullyQualifiedContainerName[..extensionDotIndex]);

                    // Now we have the container symbol, dive into the extension blocks within and try to get member
                    // extension member symbols that matches our syntactic filter, then further check if those symbols
                    // matches semantically.
                    AddExtensionMembers(extensionStaticClass, examineExtensionGroups: true, memberInfo);
                }
            }

            return builder;

            void AddExtensionMembers(
                INamedTypeSymbol? extensionStaticClass,
                bool examineExtensionGroups,
                MultiDictionary<string, (string memberName, string receiverTypeName)>.ValueSet memberInfo)
            {
                if (extensionStaticClass is null)
                    return;

                var typesToExamine = examineExtensionGroups
                    ? extensionStaticClass.GetTypeMembers().WhereAsArray(m => m.IsExtension)
                    : [extensionStaticClass];
                foreach (var (memberName, receiverTypeName) in memberInfo)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (var extensionType in typesToExamine)
                    {
                        foreach (var memberSymbol in extensionType.GetMembers(memberName))
                        {
                            if (MatchExtensionMember(memberSymbol, receiverTypeName, internalsVisible, out var receiverType))
                                builder.Add(receiverType, memberSymbol);
                        }
                    }
                }
            }

            INamedTypeSymbol? TryGetViableExtensionStaticClass(string staticClassName)
            {
                var indexOfLastDot = staticClassName.LastIndexOf('.');
                var qualifiedNamespaceName = indexOfLastDot > 0 ? staticClassName[..indexOfLastDot] : string.Empty;

                // First try to filter out types from already imported namespaces
                if (_namespaceInScope.Contains(qualifiedNamespaceName))
                    return null;

                // Container of extension method (static class in C# and Module in VB) can't be generic or nested.
                var containerSymbol = assembly.GetTypeByMetadataName(staticClassName);

                if (containerSymbol == null
                    || !containerSymbol.MightContainExtensionMethods
                    || !IsAccessible(containerSymbol, internalsVisible))
                {
                    return null;
                }

                return containerSymbol;
            }

            static bool MatchExtensionMember(
                ISymbol symbol,
                string filterReceiverTypeName,
                bool internalsVisible,
                [NotNullWhen(true)] out ITypeSymbol? receiverType)
            {
                receiverType = null;

                if (symbol.ContainingType.IsExtension && symbol is IPropertySymbol or IMethodSymbol)
                {
                    var extensionParameter = symbol.ContainingType.ExtensionParameter;
                    if (extensionParameter is null)
                        return false;

                    if (filterReceiverTypeName.Length > 0 && !string.Equals(filterReceiverTypeName, GetReceiverTypeName(extensionParameter.Type), StringComparison.Ordinal))
                        return false;

                    if (!IsAccessible(symbol, internalsVisible))
                        return false;

                    receiverType = extensionParameter.Type;
                    return true;
                }
                else if (symbol is IMethodSymbol { IsExtensionMethod: true, Parameters.Length: > 0 } method)
                {
                    if (!IsAccessible(method, internalsVisible))
                        return false;

                    // We get a match if the receiver type name match. 
                    // For complex type, we would check if it matches with filter on whether it's an array.
                    if (filterReceiverTypeName.Length > 0 && !string.Equals(filterReceiverTypeName, GetReceiverTypeName(method.Parameters[0].Type), StringComparison.Ordinal))
                        return false;

                    receiverType = method.Parameters[0].Type;
                    return true;
                }

                return false;
            }

            // An quick accessibility check based on declared accessibility only, a semantic based check is still required later.
            // Since we are dealing with extension methods and their container (top level static class and modules), only public,
            // internal and private modifiers are in play here. 
            // Also, this check is called for a method symbol only when the container was checked and is accessible.
            static bool IsAccessible(ISymbol symbol, bool internalsVisible)
                => symbol.DeclaredAccessibility == Accessibility.Public ||
                (symbol.DeclaredAccessibility == Accessibility.Internal && internalsVisible);
        }

        /// <summary>
        /// Create a filter for extension members from source.
        /// The filter is a map from fully qualified type name to info of extension members it contains.
        /// </summary>
        private MultiDictionary<string, (string memberName, string receiverTypeName)> CreateAggregatedFilter(ExtensionMemberImportCompletionCacheEntry syntaxIndex)
        {
            var results = new MultiDictionary<string, (string, string)>();

            foreach (var receiverTypeName in _receiverTypeNames)
            {
                var memberInfos = syntaxIndex.ReceiverTypeNameToExtensionMemberMap[receiverTypeName];
                if (memberInfos.Count == 0)
                    continue;

                foreach (var memberInfo in memberInfos)
                    results.Add(memberInfo.FullyQualifiedContainerName, (memberInfo.Name, receiverTypeName));
            }

            return results;
        }

        /// <summary>
        /// Create filter for extension members from metadata
        /// The filter is a map from fully qualified type name to info of extension members it contains.
        /// </summary>
        private MultiDictionary<string, (string memberName, string receiverTypeName)> CreateAggregatedFilter(SymbolTreeInfo symbolInfo)
        {
            var results = new MultiDictionary<string, (string, string)>();

            foreach (var receiverTypeName in _receiverTypeNames)
            {
                var memberInfos = symbolInfo.GetExtensionMemberInfoForReceiverType(receiverTypeName);
                if (memberInfos.Count == 0)
                    continue;

                foreach (var memberInfo in memberInfos)
                    results.Add(memberInfo.FullyQualifiedContainerName, (memberInfo.Name, receiverTypeName));
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
            return [.. allTypeNamesBuilder];

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
        /// so we would include in the filter info about extension members with complex receiver type.
        /// </summary>
        private static ImmutableArray<string> AddComplexTypes(ImmutableArray<string> receiverTypeNames)
        {
            return
            [
                .. receiverTypeNames,
                FindSymbols.Extensions.ComplexReceiverTypeName,
                FindSymbols.Extensions.ComplexArrayReceiverTypeName,
            ];
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
