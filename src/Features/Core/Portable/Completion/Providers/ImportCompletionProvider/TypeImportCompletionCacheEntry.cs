// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.Shared.Utilities.EditorBrowsableHelpers;

namespace Microsoft.CodeAnalysis.Completion.Providers;

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

    /// <summary>
    /// Only 1 entry (which corresponds to `System` namespace) can have items,
    /// suitable for enum's base list. This flag allows to fast-skip other entries
    /// without need to enumerate their items
    /// </summary>
    private bool HasEnumBaseTypes { get; }

    private TypeImportCompletionCacheEntry(
        SymbolKey assemblySymbolKey,
        Checksum checksum,
        string language,
        ImmutableArray<TypeImportCompletionItemInfo> items,
        int publicItemCount,
        bool hasEnumBaseTypes)
    {
        AssemblySymbolKey = assemblySymbolKey;
        Checksum = checksum;
        Language = language;

        ItemInfos = items;
        PublicItemCount = publicItemCount;
        HasEnumBaseTypes = hasEnumBaseTypes;
    }

    public ImmutableArray<CompletionItem> GetItemsForContext(
        Compilation originCompilation,
        string language,
        string genericTypeSuffix,
        bool isAttributeContext,
        bool isEnumBaseListContext,
        bool isCaseSensitive,
        bool hideAdvancedMembers)
    {
        if (AssemblySymbolKey.Resolve(originCompilation).Symbol is not IAssemblySymbol assemblySymbol)
            return [];

        if (isEnumBaseListContext && !HasEnumBaseTypes)
            return [];

        var isSameLanguage = Language == language;
        var isInternalsVisible = originCompilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assemblySymbol);
        using var _ = ArrayBuilder<CompletionItem>.GetInstance(out var builder);

        // PERF: try set the capacity upfront to avoid allocation from Resize
        if (!isAttributeContext && !isEnumBaseListContext)
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

            // Skip item if not suitable for enum base list
            if (isEnumBaseListContext && !info.IsEnumBaseType)
            {
                continue;
            }

            // C# and VB the display text is different for generics, i.e. <T> and (Of T). For simplicity, we only cache for one language.
            // But when we trigger in a project with different language than when the cache entry was created for, we will need to
            // change the generic suffix accordingly.
            if (!isSameLanguage && info.IsGeneric)
            {
                // We don't want to cache this item.
                item = ImportCompletionItem.CreateItemWithGenericDisplaySuffix(item, genericTypeSuffix);
            }

            builder.Add(item);
        }

        return builder.ToImmutableAndClear();

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

    public class Builder(SymbolKey assemblySymbolKey, Checksum checksum, string language, string genericTypeSuffix, EditorBrowsableInfo editorBrowsableInfo) : IDisposable
    {
        private readonly SymbolKey _assemblySymbolKey = assemblySymbolKey;
        private readonly string _language = language;
        private readonly string _genericTypeSuffix = genericTypeSuffix;
        private readonly Checksum _checksum = checksum;
        private readonly EditorBrowsableInfo _editorBrowsableInfo = editorBrowsableInfo;

        private int _publicItemCount;
        private bool _hasEnumBaseTypes;

        private readonly ArrayBuilder<TypeImportCompletionItemInfo> _itemsBuilder = ArrayBuilder<TypeImportCompletionItemInfo>.GetInstance();

        public TypeImportCompletionCacheEntry ToReferenceCacheEntry()
        {
            return new TypeImportCompletionCacheEntry(
                _assemblySymbolKey,
                _checksum,
                _language,
                _itemsBuilder.ToImmutable(),
                _publicItemCount,
                _hasEnumBaseTypes);
        }

        public void AddItem(INamedTypeSymbol symbol, string containingNamespace, bool isPublic)
        {
            // We want to cache items with EditorBrowsableState == Advanced regardless of current "hide adv members" option value
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

            var isEnumBaseType = symbol.SpecialType is >= SpecialType.System_SByte and <= SpecialType.System_UInt64;
            _hasEnumBaseTypes |= isEnumBaseType;

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

            _itemsBuilder.Add(new TypeImportCompletionItemInfo(item, isPublic, isGeneric, isAttribute, isEditorBrowsableStateAdvanced, isEnumBaseType));
        }

        public void Dispose()
            => _itemsBuilder.Free();
    }

    private readonly struct TypeImportCompletionItemInfo(CompletionItem item, bool isPublic, bool isGeneric, bool isAttribute, bool isEditorBrowsableStateAdvanced, bool isEnumBaseType)
    {
        private readonly ItemPropertyKind _properties = (isPublic ? ItemPropertyKind.IsPublic : 0)
                        | (isGeneric ? ItemPropertyKind.IsGeneric : 0)
                        | (isAttribute ? ItemPropertyKind.IsAttribute : 0)
                        | (isEnumBaseType ? ItemPropertyKind.IsEnumBaseType : 0)
                        | (isEditorBrowsableStateAdvanced ? ItemPropertyKind.IsEditorBrowsableStateAdvanced : 0);

        public CompletionItem Item { get; } = item;

        public bool IsPublic
            => (_properties & ItemPropertyKind.IsPublic) != 0;

        public bool IsGeneric
            => (_properties & ItemPropertyKind.IsGeneric) != 0;

        public bool IsAttribute
            => (_properties & ItemPropertyKind.IsAttribute) != 0;

        public bool IsEnumBaseType
            => (_properties & ItemPropertyKind.IsEnumBaseType) != 0;

        public bool IsEditorBrowsableStateAdvanced
            => (_properties & ItemPropertyKind.IsEditorBrowsableStateAdvanced) != 0;

        [Flags]
        private enum ItemPropertyKind : byte
        {
            IsPublic = 1,
            IsGeneric = 2,
            IsAttribute = 4,
            IsEnumBaseType = 8,
            IsEditorBrowsableStateAdvanced = 16
        }
    }
}
