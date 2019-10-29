// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal abstract partial class AbstractTypeImportCompletionService
    {
        private readonly struct CacheEntry
        {
            public string Language { get; }

            public Checksum Checksum { get; }

            private ImmutableArray<TypeImportCompletionItemInfo> ItemInfos { get; }

            private CacheEntry(
                Checksum checksum,
                string language,
                ImmutableArray<TypeImportCompletionItemInfo> items)
            {
                Checksum = checksum;
                Language = language;

                ItemInfos = items;
            }

            public ImmutableArray<CompletionItem> GetItemsForContext(
                string language,
                string genericTypeSuffix,
                bool isInternalsVisible,
                bool isAttributeContext,
                bool isCaseSensitive)
            {
                // We will need to adjust some items if the request is made in:
                // 1. attribute context, then we will not show or complete with "Attribute" suffix.
                // 2. a project with different langauge than when the cache entry was created,
                //    then we will change the generic suffix accordingly.
                // Otherwise, we can simply return cached items.
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

                public CacheEntry ToReferenceCacheEntry()
                {
                    return new CacheEntry(
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
        }

        [ExportWorkspaceServiceFactory(typeof(IImportCompletionCacheService<CacheEntry, CacheEntry>), ServiceLayer.Editor), Shared]
        private sealed class CacheServiceFactory : AbstractImportCompletionCacheServiceFactory<CacheEntry, CacheEntry>
        {
            [ImportingConstructor]
            public CacheServiceFactory()
            {
            }
        }
    }
}
