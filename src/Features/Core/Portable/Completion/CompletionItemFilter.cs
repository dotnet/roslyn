using System.Collections.Immutable;

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
            foreach (var tag in this.Tags)
            {
                if (item.Tags.Contains(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public static readonly CompletionItemFilter NamespaceFilter = new CompletionItemFilter(FeaturesResources.Namespaces, CompletionTags.Namespace, 'n');
        public static readonly CompletionItemFilter ClassFilter = new CompletionItemFilter(FeaturesResources.Classes, CompletionTags.Class, 'c');
        public static readonly CompletionItemFilter ModuleFilter = new CompletionItemFilter(FeaturesResources.Modules, CompletionTags.Module, 'u');
        public static readonly CompletionItemFilter StructureFilter = new CompletionItemFilter(FeaturesResources.Structures, CompletionTags.Structure, 's');
        public static readonly CompletionItemFilter InterfaceFilter = new CompletionItemFilter(FeaturesResources.Interfaces, CompletionTags.Interface, 'i');
        public static readonly CompletionItemFilter EnumFilter = new CompletionItemFilter(FeaturesResources.Enums, CompletionTags.Enum, 'e');
        public static readonly CompletionItemFilter DelegateFilter = new CompletionItemFilter(FeaturesResources.Delegates, CompletionTags.Delegate, 'd');
        public static readonly CompletionItemFilter ConstantFilter = new CompletionItemFilter(FeaturesResources.Constants, CompletionTags.Constant, 'o');
        public static readonly CompletionItemFilter FieldFilter = new CompletionItemFilter(FeaturesResources.Fields, CompletionTags.Field, 'f');
        public static readonly CompletionItemFilter EventFilter = new CompletionItemFilter(FeaturesResources.Events, CompletionTags.Event, 'v');
        public static readonly CompletionItemFilter PropertyFilter = new CompletionItemFilter(FeaturesResources.Properties, CompletionTags.Property, 'p');
        public static readonly CompletionItemFilter MethodFilter = new CompletionItemFilter(FeaturesResources.Methods, CompletionTags.Method, 'm');
        public static readonly CompletionItemFilter ExtensionMethodFilter = new CompletionItemFilter(FeaturesResources.Extension_methods, CompletionTags.ExtensionMethod, 'x');
        public static readonly CompletionItemFilter LocalAndParameterFilter = new CompletionItemFilter(FeaturesResources.Locals_and_parameters, ImmutableArray.Create(CompletionTags.Local, CompletionTags.Parameter), 'l');

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
                NamespaceFilter);
    }
}
