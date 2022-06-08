// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.Shared.Utilities.EditorBrowsableHelpers;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal readonly struct TypeImportCompletionCacheEntry
    {
        public SymbolKey AssemblySymbolKey { get; }

        public string Language { get; }

        public Checksum Checksum { get; }

        private ImmutableArray<TypeImportCompletionItemInfo> ItemInfos { get; }

        /// <summary>
        /// The number of items in this entry for types declared as public.
        /// This is used to minimize memory allocation in case non-public items aren't needed.
        /// </summary>
        private int PublicItemCount { get; }

        private TypeImportCompletionCacheEntry(
            SymbolKey assemblySymbolKey,
            Checksum checksum,
            string language,
            ImmutableArray<TypeImportCompletionItemInfo> items,
            int publicItemCount)
        {
            AssemblySymbolKey = assemblySymbolKey;
            Checksum = checksum;
            Language = language;

            ItemInfos = items;
            PublicItemCount = publicItemCount;
        }

        public ImmutableArray<CompletionItem> GetItemsForContext(
            Compilation originCompilation,
            string language,
            string genericTypeSuffix,
            bool isAttributeContext,
            bool isCaseSensitive,
            bool hideAdvancedMembers)
        {
            if (AssemblySymbolKey.Resolve(originCompilation).Symbol is not IAssemblySymbol assemblySymbol)
                return ImmutableArray<CompletionItem>.Empty;

            var isSameLanguage = Language == language;
            var isInternalsVisible = originCompilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assemblySymbol);
            using var _ = ArrayBuilder<CompletionItem>.GetInstance(out var builder);

            // PERF: try set the capacity upfront to avoid allocation from Resize
            if (!isAttributeContext)
            {
                if (isInternalsVisible)
                {
                    builder.EnsureCapacity(ItemInfos.Length);
                }
                else
                {
                    builder.EnsureCapacity(PublicItemCount);
                }
            }

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
            private readonly SymbolKey _assemblySymbolKey;
            private readonly string _language;
            private readonly string _genericTypeSuffix;
            private readonly Checksum _checksum;
            private readonly EditorBrowsableInfo _editorBrowsableInfo;

            private int _publicItemCount;

            private readonly ArrayBuilder<TypeImportCompletionItemInfo> _itemsBuilder;

            public Builder(SymbolKey assemblySymbolKey, Checksum checksum, string language, string genericTypeSuffix, EditorBrowsableInfo editorBrowsableInfo)
            {
                _assemblySymbolKey = assemblySymbolKey;
                _checksum = checksum;
                _language = language;
                _genericTypeSuffix = genericTypeSuffix;
                _editorBrowsableInfo = editorBrowsableInfo;

                _itemsBuilder = ArrayBuilder<TypeImportCompletionItemInfo>.GetInstance();
            }

            public TypeImportCompletionCacheEntry ToReferenceCacheEntry()
            {
                return new TypeImportCompletionCacheEntry(
                    _assemblySymbolKey,
                    _checksum,
                    _language,
                    _itemsBuilder.ToImmutable(),
                    _publicItemCount);
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

                if (isPublic)
                    _publicItemCount++;

                _itemsBuilder.Add(new TypeImportCompletionItemInfo(item, isPublic, isGeneric, isAttribute, isEditorBrowsableStateAdvanced));
            }

            public void Dispose()
                => _itemsBuilder.Free();
        }

        private readonly struct TypeImportCompletionItemInfo
        {
            private readonly ItemPropertyKind _properties;

            public TypeImportCompletionItemInfo(CompletionItem item, bool isPublic, bool isGeneric, bool isAttribute, bool isEditorBrowsableStateAdvanced)
            {
                Item = item;
                _properties = (isPublic ? ItemPropertyKind.IsPublic : 0)
                            | (isGeneric ? ItemPropertyKind.IsGeneric : 0)
                            | (isAttribute ? ItemPropertyKind.IsAttribute : 0)
                            | (isEditorBrowsableStateAdvanced ? ItemPropertyKind.IsEditorBrowsableStateAdvanced : 0);
            }

            public CompletionItem Item { get; }

            public bool IsPublic
                => (_properties & ItemPropertyKind.IsPublic) != 0;

            public bool IsGeneric
                => (_properties & ItemPropertyKind.IsGeneric) != 0;

            public bool IsAttribute
                => (_properties & ItemPropertyKind.IsAttribute) != 0;

            public bool IsEditorBrowsableStateAdvanced
                => (_properties & ItemPropertyKind.IsEditorBrowsableStateAdvanced) != 0;

            public TypeImportCompletionItemInfo WithItem(CompletionItem item)
                => new(item, IsPublic, IsGeneric, IsAttribute, IsEditorBrowsableStateAdvanced);

            [Flags]
            private enum ItemPropertyKind : byte
            {
                IsPublic = 0x1,
                IsGeneric = 0x2,
                IsAttribute = 0x4,
                IsEditorBrowsableStateAdvanced = 0x8,
            }
        }
    }
}
