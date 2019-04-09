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
        void GetAccessibleTopLevelTypesFromPEReference(
            Solution solution,
            Compilation compilation,
            PortableExecutableReference peReference,
            Action<CompletionItem> handleAccessibleItem,
            CancellationToken cancellationToken);

        Task GetAccessibleTopLevelTypesFromCompilationReferenceAsync(
            Solution solution,
            Compilation compilation,
            CompilationReference compilationReference,
            Action<CompletionItem> handleAccessibleItem,
            CancellationToken cancellationToken);

        /// <summary>
        /// Get all the top level types from given project. This method is intended to be used for 
        /// getting types from source only, so the project must support compilation. 
        /// For getting types from PE, use <see cref="GetAccessibleTopLevelTypesFromPEReference"/>.
        /// </summary>
        Task GetAccessibleTopLevelTypesFromProjectAsync(
            Project project,
            Action<CompletionItem> handleAccessibleItem,
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

            public async Task GetAccessibleTopLevelTypesFromProjectAsync(
                Project project,
                Action<CompletionItem> handleAccessibleItem,
                CancellationToken cancellationToken)
            {
                if (!project.SupportsCompilation)
                {
                    throw new ArgumentException(nameof(project));
                }

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

                GetAccessibleTopLevelTypesWorker(
                    project.Id,
                    compilation.Assembly.GlobalNamespace,
                    checksum,
                    isInternalsVisible: true,
                    handleAccessibleItem,
                    _projectItemsCache,
                    cancellationToken);
            }

            public async Task GetAccessibleTopLevelTypesFromCompilationReferenceAsync(
                Solution solution,
                Compilation compilation,
                CompilationReference compilationReference,
                Action<CompletionItem> handleAccessibleItem,
                CancellationToken cancellationToken)
            {
                if (!(compilation.GetAssemblyOrModuleSymbol(compilationReference) is IAssemblySymbol assemblySymbol))
                {
                    return;
                }

                var isInternalsVisible = compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assemblySymbol);
                var assemblyProject = solution.GetProject(assemblySymbol, cancellationToken);
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(assemblyProject, cancellationToken).ConfigureAwait(false);

                GetAccessibleTopLevelTypesWorker(
                    assemblyProject.Id,
                    assemblySymbol.GlobalNamespace,
                    checksum,
                    isInternalsVisible,
                    handleAccessibleItem,
                    _projectItemsCache,
                    cancellationToken);
            }

            public void GetAccessibleTopLevelTypesFromPEReference(
                Solution solution,
                Compilation compilation,
                PortableExecutableReference peReference,
                Action<CompletionItem> handleAccessibleItem,
                CancellationToken cancellationToken)
            {
                if (!(compilation.GetAssemblyOrModuleSymbol(peReference) is IAssemblySymbol assemblySymbol))
                {
                    return;
                }

                var key = GetReferenceKey(peReference);
                var isInternalsVisible = compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assemblySymbol);
                var rootNamespaceSymbol = assemblySymbol.GlobalNamespace;

                if (key == null)
                {
                    // Can't cache items for reference with null key, so just create them and return. 
                    var items = GetCompletionItemsForTopLevelTypeDeclarations(rootNamespaceSymbol);
                    HandleItems(items, isInternalsVisible, handleAccessibleItem);
                }

                var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, peReference, cancellationToken);
                GetAccessibleTopLevelTypesWorker(
                    key,
                    rootNamespaceSymbol,
                    checksum,
                    isInternalsVisible,
                    handleAccessibleItem,
                    _peItemsCache,
                    cancellationToken);

                static string GetReferenceKey(PortableExecutableReference reference)
                    => reference.FilePath ?? reference.Display;
            }

            private static void HandleItems(
                ImmutableArray<(CompletionItem item, bool visibleWithoutIVT)> items,
                bool isInternalsVisible,
                Action<CompletionItem> handleAccessibleItem)
            {
                for (var i = 0; i < items.Length; ++i)
                {
                    var item = items[i];
                    if (item.visibleWithoutIVT || isInternalsVisible)
                    {
                        handleAccessibleItem(item.item);
                    }
                }
            }

            private static void GetAccessibleTopLevelTypesWorker<TKey>(
                TKey key,
                INamespaceSymbol rootNamespace,
                Checksum checksum,
                bool isInternalsVisible,
                Action<CompletionItem> handleAccessibleItem,
                ConcurrentDictionary<TKey, ReferenceCacheEntry> cache,
                CancellationToken cancellationToken)
            {
                var tick = Environment.TickCount;
                var created = ImmutableArray<(CompletionItem, bool)>.Empty;
#if DEBUG
                try
#endif
                {
                    // Cache miss, create all requested items.
                    if (!cache.TryGetValue(key, out var cacheEntry) ||
                        cacheEntry.Checksum != checksum)
                    {
                        created = GetCompletionItemsForTopLevelTypeDeclarations(rootNamespace);
                        cacheEntry = new ReferenceCacheEntry(checksum, created);
                    }

                    HandleItems(cacheEntry.CachedItems, isInternalsVisible, handleAccessibleItem);
                }
#if DEBUG
                finally
                {
                    tick = Environment.TickCount - tick;

                    if (key is string)
                    {
                        DebugObject.debug_total_pe++;
                        DebugObject.debug_total_pe_decl_created += created.Length;
                        DebugObject.debug_total_pe_time += tick;
                    }
                    else
                    {
                        if (DebugObject.IsCurrentCompilation)
                        {
                            DebugObject.debug_total_compilation_decl_created += created.Length;
                            DebugObject.debug_total_compilation_time += tick;
                        }
                        else
                        {
                            DebugObject.debug_total_compilationRef++;
                            DebugObject.debug_total_compilationRef_decl_created += created.Length;
                            DebugObject.debug_total_compilationRef_time += tick;
                        }
                    }
                }
#endif
            }

            private static ImmutableArray<(CompletionItem item, bool visibleWithoutIVT)> GetCompletionItemsForTopLevelTypeDeclarations(INamespaceSymbol rootNamespaceSymbol)
            {
                var builder = ArrayBuilder<(CompletionItem, bool)>.GetInstance();
                VisitNamespace(rootNamespaceSymbol, null, builder);
                return builder.ToImmutableAndFree();

                static void VisitNamespace(
                    INamespaceSymbol symbol,
                    string containingNamespace,
                    ArrayBuilder<(CompletionItem, bool)> builder)
                {
                    containingNamespace = ConcatNamespace(containingNamespace, symbol.Name);

                    foreach (var memberNamespace in symbol.GetNamespaceMembers())
                    {
                        VisitNamespace(memberNamespace, containingNamespace, builder);
                    }

                    var overloads = PooledDictionary<string, TypeOverloadInfo>.GetInstance();
                    var memberTypes = symbol.GetTypeMembers();

                    foreach (var memberType in memberTypes)
                    {
                        if (IsAccessible(memberType.DeclaredAccessibility) && memberType.CanBeReferencedByName)
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
                            var item = TypeImportCompletionItem.Create(overloadInfo.NonGenericOverload, containingNamespace);
                            builder.Add((item, overloadInfo.NonGenericOverload.DeclaredAccessibility == Accessibility.Public));
                        }

                        if (overloadInfo.BestGenericOverload != null)
                        {
                            var item = TypeImportCompletionItem.Create(overloadInfo.BestGenericOverload, containingNamespace);
                            builder.Add((item, overloadInfo.ContainsPublicGenericOverload));
                        }
                    }
                }

                static bool IsAccessible(Accessibility declaredAccessibility)
                {
                    // For top level types, default accessibility is `internal`
                    return declaredAccessibility >= Accessibility.Internal || declaredAccessibility == Accessibility.NotApplicable;
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

            return containingNamespace + "." + name;
        }

        private readonly struct TypeOverloadInfo
        {
            public TypeOverloadInfo(INamedTypeSymbol nonGenericOverload, INamedTypeSymbol bestGenericOverload, bool containsPublicGenericOverload)
            {
                NonGenericOverload = nonGenericOverload;
                BestGenericOverload = bestGenericOverload;
                ContainsPublicGenericOverload = containsPublicGenericOverload;
            }

            public INamedTypeSymbol NonGenericOverload { get; }

            // Generic with fewest type parameters is considered best symbol to show in description.
            public INamedTypeSymbol BestGenericOverload { get; }

            public bool ContainsPublicGenericOverload { get; }

            public TypeOverloadInfo Aggregate(INamedTypeSymbol type)
            {
                if (type.Arity == 0)
                {
                    return new TypeOverloadInfo(type, BestGenericOverload, ContainsPublicGenericOverload);
                }

                var containsPublic = type.DeclaredAccessibility >= Accessibility.Public || ContainsPublicGenericOverload;

                // We consider generic with fewer type parameters better symbol to show in description.
                if (BestGenericOverload == null || type.Arity < BestGenericOverload.Arity)
                {
                    return new TypeOverloadInfo(NonGenericOverload, type, containsPublic);
                }

                return new TypeOverloadInfo(NonGenericOverload, BestGenericOverload, containsPublic);
            }
        }

        private readonly struct ReferenceCacheEntry
        {
            public ReferenceCacheEntry(
                Checksum checksum,
                ImmutableArray<(CompletionItem item, bool visibleWithoutIVT)> cachedItems)
            {
                Checksum = checksum;
                CachedItems = cachedItems;
            }

            public Checksum Checksum { get; }

            public ImmutableArray<(CompletionItem item, bool visibleWithoutIVT)> CachedItems { get; }
        }
    }
}
