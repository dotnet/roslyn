using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionItemFilter
    {
        public readonly ImmutableArray<string> Tags;
        public readonly char AccessKey;
        public readonly string DisplayText;

        public CompletionItemFilter(string displayText, ImmutableArray<string> tags, char accessKey)
        {
            DisplayText = displayText;
            Tags = tags;
            AccessKey = accessKey;
        }

        public CompletionItemFilter(string displayText, string tag, char accessKey)
            : this(displayText, ImmutableArray.Create(tag), accessKey)
        {
        }

        public bool Matches(CompletionItem item)
        {
            foreach (var tag in Tags)
            {
                if (item.Tags.Contains(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ShouldBeFilteredOutOfCompletionList(
            CompletionItem item,
            ImmutableDictionary<CompletionItemFilter, bool> filterState)
        {
            if (filterState == null)
            {
                // No filtering.  The item is not filtered out.
                return false;
            }

            foreach (var filter in AllFilters)
            {
                // only consider filters that match the item
                var matches = filter.Matches(item);
                if (matches)
                {
                    // if the specific filter is enabled then it is not filtered out
                    if (filterState.TryGetValue(filter, out var enabled) && enabled)
                    {
                        return false;
                    }
                }
            }

            // The item was filtered out.
            return true;
        }

        public static readonly CompletionItemFilter NamespaceFilter = new CompletionItemFilter(FeaturesResources.Namespaces, WellKnownTags.Namespace, 'n');
        public static readonly CompletionItemFilter ClassFilter = new CompletionItemFilter(FeaturesResources.Classes, WellKnownTags.Class, 'c');
        public static readonly CompletionItemFilter ModuleFilter = new CompletionItemFilter(FeaturesResources.Modules, WellKnownTags.Module, 'u');
        public static readonly CompletionItemFilter StructureFilter = new CompletionItemFilter(FeaturesResources.Structures, WellKnownTags.Structure, 's');
        public static readonly CompletionItemFilter InterfaceFilter = new CompletionItemFilter(FeaturesResources.Interfaces, WellKnownTags.Interface, 'i');
        public static readonly CompletionItemFilter EnumFilter = new CompletionItemFilter(FeaturesResources.Enums, WellKnownTags.Enum, 'e');
        public static readonly CompletionItemFilter DelegateFilter = new CompletionItemFilter(FeaturesResources.Delegates, WellKnownTags.Delegate, 'd');
        public static readonly CompletionItemFilter ConstantFilter = new CompletionItemFilter(FeaturesResources.Constants, WellKnownTags.Constant, 'o');
        public static readonly CompletionItemFilter FieldFilter = new CompletionItemFilter(FeaturesResources.Fields, WellKnownTags.Field, 'f');
        public static readonly CompletionItemFilter EventFilter = new CompletionItemFilter(FeaturesResources.Events, WellKnownTags.Event, 'v');
        public static readonly CompletionItemFilter PropertyFilter = new CompletionItemFilter(FeaturesResources.Properties, WellKnownTags.Property, 'p');
        public static readonly CompletionItemFilter MethodFilter = new CompletionItemFilter(FeaturesResources.Methods, WellKnownTags.Method, 'm');
        public static readonly CompletionItemFilter ExtensionMethodFilter = new CompletionItemFilter(FeaturesResources.Extension_methods, WellKnownTags.ExtensionMethod, 'x');
        public static readonly CompletionItemFilter LocalAndParameterFilter = new CompletionItemFilter(FeaturesResources.Locals_and_parameters, ImmutableArray.Create(WellKnownTags.Local, WellKnownTags.Parameter), 'l');
        public static readonly CompletionItemFilter KeywordFilter = new CompletionItemFilter(FeaturesResources.Keywords, ImmutableArray.Create(WellKnownTags.Keyword), 'k');
        public static readonly CompletionItemFilter SnippetFilter = new CompletionItemFilter(FeaturesResources.Snippets, ImmutableArray.Create(WellKnownTags.Snippet), 't');
        public static readonly CompletionItemFilter TargetTypedFilter = new CompletionItemFilter(FeaturesResources.Target_type_matches, ImmutableArray.Create(WellKnownTags.TargetTypeMatch), 'j');

        public static readonly ImmutableArray<CompletionItemFilter> NamespaceFilters = ImmutableArray.Create(NamespaceFilter);
        public static readonly ImmutableArray<CompletionItemFilter> ClassFilters = ImmutableArray.Create(ClassFilter);
        public static readonly ImmutableArray<CompletionItemFilter> ModuleFilters = ImmutableArray.Create(ModuleFilter);
        public static readonly ImmutableArray<CompletionItemFilter> StructureFilters = ImmutableArray.Create(StructureFilter);
        public static readonly ImmutableArray<CompletionItemFilter> InterfaceFilters = ImmutableArray.Create(InterfaceFilter);
        public static readonly ImmutableArray<CompletionItemFilter> EnumFilters = ImmutableArray.Create(EnumFilter);
        public static readonly ImmutableArray<CompletionItemFilter> DelegateFilters = ImmutableArray.Create(DelegateFilter);
        public static readonly ImmutableArray<CompletionItemFilter> ConstantFilters = ImmutableArray.Create(ConstantFilter);
        public static readonly ImmutableArray<CompletionItemFilter> FieldFilters = ImmutableArray.Create(FieldFilter);
        public static readonly ImmutableArray<CompletionItemFilter> EventFilters = ImmutableArray.Create(EventFilter);
        public static readonly ImmutableArray<CompletionItemFilter> PropertyFilters = ImmutableArray.Create(PropertyFilter);
        public static readonly ImmutableArray<CompletionItemFilter> MethodFilters = ImmutableArray.Create(MethodFilter);
        public static readonly ImmutableArray<CompletionItemFilter> ExtensionMethodFilters = ImmutableArray.Create(ExtensionMethodFilter);
        public static readonly ImmutableArray<CompletionItemFilter> LocalAndParameterFilters = ImmutableArray.Create(LocalAndParameterFilter);
        public static readonly ImmutableArray<CompletionItemFilter> KeywordFilters = ImmutableArray.Create(KeywordFilter);
        public static readonly ImmutableArray<CompletionItemFilter> SnippetFilters = ImmutableArray.Create(SnippetFilter);
        public static readonly ImmutableArray<CompletionItemFilter> TargetTypedFilters = ImmutableArray.Create(TargetTypedFilter);

        public static ImmutableArray<CompletionItemFilter> AllFilters { get; } =
            ImmutableArray.Create(
                LocalAndParameterFilter,
                ConstantFilter,
                PropertyFilter,
                EventFilter,
                FieldFilter,
                MethodFilter,
                ExtensionMethodFilter,
                InterfaceFilter,
                ClassFilter,
                ModuleFilter,
                StructureFilter,
                EnumFilter,
                DelegateFilter,
                NamespaceFilter,
                KeywordFilter,
                SnippetFilter,
                TargetTypedFilter);
    }
}
