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

        public CompletionItemFilter(Glyph glyph, char accessKey)
        {
            this.Glyph = glyph;
            this.AccessKey = accessKey;
        }

        public static readonly CompletionItemFilter NamespaceFilter = new CompletionItemFilter(Glyph.Namespace, 'n');
        public static readonly CompletionItemFilter ClassFilter = new CompletionItemFilter(Glyph.ClassPublic, 'c');
        public static readonly CompletionItemFilter ModuleFilter = new CompletionItemFilter(Glyph.ModulePublic, 'm');
        public static readonly CompletionItemFilter StructureFilter = new CompletionItemFilter(Glyph.StructurePublic, 's');
        public static readonly CompletionItemFilter InterfaceFilter = new CompletionItemFilter(Glyph.InterfacePublic, 'i');
        public static readonly CompletionItemFilter EnumFilter = new CompletionItemFilter(Glyph.EnumPublic, 'e');
        public static readonly CompletionItemFilter DelegateFilter = new CompletionItemFilter(Glyph.DelegatePublic, 'd');
        public static readonly CompletionItemFilter ConstantFilter = new CompletionItemFilter(Glyph.ConstantPublic, 'o');
        public static readonly CompletionItemFilter FieldFilter = new CompletionItemFilter(Glyph.FieldPublic, 'f');
        public static readonly CompletionItemFilter EventFilter = new CompletionItemFilter(Glyph.EventPublic, 'e');
        public static readonly CompletionItemFilter PropertyFilter = new CompletionItemFilter(Glyph.PropertyPublic, 'r');
        public static readonly CompletionItemFilter MethodFilter = new CompletionItemFilter(Glyph.MethodPublic, 'm');
        public static readonly CompletionItemFilter ExtensionMethodFilter = new CompletionItemFilter(Glyph.ExtensionMethodPublic, 'x');
        public static readonly CompletionItemFilter ParameterFilter = new CompletionItemFilter(Glyph.Parameter, 'p');
        public static readonly CompletionItemFilter LocalFilter = new CompletionItemFilter(Glyph.Local, 'l');

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
        public static readonly ImmutableArray<CompletionItemFilter> ParameterFilters = ImmutableArray.Create(ParameterFilter);
        public static readonly ImmutableArray<CompletionItemFilter> LocalFilters = ImmutableArray.Create(LocalFilter);

        public static ImmutableArray<CompletionItemFilter> AllFilters { get; } =
            ImmutableArray.Create(
                NamespaceFilter,
                ClassFilter,
                ModuleFilter,
                StructureFilter,
                InterfaceFilter,
                EnumFilter,
                DelegateFilter,
                ConstantFilter,
                FieldFilter,
                EventFilter,
                PropertyFilter,
                MethodFilter,
                ExtensionMethodFilter,
                ParameterFilter,
                LocalFilter);
    }
}
