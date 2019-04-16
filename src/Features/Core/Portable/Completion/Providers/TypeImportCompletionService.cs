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

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal interface ITypeImportCompletionService : IWorkspaceService
    {
        /// <summary>
        /// Get all the top level types from given project. This method is intended to be used for 
        /// getting types from source only, so the project must support compilation. 
        /// For getting types from PE, use <see cref="GetTopLevelTypesFromPEReference"/>.
        /// </summary>
        Task GetTopLevelTypesAsync(
            Project project,
            Action<TypeImportCompletionItemInfo> handleItem,
            CancellationToken cancellationToken);

        void GetTopLevelTypesFromPEReference(
            Solution solution,
            Compilation compilation,
            PortableExecutableReference peReference,
            Action<TypeImportCompletionItemInfo> handleItem,
            CancellationToken cancellationToken);
    }

    internal readonly struct TypeImportCompletionItemInfo
    {
        public TypeImportCompletionItemInfo(CompletionItem item, bool isPublic)
        {
            Item = item;
            IsPublic = isPublic;
        }

        public CompletionItem Item { get; }

        public bool IsPublic { get; }
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
            var workspace = workspaceServices.Workspace;
            if (workspace.Kind == WorkspaceKind.Host)
            {
                var cacheService = workspaceServices.GetService<IWorkspaceCacheService>();
                if (cacheService != null)
                {
                    cacheService.CacheFlushRequested += OnCacheFlushRequested;
                }
            }

            return new Service(workspace, _peItemsCache, _projectItemsCache);
        }

        private void OnCacheFlushRequested(object sender, EventArgs e)
        {
            _peItemsCache.Clear();
            _projectItemsCache.Clear();
        }

        private class Service : ITypeImportCompletionService
        {
            // PE references are keyed on assembly path.
            private readonly ConcurrentDictionary<string, ReferenceCacheEntry> _peItemsCache;
            private readonly ConcurrentDictionary<ProjectId, ReferenceCacheEntry> _projectItemsCache;

            public Service(
                Workspace workspace,
                ConcurrentDictionary<string, ReferenceCacheEntry> peReferenceCache,
                ConcurrentDictionary<ProjectId, ReferenceCacheEntry> projectReferenceCache)
            {
                _peItemsCache = peReferenceCache;
                _projectItemsCache = projectReferenceCache;

                workspace.WorkspaceChanged += OnWorkspaceChanged;
            }

            public void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionReloaded:
                    case WorkspaceChangeKind.SolutionRemoved:
                        _peItemsCache.Clear();
                        _projectItemsCache.Clear();
                        break;
                    case WorkspaceChangeKind.ProjectRemoved:
                    case WorkspaceChangeKind.ProjectReloaded:
                        _projectItemsCache.TryRemove(e.ProjectId, out _);
                        break;
                }
            }

            public async Task GetTopLevelTypesAsync(
                Project project,
                Action<TypeImportCompletionItemInfo> handleItem,
                CancellationToken cancellationToken)
            {
                if (!project.SupportsCompilation)
                {
                    throw new ArgumentException(nameof(project));
                }

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                // Since we only need top level types from source, therefore we only care if source symbol checksum changes.
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

                GetAccessibleTopLevelTypesWorker(
                    project.Id,
                    compilation.Assembly,
                    checksum,
                    handleItem,
                    _projectItemsCache,
                    cancellationToken);
            }

            public void GetTopLevelTypesFromPEReference(
                Solution solution,
                Compilation compilation,
                PortableExecutableReference peReference,
                Action<TypeImportCompletionItemInfo> handleItem,
                CancellationToken cancellationToken)
            {
                var key = GetReferenceKey(peReference);
                if (key == null)
                {
                    // Can't cache items for reference with null key. We don't want risk potential perf regression by 
                    // making those items repeatedly, so simply not returning anything from this assembly, until 
                    // we have a better understanding on this sceanrio.
                    // TODO: Add telemetry
                    return;
                }

                if (!(compilation.GetAssemblyOrModuleSymbol(peReference) is IAssemblySymbol assemblySymbol))
                {
                    return;
                }

                var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, peReference, cancellationToken);
                GetAccessibleTopLevelTypesWorker(
                    key,
                    assemblySymbol,
                    checksum,
                    handleItem,
                    _peItemsCache,
                    cancellationToken);

                return;

                static string GetReferenceKey(PortableExecutableReference reference)
                    => reference.FilePath ?? reference.Display;
            }

            private static void GetAccessibleTopLevelTypesWorker<TKey>(
                TKey key,
                IAssemblySymbol assembly,
                Checksum checksum,
                Action<TypeImportCompletionItemInfo> handleItem,
                ConcurrentDictionary<TKey, ReferenceCacheEntry> cache,
                CancellationToken cancellationToken)
            {
                // Cache miss, create all requested items.
                if (!cache.TryGetValue(key, out var cacheEntry) ||
                    cacheEntry.Checksum != checksum)
                {
                    var items = GetCompletionItemsForTopLevelTypeDeclarations(assembly.GlobalNamespace, cancellationToken);
                    cacheEntry = new ReferenceCacheEntry(checksum, items);
                    cache[key] = cacheEntry;
                }

                foreach (var item in cacheEntry.CachedItems)
                {
                    handleItem(item);
                }
            }

            private static ImmutableArray<TypeImportCompletionItemInfo> GetCompletionItemsForTopLevelTypeDeclarations(
                INamespaceSymbol rootNamespaceSymbol,
                CancellationToken cancellationToken)
            {
                var builder = ArrayBuilder<TypeImportCompletionItemInfo>.GetInstance();
                VisitNamespace(rootNamespaceSymbol, containingNamespace: null, builder, cancellationToken);
                return builder.ToImmutableAndFree();

                static void VisitNamespace(
                    INamespaceSymbol symbol,
                    string containingNamespace,
                    ArrayBuilder<TypeImportCompletionItemInfo> builder,
                    CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    containingNamespace = ConcatNamespace(containingNamespace, symbol.Name);

                    foreach (var memberNamespace in symbol.GetNamespaceMembers())
                    {
                        VisitNamespace(memberNamespace, containingNamespace, builder, cancellationToken);
                    }

                    var overloads = PooledDictionary<string, TypeOverloadInfo>.GetInstance();
                    var types = symbol.GetTypeMembers();

                    // Iterate over all top level internal and public types, keep track of "type overloads".
                    foreach (var type in types)
                    {
                        // No need to check accessibility here, since top level types can only be internal or public.
                        if (type.CanBeReferencedByName)
                        {
                            overloads.TryGetValue(type.Name, out var overloadInfo);
                            overloads[type.Name] = overloadInfo.Aggregate(type);
                        }
                    }

                    foreach (var pair in overloads)
                    {
                        var overloadInfo = pair.Value;

                        // Create CompletionItem for non-generic type overload, if exists.
                        if (overloadInfo.NonGenericOverload != null)
                        {
                            var item = TypeImportCompletionItem.Create(overloadInfo.NonGenericOverload, containingNamespace);
                            var isPublic = overloadInfo.NonGenericOverload.DeclaredAccessibility == Accessibility.Public;
                            builder.Add(new TypeImportCompletionItemInfo(item, isPublic));
                        }

                        // Create one CompletionItem for all generic type overloads, if there's any.
                        // For simplicity, we always show the type symbol with lowest arity in CompletionDescription
                        // and without displaying the total number of overloads.
                        if (overloadInfo.BestGenericOverload != null)
                        {
                            // If any of the generic overloads is public, then the completion item is considered public.
                            var item = TypeImportCompletionItem.Create(overloadInfo.BestGenericOverload, containingNamespace);
                            var isPublic = overloadInfo.ContainsPublicGenericOverload;
                            builder.Add(new TypeImportCompletionItemInfo(item, isPublic));
                        }
                    }

                    overloads.Free();
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
                    return new TypeOverloadInfo(nonGenericOverload: type, BestGenericOverload, ContainsPublicGenericOverload);
                }

                // We consider generic with fewer type parameters better symbol to show in description
                var newBestGenericOverload = BestGenericOverload == null || type.Arity < BestGenericOverload.Arity
                    ? type
                    : BestGenericOverload;

                var newContainsPublicGenericOverload = type.DeclaredAccessibility >= Accessibility.Public || ContainsPublicGenericOverload;

                return new TypeOverloadInfo(NonGenericOverload, newBestGenericOverload, newContainsPublicGenericOverload);
            }
        }

        private readonly struct ReferenceCacheEntry
        {
            public ReferenceCacheEntry(
                Checksum checksum,
                ImmutableArray<TypeImportCompletionItemInfo> cachedItems)
            {
                Checksum = checksum;
                CachedItems = cachedItems;
            }

            public Checksum Checksum { get; }

            public ImmutableArray<TypeImportCompletionItemInfo> CachedItems { get; }
        }
    }
}
