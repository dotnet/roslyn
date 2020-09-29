// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.Shared.Utilities.EditorBrowsableHelpers;

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
                bool isCaseSensitive,
                bool hideAdvancedMembers)
            {
                var isSameLanguage = Language == language;
                using var _ = ArrayBuilder<CompletionItem>.GetInstance(out var builder);

                foreach (var info in ItemInfos)
                {
                    if (!info.IsPublic && !isInternalsVisible)
                    {
                        continue;
                    }

                    // Option to show advanced members is false so we need to exclude them.
                    if (hideAdvancedMembers && info.IsEditorBrowsableStateAdvanced)
                    {
                        continue;
                    }

                    var item = info.Item;

                    if (isAttributeContext)
                    {
                        // Don't show non attribute item in attribute context
                        if (!info.IsAttribute)
                        {
                            continue;
                        }

                        // We are in attribute context, will not show or complete with "Attribute" suffix.
                        item = GetAppropriateAttributeItem(info.Item, isCaseSensitive);
                    }

                    // C# and VB the display text is different for generics, i.e. <T> and (Of T). For simpllicity, we only cache for one language.
                    // But when we trigger in a project with different language than when the cache entry was created for, we will need to
                    // change the generic suffix accordingly.
                    if (!isSameLanguage && info.IsGeneric)
                    {
                        // We don't want to cache this item.
                        item = ImportCompletionItem.CreateItemWithGenericDisplaySuffix(item, genericTypeSuffix);
                    }

                    builder.Add(item);
                }

                return builder.ToImmutable();

                static CompletionItem GetAppropriateAttributeItem(CompletionItem attributeItem, bool isCaseSensitive)
                {
                    if (attributeItem.DisplayText.TryGetWithoutAttributeSuffix(isCaseSensitive: isCaseSensitive, out var attributeNameWithoutSuffix))
                    {
                        // We don't want to cache this item.
                        return ImportCompletionItem.CreateAttributeItemWithoutSuffix(attributeItem, attributeNameWithoutSuffix, CompletionItemFlags.Expanded);
                    }

                    return attributeItem;
                }
            }

            public class Builder : IDisposable
            {
                private readonly string _language;
                private readonly string _genericTypeSuffix;
                private readonly Checksum _checksum;
                private readonly EditorBrowsableInfo _editorBrowsableInfo;

                private readonly ArrayBuilder<TypeImportCompletionItemInfo> _itemsBuilder;

                public Builder(Checksum checksum, string language, string genericTypeSuffix, EditorBrowsableInfo editorBrowsableInfo)
                {
                    _checksum = checksum;
                    _language = language;
                    _genericTypeSuffix = genericTypeSuffix;
                    _editorBrowsableInfo = editorBrowsableInfo;

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
                    // We want to cache items with EditoBrowsableState == Advanced regardless of current "hide adv members" option value
                    var (isBrowsable, isEditorBrowsableStateAdvanced) = symbol.IsEditorBrowsableWithState(
                        hideAdvancedMembers: false,
                        _editorBrowsableInfo.Compilation,
                        _editorBrowsableInfo);

                    if (!isBrowsable)
                    {
                        // Hide this item from completion
                        return;
                    }

                    var isGeneric = symbol.Arity > 0;

                    // Need to determine if a type is an attribute up front since we want to filter out 
                    // non-attribute types when in attribute context. We can't do this lazily since we don't hold 
                    // on to symbols. However, the cost of calling `IsAttribute` on every top-level type symbols 
                    // is prohibitively high, so we opt for the heuristic that would do the simple textual "Attribute" 
                    // suffix check first, then the more expensive symbolic check. As a result, all unimported
                    // attribute types that don't have "Attribute" suffix would be filtered out when in attribute context.
                    var isAttribute = symbol.Name.HasAttributeSuffix(isCaseSensitive: false) && symbol.IsAttribute();

                    var item = ImportCompletionItem.Create(
                        symbol.Name,
                        symbol.Arity,
                        containingNamespace,
                        symbol.GetGlyph(),
                        _genericTypeSuffix,
                        CompletionItemFlags.CachedAndExpanded,
                        extensionMethodData: null);

                    _itemsBuilder.Add(new TypeImportCompletionItemInfo(item, isPublic, isGeneric, isAttribute, isEditorBrowsableStateAdvanced));
                }

                public void Dispose()
                    => _itemsBuilder.Free();
            }
        }

        [ExportWorkspaceServiceFactory(typeof(IImportCompletionCacheService<CacheEntry, CacheEntry>), ServiceLayer.Editor), Shared]
        private sealed class CacheServiceFactory : AbstractImportCompletionCacheServiceFactory<CacheEntry, CacheEntry>
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public CacheServiceFactory()
            {
            }
        }
    }
}
