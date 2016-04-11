using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionItemFilter
    {
        public readonly Glyph Glyph;
        public readonly char AccessKey;
        public readonly string DisplayText;

        public CompletionItemFilter(string displayText, Glyph glyph, char accessKey)
        {
            DisplayText = displayText;
            Glyph = glyph;
            AccessKey = accessKey;
        }

        public static readonly CompletionItemFilter NamespaceFilter = new CompletionItemFilter(FeaturesResources.Namespaces, Glyph.Namespace, 'n');
        public static readonly CompletionItemFilter ClassFilter = new CompletionItemFilter(FeaturesResources.Classes, Glyph.ClassPublic, 'c');
        public static readonly CompletionItemFilter ModuleFilter = new CompletionItemFilter(FeaturesResources.Modules, Glyph.ModulePublic, 'u');
        public static readonly CompletionItemFilter StructureFilter = new CompletionItemFilter(FeaturesResources.Structures, Glyph.StructurePublic, 's');
        public static readonly CompletionItemFilter InterfaceFilter = new CompletionItemFilter(FeaturesResources.Interfaces, Glyph.InterfacePublic, 'i');
        public static readonly CompletionItemFilter EnumFilter = new CompletionItemFilter(FeaturesResources.Enums, Glyph.EnumPublic, 'e');
        public static readonly CompletionItemFilter DelegateFilter = new CompletionItemFilter(FeaturesResources.Delegates, Glyph.DelegatePublic, 'd');
        public static readonly CompletionItemFilter ConstantFilter = new CompletionItemFilter(FeaturesResources.Constants, Glyph.ConstantPublic, 'o');
        public static readonly CompletionItemFilter FieldFilter = new CompletionItemFilter(FeaturesResources.Fields, Glyph.FieldPublic, 'f');
        public static readonly CompletionItemFilter EventFilter = new CompletionItemFilter(FeaturesResources.Events, Glyph.EventPublic, 'v');
        public static readonly CompletionItemFilter PropertyFilter = new CompletionItemFilter(FeaturesResources.Properties, Glyph.PropertyPublic, 'p');
        public static readonly CompletionItemFilter MethodFilter = new CompletionItemFilter(FeaturesResources.Methods, Glyph.MethodPublic, 'm');
        public static readonly CompletionItemFilter ExtensionMethodFilter = new CompletionItemFilter(FeaturesResources.Extension_methods, Glyph.ExtensionMethodPublic, 'x');
        public static readonly CompletionItemFilter LocalAndParameterFilter = new CompletionItemFilter(FeaturesResources.Locals_and_parameters, Glyph.Parameter, 'l');

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
