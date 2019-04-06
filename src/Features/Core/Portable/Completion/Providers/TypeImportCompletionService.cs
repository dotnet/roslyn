// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal interface ITypeImportCompletionService : IWorkspaceService
    {
        ImmutableArray<TypeImportCompletionItem> GetAccessibleTopLevelTypesFromPEReference(
            Solution solution,
            Compilation compilation,
            PortableExecutableReference peReference,
            ImmutableHashSet<string> excludedNamespaces,
            CancellationToken cancellationToken);

        Task<ImmutableArray<TypeImportCompletionItem>> GetAccessibleTopLevelTypesFromCompilationReferenceAsync(
            Solution solution,
            Compilation compilation,
            CompilationReference compilationReference,
            ImmutableHashSet<string> excludedNamespaces,
            CancellationToken cancellationToken);

        Task<ImmutableArray<TypeImportCompletionItem>> GetAccessibleTopLevelTypesFromProjectAsync(
            Project project,
            ImmutableHashSet<string> excludedNamespaces,
            CancellationToken cancellationToken);
    }

    [ExportWorkspaceServiceFactory(typeof(ITypeImportCompletionService), ServiceLayer.Editor), Shared]
    internal sealed class TypeImportCompletionService : IWorkspaceServiceFactory
    {
        private readonly ConcurrentDictionary<string, ReferenceCacheEntry> _peItemsCache
            = new ConcurrentDictionary<string, ReferenceCacheEntry>();

        private readonly ConcurrentDictionary<ProjectId, ReferenceCacheEntry> _projectItemsCache
            = new ConcurrentDictionary<ProjectId, ReferenceCacheEntry>();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var cacheService = workspaceServices.GetService<IWorkspaceCacheService>();
            if (cacheService != null)
            {
                cacheService.CacheFlushRequested += OnCacheFlushRequested;
            }

            return new Service(_peItemsCache, _projectItemsCache);
        }

        private void OnCacheFlushRequested(object sender, EventArgs e)
        {
            _peItemsCache.Clear();
            _projectItemsCache.Clear();
        }

        private class Service : ITypeImportCompletionService
        {
            private readonly ConcurrentDictionary<string, ReferenceCacheEntry> _peItemsCache;
            private readonly ConcurrentDictionary<ProjectId, ReferenceCacheEntry> _projectItemsCache;

            public Service(ConcurrentDictionary<string, ReferenceCacheEntry> peReferenceCache, ConcurrentDictionary<ProjectId, ReferenceCacheEntry> projectReferenceCache)
            {
                _peItemsCache = peReferenceCache;
                _projectItemsCache = projectReferenceCache;
            }

            public async Task<ImmutableArray<TypeImportCompletionItem>> GetAccessibleTopLevelTypesFromProjectAsync(
                Project project,
                ImmutableHashSet<string> excludedNamespaces,
                CancellationToken cancellationToken)
            {
#if DEBUG
                DebugObject.IsCurrentCompilation = true;
#endif

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

                return GetAccessibleTopLevelTypesWorker(project.Id, compilation.Assembly.GlobalNamespace, checksum, excludedNamespaces, isInternalsVisible: true, _projectItemsCache, cancellationToken);
            }

            public async Task<ImmutableArray<TypeImportCompletionItem>> GetAccessibleTopLevelTypesFromCompilationReferenceAsync(
                Solution solution,
                Compilation compilation,
                CompilationReference compilationReference,
                ImmutableHashSet<string> excludedNamespaces,
                CancellationToken cancellationToken)
            {
#if DEBUG
                DebugObject.IsCurrentCompilation = false;
#endif

                if (!(compilation.GetAssemblyOrModuleSymbol(compilationReference) is IAssemblySymbol assemblySymbol))
                {
                    return ImmutableArray<TypeImportCompletionItem>.Empty;
                }

                var isInternalsVisible = compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assemblySymbol);
                var assemblyProject = solution.GetProject(assemblySymbol, cancellationToken);
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(assemblyProject, cancellationToken).ConfigureAwait(false);

                return GetAccessibleTopLevelTypesWorker(assemblyProject.Id, assemblySymbol.GlobalNamespace, checksum, excludedNamespaces, isInternalsVisible, _projectItemsCache, cancellationToken);
            }

            public ImmutableArray<TypeImportCompletionItem> GetAccessibleTopLevelTypesFromPEReference(
                Solution solution,
                Compilation compilation,
                PortableExecutableReference peReference,
                ImmutableHashSet<string> excludedNamespaces,
                CancellationToken cancellationToken)
            {
                if (!(compilation.GetAssemblyOrModuleSymbol(peReference) is IAssemblySymbol assemblySymbol))
                {
                    return ImmutableArray<TypeImportCompletionItem>.Empty;
                }

                var key = GetReferenceKey(peReference);
                var isInternalsVisible = compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assemblySymbol);
                var rootNamespaceSymbol = assemblySymbol.GlobalNamespace;

                if (key == null)
                {
                    // Can't cache items for reference with null key, so just create them and return. 
                    return GetCompletionItemsForTopLevelTypeDeclarations(rootNamespaceSymbol, ns => !excludedNamespaces.Contains(ns), isInternalsVisible);
                }

                var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, peReference, cancellationToken);
                return GetAccessibleTopLevelTypesWorker(key, rootNamespaceSymbol, checksum, excludedNamespaces, isInternalsVisible, _peItemsCache, cancellationToken);

                static string GetReferenceKey(PortableExecutableReference reference)
                    => reference.FilePath ?? reference.Display;
            }

            private static ImmutableArray<TypeImportCompletionItem> GetAccessibleTopLevelTypesWorker<TKey>(
                TKey key,
                INamespaceSymbol rootNamespace,
                Checksum checksum,
                ImmutableHashSet<string> excludedNamespaces,
                bool isInternalsVisible,
                ConcurrentDictionary<TKey, ReferenceCacheEntry> cache,
                CancellationToken cancellationToken)
            {
                var tick = Environment.TickCount;
                var returned = ImmutableArray<TypeImportCompletionItem>.Empty;
                var created = ImmutableArray<TypeImportCompletionItem>.Empty;
#if DEBUG
                try
                {
#endif
                    // Cache miss, create all requested items.
                    if (!cache.TryGetValue(key, out var cacheEntry) ||
                        cacheEntry.Checksum != checksum ||
                        !AccessibilityMatch(cacheEntry.IncludeInternalTypes, isInternalsVisible))
                    {
                        var items = GetCompletionItemsForTopLevelTypeDeclarations(rootNamespace, ns => !excludedNamespaces.Contains(ns), isInternalsVisible);
                        cache[key] = new ReferenceCacheEntry(checksum, isInternalsVisible, excludedNamespaces, items);

                        returned = created = items;
                        return items;
                    }

                    // Having fewer excluded names space in cache than in request means the cache contains items for all the types not excluded.
                    if (cacheEntry.ExcludedNamespaces.IsSubsetOf(excludedNamespaces))
                    {
                        returned = cacheEntry.CachedItems.WhereAsArray(item => !excludedNamespaces.Contains(item.ContainingNamespace));
                        return returned;
                    }

                    var namespacesToInclude = cacheEntry.ExcludedNamespaces.Except(excludedNamespaces);

                    var itemsFromNamespacesToInclude = GetCompletionItemsForTopLevelTypeDeclarations(rootNamespace, namespacesToInclude.Contains, isInternalsVisible);

                    var itemsToCache = cacheEntry.CachedItems.Concat(itemsFromNamespacesToInclude);
                    var excludedNamespacesToCache = cacheEntry.ExcludedNamespaces.Intersect(excludedNamespaces);

                    cache[key] = new ReferenceCacheEntry(checksum, isInternalsVisible, excludedNamespacesToCache, itemsToCache);

                    created = itemsFromNamespacesToInclude;
                    returned = itemsToCache.WhereAsArray(item => !excludedNamespaces.Contains(item.ContainingNamespace));

                    return returned;
#if DEBUG
                }
                finally
                {
                    tick = Environment.TickCount - tick;

                    if (key is string)
                    {
                        DebugObject.SetPE(returned.Length, created.Length, tick);
                    }
                    else
                    {
                        DebugObject.SetCompilation(returned.Length, created.Length, tick);
                    }
                }
#endif
                static bool AccessibilityMatch(bool includeInternalTypes, bool isInternalsVisible)
                {
                    return isInternalsVisible
                        ? includeInternalTypes
                        : true;
                }
            }

            private static ImmutableArray<TypeImportCompletionItem> GetCompletionItemsForTopLevelTypeDeclarations(
                INamespaceSymbol rootNamespaceSymbol,
                Func<string, bool> predicate,
                bool isInternalsVisible)
            {
                var builder = ArrayBuilder<TypeImportCompletionItem>.GetInstance();
                VisitNamespace(rootNamespaceSymbol, null);
                return builder.ToImmutableAndFree();

                void VisitNamespace(INamespaceSymbol symbol, string containingNamespace)
                {
                    containingNamespace = ConcatNamespace(containingNamespace, symbol.Name);

                    foreach (var memberNamespace in symbol.GetNamespaceMembers())
                    {
                        VisitNamespace(memberNamespace, containingNamespace);
                    }

                    if (predicate(containingNamespace))
                    {
                        var overloads = PooledDictionary<string, TypeOverloadInfo>.GetInstance();
                        var memberTypes = symbol.GetTypeMembers();

                        foreach (var memberType in memberTypes)
                        {
                            if (IsAccessible(memberType.DeclaredAccessibility, isInternalsVisible)
                                && memberType.CanBeReferencedByName)
                            {
                                if (!overloads.TryGetValue(memberType.Name, out var overloadInfo))
                                {
                                    overloadInfo = default;
                                }
                                overloads[memberType.Name] = overloadInfo.Aggregate(memberType);
                            }
                        }

                        foreach (var pair in overloads)
                        {
                            var overloadInfo = pair.Value;
                            if (overloadInfo.NonGenericOverload != null)
                            {
                                var item = TypeImportCompletionItem.Create(overloadInfo.NonGenericOverload, containingNamespace, overloadInfo.Count - 1);
                                builder.Add(item);
                            }

                            if (overloadInfo.BestGenericOverload != null)
                            {
                                var item = TypeImportCompletionItem.Create(overloadInfo.BestGenericOverload, containingNamespace, overloadInfo.Count - 1);
                                builder.Add(item);
                            }
                        }
                    }
                }

                static bool IsAccessible(Accessibility declaredAccessibility, bool isInternalsVisible)
                {
                    // For top level types, default accessibility is `internal`
                    return isInternalsVisible
                        ? declaredAccessibility >= Accessibility.Internal || declaredAccessibility == Accessibility.NotApplicable
                        : declaredAccessibility >= Accessibility.Public;
                }
            }
        }

        private static string ConcatNamespace(string containingNamespace, string name)
        {
            Debug.Assert(name != null);
            if (string.IsNullOrEmpty(containingNamespace))
            {
                return name;
            }

            var @namespace = containingNamespace + "." + name;
#if DEBUG
            DebugObject.debug_total_namespace_concat++;
            DebugObject.Namespaces.Add(@namespace);
#endif
            return @namespace;
        }

        private readonly struct TypeOverloadInfo
        {
            public TypeOverloadInfo(INamedTypeSymbol nonGenericOverload, INamedTypeSymbol bestGenericOverload, int count)
            {
                NonGenericOverload = nonGenericOverload;
                BestGenericOverload = bestGenericOverload;
                Count = count;
            }

            public INamedTypeSymbol NonGenericOverload { get; }

            // Generic with fewest type parameters is considered best symbol to show in description.
            public INamedTypeSymbol BestGenericOverload { get; }

            public int Count { get; }

            public TypeOverloadInfo Aggregate(INamedTypeSymbol type)
            {
                if (type.Arity == 0)
                {
                    return new TypeOverloadInfo(type, BestGenericOverload, Count + 1);
                }

                // We consider generic with fewer type parameters better symbol to show in description.
                if (BestGenericOverload == null || type.Arity < BestGenericOverload.Arity)
                {
                    return new TypeOverloadInfo(NonGenericOverload, type, Count + 1);
                }

                return new TypeOverloadInfo(NonGenericOverload, BestGenericOverload, Count + 1);
            }
        }

        private readonly struct ReferenceCacheEntry
        {
            public ReferenceCacheEntry(
                Checksum checksum,
                bool includeInternalTypes,
                ImmutableHashSet<string> excludedNamespaces,
                ImmutableArray<TypeImportCompletionItem> cachedItems)
            {
                IncludeInternalTypes = includeInternalTypes;
                ExcludedNamespaces = excludedNamespaces;
                Checksum = checksum;
                CachedItems = cachedItems;
            }

            public Checksum Checksum { get; }

            public bool IncludeInternalTypes { get; }

            public ImmutableHashSet<string> ExcludedNamespaces { get; }

            public ImmutableArray<TypeImportCompletionItem> CachedItems { get; }
        }
    }
}
