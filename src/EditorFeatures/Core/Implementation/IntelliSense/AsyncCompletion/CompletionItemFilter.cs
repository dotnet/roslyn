// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Adornments;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal static class CompletionItemFilter
    {
        private static readonly ImmutableDictionary<string, CompletionFilter> allFilters;

        static CompletionItemFilter()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, CompletionFilter>();

            NamespaceFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Namespaces, WellKnownTags.Namespace, 'n', builder);
            ClassFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Classes, WellKnownTags.Class, 'c', builder);
            ModuleFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Modules, WellKnownTags.Module, 'u', builder);
            StructureFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Structures, WellKnownTags.Structure, 's', builder);
            InterfaceFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Interfaces, WellKnownTags.Interface, 'i', builder);
            EnumFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Enums, WellKnownTags.Enum, 'e', builder);
            DelegateFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Delegates, WellKnownTags.Delegate, 'd', builder);
            ConstantFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Constants, WellKnownTags.Constant, 'o', builder);
            FieldFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Fields, WellKnownTags.Field, 'f', builder);
            EventFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Events, WellKnownTags.Event, 'v', builder);
            PropertyFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Properties, WellKnownTags.Property, 'p', builder);
            MethodFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Methods, WellKnownTags.Method, 'm', builder);
            ExtensionMethodFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Extension_methods, WellKnownTags.ExtensionMethod, 'x', builder);
            LocalAndParameterFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Locals_and_parameters, ImmutableArray.Create(WellKnownTags.Local, WellKnownTags.Parameter), 'l', builder);
            KeywordFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Keywords, ImmutableArray.Create(WellKnownTags.Keyword), 'k', builder);
            SnippetFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Snippets, ImmutableArray.Create(WellKnownTags.Snippet), 't', builder);
            TargetTypedFilter = CreateCompletionFilterAndAddToBuilder(FeaturesResources.Target_type_matches, ImmutableArray.Create(WellKnownTags.TargetTypeMatch), 'j', builder);

            allFilters = builder.ToImmutable();
        }

        private static CompletionFilter CreateCompletionFilter(
            string displayText, ImmutableArray<string> tags, char accessKey)
        {
            var imageId = tags.GetFirstGlyph().GetImageId();
            return new CompletionFilter(
                displayText,
                accessKey.ToString(),
                new ImageElement(new ImageId(imageId.Guid, imageId.Id), EditorFeaturesResources.Filter_image_element));
        }

        private static CompletionFilter CreateCompletionFilterAndAddToBuilder(
            string displayText, ImmutableArray<string> tags, char accessKey,
            ImmutableDictionary<string, CompletionFilter>.Builder builder)
        {
            var filter = CreateCompletionFilter(displayText, tags, accessKey);
            foreach (var tag in tags)
            {
                builder.Add(tag, filter);
            }

            return filter;
        }

        private static CompletionFilter CreateCompletionFilterAndAddToBuilder(
            string displayText, string tag, char accessKey,
            ImmutableDictionary<string, CompletionFilter>.Builder builder)
            => CreateCompletionFilterAndAddToBuilder(displayText, ImmutableArray.Create(tag), accessKey, builder);

        public static ImmutableArray<CompletionFilter> GetFilters(RoslynCompletionItem item)
        {
            var result = ArrayBuilder<CompletionFilter>.GetInstance();
            foreach (var tag in item.Tags)
            {
                if (allFilters.TryGetValue(tag, out var filter))
                {
                    result.Add(filter);
                }
            }
            return result.ToImmutableAndFree();
        }

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
    }
}
