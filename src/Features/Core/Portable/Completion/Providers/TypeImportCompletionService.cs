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
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

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
            SyntaxContext syntaxContext,
            Action<TypeImportCompletionItemInfo> handleItem,
            CancellationToken cancellationToken);

        void GetTopLevelTypesFromPEReference(
            Solution solution,
            Compilation compilation,
            PortableExecutableReference peReference,
            SyntaxContext syntaxContext,
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

        public TypeImportCompletionItemInfo WithItem(CompletionItem item)
        {
            return new TypeImportCompletionItemInfo(item, IsPublic);
        }
    }

    [ExportWorkspaceServiceFactory(typeof(ITypeImportCompletionService), ServiceLayer.Editor), Shared]
    internal sealed class TypeImportCompletionService : IWorkspaceServiceFactory
    {
        private readonly ConcurrentDictionary<string, ReferenceCacheEntry> _peItemsCache
            = new ConcurrentDictionary<string, ReferenceCacheEntry>();

        private readonly ConcurrentDictionary<ProjectId, ReferenceCacheEntry> _projectItemsCache
            = new ConcurrentDictionary<ProjectId, ReferenceCacheEntry>();

        [ImportingConstructor]
        public TypeImportCompletionService()
        {
        }

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
                SyntaxContext syntaxContext,
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
                    syntaxContext,
                    handleItem,
                    _projectItemsCache,
                    cancellationToken);
            }

            public void GetTopLevelTypesFromPEReference(
                Solution solution,
                Compilation compilation,
                PortableExecutableReference peReference,
                SyntaxContext syntaxContext,
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
                    syntaxContext,
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
                SyntaxContext syntaxContext,
                Action<TypeImportCompletionItemInfo> handleItem,
                ConcurrentDictionary<TKey, ReferenceCacheEntry> cache,
                CancellationToken cancellationToken)
            {
                var isCSharp = syntaxContext.SemanticModel.Language == LanguageNames.CSharp;

                // Cache miss, create all requested items.
                if (!cache.TryGetValue(key, out var cacheEntry) ||
                    cacheEntry.Checksum != checksum)
                {
                    var builder = new ReferenceCacheEntry.Builder(checksum, isCSharp);
                    GetCompletionItemsForTopLevelTypeDeclarations(assembly.GlobalNamespace, builder, cancellationToken);
                    cacheEntry = builder.ToReferenceCacheEntry();
                    cache[key] = cacheEntry;
                }

                foreach (var item in cacheEntry.CommonItems)
                {
                    handleItem(item);
                }

                foreach (var item in cacheEntry.GetGenericItems(isCSharp))
                {
                    handleItem(item);
                }

                foreach (var item in cacheEntry.GetAttributeItems(isCSharp, syntaxContext.IsAttributeNameContext))
                {
                    handleItem(item);
                }
            }

            private static void GetCompletionItemsForTopLevelTypeDeclarations(
                INamespaceSymbol rootNamespaceSymbol,
                ReferenceCacheEntry.Builder builder,
                CancellationToken cancellationToken)
            {
                VisitNamespace(rootNamespaceSymbol, containingNamespace: null, builder, cancellationToken);
                return;

                static void VisitNamespace(
                    INamespaceSymbol symbol,
                    string containingNamespace,
                     ReferenceCacheEntry.Builder builder,
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
                            builder.AddItem(
                                overloadInfo.NonGenericOverload,
                                containingNamespace,
                                overloadInfo.NonGenericOverload.DeclaredAccessibility == Accessibility.Public);
                        }

                        // Create one CompletionItem for all generic type overloads, if there's any.
                        // For simplicity, we always show the type symbol with lowest arity in CompletionDescription
                        // and without displaying the total number of overloads.
                        if (overloadInfo.BestGenericOverload != null)
                        {
                            // If any of the generic overloads is public, then the completion item is considered public.
                            builder.AddItem(
                                overloadInfo.BestGenericOverload,
                                containingNamespace,
                                overloadInfo.ContainsPublicGenericOverload);
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
            public class Builder
            {
                private readonly bool _isCSharp;
                private readonly Checksum _checksum;

                public Builder(Checksum checksum, bool isCSharp)
                {
                    _checksum = checksum;
                    _isCSharp = isCSharp;

                    _commonItemsBuilder = ArrayBuilder<TypeImportCompletionItemInfo>.GetInstance();
                    _genericItemsBuilder = ArrayBuilder<TypeImportCompletionItemInfo>.GetInstance();
                    _attributeWithSuffixItemsBuilder = ArrayBuilder<TypeImportCompletionItemInfo>.GetInstance();
                }

                private ArrayBuilder<TypeImportCompletionItemInfo> _commonItemsBuilder;
                private ArrayBuilder<TypeImportCompletionItemInfo> _genericItemsBuilder;
                private ArrayBuilder<TypeImportCompletionItemInfo> _attributeWithSuffixItemsBuilder;

                public ReferenceCacheEntry ToReferenceCacheEntry()
                {
                    return new ReferenceCacheEntry(
                        _checksum,
                        _isCSharp,
                        _commonItemsBuilder.ToImmutableAndFree(),
                        _genericItemsBuilder.ToImmutableAndFree(),
                        _attributeWithSuffixItemsBuilder.ToImmutableAndFree());
                }

                public void AddItem(INamedTypeSymbol symbol, string containingNamespace, bool isPublic)
                {
                    ArrayBuilder<TypeImportCompletionItemInfo> correspondingBuilder;

                    if (symbol.Arity > 0)
                    {
                        correspondingBuilder = _genericItemsBuilder;
                    }
                    else if (IsAttributeWithAttributeSuffix(symbol))
                    {
                        correspondingBuilder = _attributeWithSuffixItemsBuilder;
                    }
                    else
                    {
                        correspondingBuilder = _commonItemsBuilder;
                    }

                    var item = TypeImportCompletionItem.Create(symbol, containingNamespace, _isCSharp);
                    item.IsCached = true;
                    correspondingBuilder.Add(new TypeImportCompletionItemInfo(item, isPublic));

                    static bool IsAttributeWithAttributeSuffix(INamedTypeSymbol symbol)
                    {
                        // Do the symbol textual check first. Then the more expensive symbolic check.
                        return symbol.Name.HasAttributeSuffix(isCaseSensitive: false) && symbol.IsAttribute();
                    }
                }
            }

            private ReferenceCacheEntry(
                Checksum checksum,
                bool isCSharp,
                ImmutableArray<TypeImportCompletionItemInfo> commonItems,
                ImmutableArray<TypeImportCompletionItemInfo> genericItems,
                ImmutableArray<TypeImportCompletionItemInfo> attributeItems)
            {
                Checksum = checksum;
                IsCSharp = isCSharp;

                CommonItems = commonItems;
                AttributeItems = attributeItems;
                GenericItems = genericItems;
            }

            public bool IsCSharp { get; }

            public Checksum Checksum { get; }

            public ImmutableArray<TypeImportCompletionItemInfo> CommonItems { get; }

            private ImmutableArray<TypeImportCompletionItemInfo> GenericItems { get; }

            // Attribute types can't be generic
            private ImmutableArray<TypeImportCompletionItemInfo> AttributeItems { get; }

            public ImmutableArray<TypeImportCompletionItemInfo> GetGenericItems(bool isCSharp)
            {
                if (isCSharp == IsCSharp || GenericItems.Length == 0)
                {
                    return GenericItems;
                }

                // We don't want to cache this item.
                return GenericItems.SelectAsArray(itemInfo => itemInfo.WithItem(TypeImportCompletionItem.CreateItemWithGenericDisplaySuffix(itemInfo.Item, isCSharp)));
            }

            public ImmutableArray<TypeImportCompletionItemInfo> GetAttributeItems(bool isCSharp, bool isAttributeNameContext)
            {
                if (!isAttributeNameContext || AttributeItems.Length == 0)
                {
                    return AttributeItems;
                }

                return AttributeItems.SelectAsArray(GetAppropriateAttributeItem, isCSharp);

                static TypeImportCompletionItemInfo GetAppropriateAttributeItem(TypeImportCompletionItemInfo itemInfo, bool isCSharp)
                {
                    var item = itemInfo.Item;
                    if (item.DisplayText.TryGetWithoutAttributeSuffix(isCaseSensitive: isCSharp, out var attributeName))
                    {
                        // We don't want to cache this item.
                        return itemInfo.WithItem(TypeImportCompletionItem.CreateAttributeNameItem(item, attributeName));
                    }

                    return itemInfo;
                }
            }
        }
    }
}
