// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Adornments;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    /// <summary>
    /// Provides an efficient way to compute a set of completion filters associated with a collection of completion items.
    /// Presence of expander and filter in the set have different meanings. Set contains a filter means the filter is
    /// available but unselected, whereas it means available and selected for an expander. Note that even though VS supports 
    /// having multiple expanders, we only support one.
    /// </summary>
    internal sealed class FilterSet
    {
        // Cache all the VS completion filters which essentially make them singletons.
        // Because all items that should be filtered using the same filter button must 
        // use the same reference to the instance of CompletionFilter.
        private static readonly ImmutableDictionary<string, FilterWithMask> s_filterMap;

        private BitVector32 _vector;
        private static readonly int s_expanderMask;

        public static readonly CompletionFilter NamespaceFilter;
        public static readonly CompletionFilter ClassFilter;
        public static readonly CompletionFilter ModuleFilter;
        public static readonly CompletionFilter StructureFilter;
        public static readonly CompletionFilter InterfaceFilter;
        public static readonly CompletionFilter EnumFilter;
        public static readonly CompletionFilter DelegateFilter;
        public static readonly CompletionFilter ConstantFilter;
        public static readonly CompletionFilter FieldFilter;
        public static readonly CompletionFilter EventFilter;
        public static readonly CompletionFilter PropertyFilter;
        public static readonly CompletionFilter MethodFilter;
        public static readonly CompletionFilter ExtensionMethodFilter;
        public static readonly CompletionFilter LocalAndParameterFilter;
        public static readonly CompletionFilter KeywordFilter;
        public static readonly CompletionFilter SnippetFilter;
        public static readonly CompletionFilter TargetTypedFilter;

        public static readonly CompletionExpander Expander;

        static FilterSet()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, FilterWithMask>();
            var previousMask = 0;

            NamespaceFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Namespaces, 'n', WellKnownTags.Namespace);
            ClassFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Classes, 'c', WellKnownTags.Class);
            ModuleFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Modules, 'u', WellKnownTags.Module);
            StructureFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Structures, 's', WellKnownTags.Structure);
            InterfaceFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Interfaces, 'i', WellKnownTags.Interface);
            EnumFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Enums, 'e', WellKnownTags.Enum);
            DelegateFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Delegates, 'd', WellKnownTags.Delegate);
            ConstantFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Constants, 'o', WellKnownTags.Constant);
            FieldFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Fields, 'f', WellKnownTags.Field);
            EventFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Events, 'v', WellKnownTags.Event);
            PropertyFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Properties, 'p', WellKnownTags.Property);
            MethodFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Methods, 'm', WellKnownTags.Method);
            ExtensionMethodFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Extension_methods, 'x', WellKnownTags.ExtensionMethod);
            LocalAndParameterFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Locals_and_parameters, 'l', WellKnownTags.Local, WellKnownTags.Parameter);
            KeywordFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Keywords, 'k', WellKnownTags.Keyword);
            SnippetFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Snippets, 't', WellKnownTags.Snippet);
            TargetTypedFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Target_type_matches, 'j', WellKnownTags.TargetTypeMatch);

            s_filterMap = builder.ToImmutable();

            s_expanderMask = BitVector32.CreateMask(previousMask);

            var addImageId = Shared.Extensions.GlyphExtensions.GetImageCatalogImageId(KnownImageIds.ExpandScope);

            Expander = new CompletionExpander(
                EditorFeaturesResources.Expander_display_text,
                accessKey: "a",
                new ImageElement(addImageId, EditorFeaturesResources.Expander_image_element));

            CompletionFilter CreateCompletionFilterAndAddToBuilder(string displayText, char accessKey, params string[] tags)
            {
                var filter = CreateCompletionFilter(displayText, tags, accessKey);
                previousMask = BitVector32.CreateMask(previousMask);

                foreach (var tag in tags)
                {
                    builder.Add(tag, new FilterWithMask(filter, previousMask));
                }

                return filter;
            }
        }

        private static CompletionFilter CreateCompletionFilter(
            string displayText, string[] tags, char accessKey)
        {
            var imageId = tags.ToImmutableArray().GetFirstGlyph().GetImageId();
            return new CompletionFilter(
                displayText,
                accessKey.ToString(),
                new ImageElement(new ImageId(imageId.Guid, imageId.Id), EditorFeaturesResources.Filter_image_element));
        }

        public FilterSet()
        {
            _vector = new BitVector32();
        }

        public (ImmutableArray<CompletionFilter> filters, int data) GetFiltersAndAddToSet(RoslynCompletionItem item)
        {
            var listBuilder = new ArrayBuilder<CompletionFilter>();
            var vectorForSingleItem = new BitVector32();

            if (item.Flags.IsExpanded())
            {
                listBuilder.Add(Expander);
                vectorForSingleItem[s_expanderMask] = _vector[s_expanderMask] = true;
            }

            foreach (var tag in item.Tags)
            {
                if (s_filterMap.TryGetValue(tag, out var filterWithMask))
                {
                    listBuilder.Add(filterWithMask.Filter);
                    vectorForSingleItem[filterWithMask.Mask] = _vector[filterWithMask.Mask] = true;
                }
            }

            return (listBuilder.ToImmutableAndFree(), vectorForSingleItem.Data);
        }

        // test only
        internal static List<CompletionFilter> GetFilters(RoslynCompletionItem item)
        {
            var result = new List<CompletionFilter>();

            foreach (var tag in item.Tags)
            {
                if (s_filterMap.TryGetValue(tag, out var filterWithMask))
                {
                    result.Add(filterWithMask.Filter);
                }
            }

            return result;
        }

        public void CombineData(int filterSetData)
        {
            _vector[filterSetData] = true;
        }

        public ImmutableArray<CompletionFilterWithState> GetFilterStatesInSet(bool addUnselectedExpander)
        {
            var builder = new ArrayBuilder<CompletionFilterWithState>();

            // An unselected expander is only added if `addUnselectedExpander == true` and the expander is not in the set.
            if (_vector[s_expanderMask])
            {
                builder.Add(new CompletionFilterWithState(Expander, isAvailable: true, isSelected: true));
            }
            else if (addUnselectedExpander)
            {
                builder.Add(new CompletionFilterWithState(Expander, isAvailable: true, isSelected: false));
            }

            foreach (var filterWithMask in s_filterMap.Values)
            {
                if (_vector[filterWithMask.Mask])
                {
                    builder.Add(new CompletionFilterWithState(filterWithMask.Filter, isAvailable: true, isSelected: false));
                }
            }

            return builder.ToImmutableAndFree();
        }

        private readonly struct FilterWithMask
        {
            public readonly CompletionFilter Filter;
            public readonly int Mask;

            public FilterWithMask(CompletionFilter filter, int mask)
            {
                Filter = filter;
                Mask = mask;
            }
        }
    }
}
