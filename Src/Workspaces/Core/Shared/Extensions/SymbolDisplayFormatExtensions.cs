using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SymbolDisplayFormatExtensions
    {
        public static SymbolDisplayFormat WithMiscellaneousOptions(
            this SymbolDisplayFormat format, SymbolDisplayMiscellaneousOptions options)
        {
            return new SymbolDisplayFormat(
                format.GlobalNamespaceStyle,
                format.TypeQualificationStyle,
                format.GenericsOptions,
                format.MemberOptions,
                format.DelegateStyle,
                format.ExtensionMethodStyle,
                format.ParameterOptions,
                format.PropertyStyle,
                format.LocalOptions,
                format.KindOptions,
                options);
        }

        public static SymbolDisplayFormat AddMiscellaneousOptions(
            this SymbolDisplayFormat format, SymbolDisplayMiscellaneousOptions options)
        {
            return format.WithMiscellaneousOptions(format.MiscellaneousOptions | options);
        }

        public static SymbolDisplayFormat WithGenericsOptions(
            this SymbolDisplayFormat format, SymbolDisplayGenericsOptions options)
        {
            return new SymbolDisplayFormat(
                format.GlobalNamespaceStyle,
                format.TypeQualificationStyle,
                options,
                format.MemberOptions,
                format.DelegateStyle,
                format.ExtensionMethodStyle,
                format.ParameterOptions,
                format.PropertyStyle,
                format.LocalOptions,
                format.KindOptions,
                format.MiscellaneousOptions);
        }

        public static SymbolDisplayFormat AddGenericsOptions(
            this SymbolDisplayFormat format, SymbolDisplayGenericsOptions options)
        {
            return format.WithGenericsOptions(format.GenericsOptions | options);
        }

        public static SymbolDisplayFormat WithMemberOptions(
            this SymbolDisplayFormat format, SymbolDisplayMemberOptions options)
        {
            return new SymbolDisplayFormat(
                format.GlobalNamespaceStyle,
                format.TypeQualificationStyle,
                format.GenericsOptions,
                options,
                format.DelegateStyle,
                format.ExtensionMethodStyle,
                format.ParameterOptions,
                format.PropertyStyle,
                format.LocalOptions,
                format.KindOptions,
                format.MiscellaneousOptions);
        }

        public static SymbolDisplayFormat AddMemberOptions(
            this SymbolDisplayFormat format, SymbolDisplayMemberOptions options)
        {
            return format.WithMemberOptions(format.MemberOptions | options);
        }

        public static SymbolDisplayFormat RemoveMemberOptions(
            this SymbolDisplayFormat format, SymbolDisplayMemberOptions options)
        {
            return format.WithMemberOptions(format.MemberOptions & ~options);
        }

        public static SymbolDisplayFormat WithKindOptions(
            this SymbolDisplayFormat format, SymbolDisplayKindOptions options)
        {
            return new SymbolDisplayFormat(
                format.GlobalNamespaceStyle,
                format.TypeQualificationStyle,
                format.GenericsOptions,
                format.MemberOptions,
                format.DelegateStyle,
                format.ExtensionMethodStyle,
                format.ParameterOptions,
                format.PropertyStyle,
                format.LocalOptions,
                options,
                format.MiscellaneousOptions);
        }

        public static SymbolDisplayFormat AddKindOptions(
            this SymbolDisplayFormat format, SymbolDisplayKindOptions options)
        {
            return format.WithKindOptions(format.KindOptions | options);
        }

        public static SymbolDisplayFormat RemoveKindOptions(
            this SymbolDisplayFormat format, SymbolDisplayKindOptions options)
        {
            return format.WithKindOptions(format.KindOptions & ~options);
        }

        public static SymbolDisplayFormat WithParameterOptions(
            this SymbolDisplayFormat format, SymbolDisplayParameterOptions options)
        {
            return new SymbolDisplayFormat(
                format.GlobalNamespaceStyle,
                format.TypeQualificationStyle,
                format.GenericsOptions,
                format.MemberOptions,
                format.DelegateStyle,
                format.ExtensionMethodStyle,
                options,
                format.PropertyStyle,
                format.LocalOptions,
                format.KindOptions,
                format.MiscellaneousOptions);
        }

        public static SymbolDisplayFormat AddParameterOptions(
            this SymbolDisplayFormat format, SymbolDisplayParameterOptions options)
        {
            return format.WithParameterOptions(format.ParameterOptions | options);
        }

        public static SymbolDisplayFormat RemoveParameterOptions(
            this SymbolDisplayFormat format, SymbolDisplayParameterOptions options)
        {
            return format.WithParameterOptions(format.ParameterOptions & ~options);
        }

        public static SymbolDisplayFormat WithGlobalNamespaceStyle(
            this SymbolDisplayFormat format, SymbolDisplayGlobalNamespaceStyle style)
        {
            return new SymbolDisplayFormat(
                style,
                format.TypeQualificationStyle,
                format.GenericsOptions,
                format.MemberOptions,
                format.DelegateStyle,
                format.ExtensionMethodStyle,
                format.ParameterOptions,
                format.PropertyStyle,
                format.LocalOptions,
                format.KindOptions,
                format.MiscellaneousOptions);
        }

        public static SymbolDisplayFormat WithLocalOptions(
            this SymbolDisplayFormat format, SymbolDisplayLocalOptions options)
        {
            return new SymbolDisplayFormat(
                format.GlobalNamespaceStyle,
                format.TypeQualificationStyle,
                format.GenericsOptions,
                format.MemberOptions,
                format.DelegateStyle,
                format.ExtensionMethodStyle,
                format.ParameterOptions,
                format.PropertyStyle,
                options,
                format.KindOptions,
                format.MiscellaneousOptions);
        }

        public static SymbolDisplayFormat AddLocalOptions(
            this SymbolDisplayFormat format, SymbolDisplayLocalOptions options)
        {
            return format.WithLocalOptions(format.LocalOptions | options);
        }
    }
}
