// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal abstract partial class AbstractTypeImportCompletionService : ITypeImportCompletionService
    {
        private IImportCompletionCacheService CacheService { get; }

        protected abstract string GenericTypeSuffix { get; }

        protected abstract bool IsCaseSensitive { get; }

        internal AbstractTypeImportCompletionService(Workspace workspace)
        {
            CacheService = workspace.Services.GetService<IImportCompletionCacheService>();
        }

        public async Task<ImmutableArray<CompletionItem>> GetTopLevelTypesAsync(
            Project project,
            SyntaxContext syntaxContext,
            bool isInternalsVisible,
            CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
            {
                throw new ArgumentException(nameof(project));
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Since we only need top level types from source, therefore we only care if source symbol checksum changes.
            var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

            return GetAccessibleTopLevelTypesWorker(
                project.Id,
                compilation.Assembly,
                checksum,
                syntaxContext,
                isInternalsVisible,
                CacheService.ProjectItemsCache,
                cancellationToken);
        }

        public ImmutableArray<CompletionItem> GetTopLevelTypesFromPEReference(
            Solution solution,
            Compilation compilation,
            PortableExecutableReference peReference,
            SyntaxContext syntaxContext,
            bool isInternalsVisible,
            CancellationToken cancellationToken)
        {
            var key = GetReferenceKey(peReference);
            if (key == null)
            {
                // Can't cache items for reference with null key. We don't want risk potential perf regression by 
                // making those items repeatedly, so simply not returning anything from this assembly, until 
                // we have a better understanding on this sceanrio.
                // TODO: Add telemetry
                return ImmutableArray<CompletionItem>.Empty;
            }

            if (!(compilation.GetAssemblyOrModuleSymbol(peReference) is IAssemblySymbol assemblySymbol))
            {
                return ImmutableArray<CompletionItem>.Empty;
            }

            var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, peReference, cancellationToken);
            return GetAccessibleTopLevelTypesWorker(
                key,
                assemblySymbol,
                checksum,
                syntaxContext,
                isInternalsVisible,
                CacheService.PEItemsCache,
                cancellationToken);

            static string GetReferenceKey(PortableExecutableReference reference)
                => reference.FilePath ?? reference.Display;
        }

        private ImmutableArray<CompletionItem> GetAccessibleTopLevelTypesWorker<TKey>(
            TKey key,
            IAssemblySymbol assembly,
            Checksum checksum,
            SyntaxContext syntaxContext,
            bool isInternalsVisible,
            IDictionary<TKey, ReferenceCacheEntry> cache,
            CancellationToken cancellationToken)
        {
            var cacheEntry = GetCacheEntry(key, assembly, checksum, syntaxContext, cache, cancellationToken);
            return cacheEntry.GetItemsForContext(
                syntaxContext.SemanticModel.Language,
                GenericTypeSuffix,
                isInternalsVisible,
                syntaxContext.IsAttributeNameContext,
                IsCaseSensitive);
        }

        private ReferenceCacheEntry GetCacheEntry<TKey>(
            TKey key,
            IAssemblySymbol assembly,
            Checksum checksum,
            SyntaxContext syntaxContext,
            IDictionary<TKey, ReferenceCacheEntry> cache,
            CancellationToken cancellationToken)
        {
            var language = syntaxContext.SemanticModel.Language;

            // Cache miss, create all requested items.
            if (!cache.TryGetValue(key, out var cacheEntry) ||
                cacheEntry.Checksum != checksum)
            {
                using var builder = new ReferenceCacheEntry.Builder(checksum, language, GenericTypeSuffix);
                GetCompletionItemsForTopLevelTypeDeclarations(assembly.GlobalNamespace, builder, cancellationToken);
                cacheEntry = builder.ToReferenceCacheEntry();
                cache[key] = cacheEntry;
            }

            return cacheEntry;
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
            public class Builder : IDisposable
            {
                private readonly string _language;
                private readonly string _genericTypeSuffix;
                private readonly Checksum _checksum;

                private readonly ArrayBuilder<TypeImportCompletionItemInfo> _itemsBuilder;

                public Builder(Checksum checksum, string language, string genericTypeSuffix)
                {
                    _checksum = checksum;
                    _language = language;
                    _genericTypeSuffix = genericTypeSuffix;

                    _itemsBuilder = ArrayBuilder<TypeImportCompletionItemInfo>.GetInstance();
                }

                public ReferenceCacheEntry ToReferenceCacheEntry()
                {
                    return new ReferenceCacheEntry(
                        _checksum,
                        _language,
                        _itemsBuilder.ToImmutable());
                }

                public void AddItem(INamedTypeSymbol symbol, string containingNamespace, bool isPublic)
                {
                    var isGeneric = symbol.Arity > 0;

                    // Need to determine if a type is an attribute up front since we want to filter out 
                    // non-attribute types when in attribute context. We can't do this lazily since we don't hold 
                    // on to symbols. However, the cost of calling `IsAttribute` on every top-level type symbols 
                    // is prohibitively high, so we opt for the heuristic that would do the simple textual "Attribute" 
                    // suffix check first, then the more expensive symbolic check. As a result, all unimported
                    // attribute types that don't have "Attribute" suffix would be filtered out when in attribute context.
                    var isAttribute = symbol.Name.HasAttributeSuffix(isCaseSensitive: false) && symbol.IsAttribute();

                    var item = ImportCompletionItem.Create(symbol, containingNamespace, _genericTypeSuffix);
                    _itemsBuilder.Add(new TypeImportCompletionItemInfo(item, isPublic, isGeneric, isAttribute));
                }

                public void Dispose()
                    => _itemsBuilder.Free();
            }

            private ReferenceCacheEntry(
                Checksum checksum,
                string language,
                ImmutableArray<TypeImportCompletionItemInfo> items)
            {
                Checksum = checksum;
                Language = language;

                ItemInfos = items;
            }

            public string Language { get; }

            public Checksum Checksum { get; }

            private ImmutableArray<TypeImportCompletionItemInfo> ItemInfos { get; }

            public ImmutableArray<CompletionItem> GetItemsForContext(
                string language,
                string genericTypeSuffix,
                bool isInternalsVisible,
                bool isAttributeContext,
                bool isCaseSensitive)
            {
                var isSameLanguage = Language == language;
                if (isSameLanguage && !isAttributeContext)
                {
                    return ItemInfos.Where(info => info.IsPublic || isInternalsVisible).SelectAsArray(info => info.Item);
                }

                var builder = ArrayBuilder<CompletionItem>.GetInstance();
                foreach (var info in ItemInfos)
                {
                    if (info.IsPublic || isInternalsVisible)
                    {
                        var item = info.Item;
                        if (isAttributeContext)
                        {
                            if (!info.IsAttribute)
                            {
                                continue;
                            }

                            item = GetAppropriateAttributeItem(info.Item, isCaseSensitive);
                        }

                        if (!isSameLanguage && info.IsGeneric)
                        {
                            // We don't want to cache this item.
                            item = ImportCompletionItem.CreateItemWithGenericDisplaySuffix(item, genericTypeSuffix);
                        }

                        builder.Add(item);
                    }
                }

                return builder.ToImmutableAndFree();

                static CompletionItem GetAppropriateAttributeItem(CompletionItem attributeItem, bool isCaseSensitive)
                {
                    if (attributeItem.DisplayText.TryGetWithoutAttributeSuffix(isCaseSensitive: isCaseSensitive, out var attributeNameWithoutSuffix))
                    {
                        // We don't want to cache this item.
                        return ImportCompletionItem.CreateAttributeItemWithoutSuffix(attributeItem, attributeNameWithoutSuffix);
                    }

                    return attributeItem;
                }
            }
        }

        private readonly struct TypeImportCompletionItemInfo
        {
            private readonly ItemPropertyKind _properties;

            public TypeImportCompletionItemInfo(CompletionItem item, bool isPublic, bool isGeneric, bool isAttribute)
            {
                Item = item;
                _properties = (isPublic ? ItemPropertyKind.IsPublic : 0)
                            | (isGeneric ? ItemPropertyKind.IsGeneric : 0)
                            | (isAttribute ? ItemPropertyKind.IsAttribute : 0);
            }

            public CompletionItem Item { get; }

            public bool IsPublic
                => (_properties & ItemPropertyKind.IsPublic) != 0;

            public bool IsGeneric
                => (_properties & ItemPropertyKind.IsGeneric) != 0;

            public bool IsAttribute
                => (_properties & ItemPropertyKind.IsAttribute) != 0;

            public TypeImportCompletionItemInfo WithItem(CompletionItem item)
            {
                return new TypeImportCompletionItemInfo(item, IsPublic, IsGeneric, IsAttribute);
            }

            [Flags]
            private enum ItemPropertyKind : byte
            {
                IsPublic = 0x1,
                IsGeneric = 0x2,
                IsAttribute = 0x4
            }
        }
    }
}
