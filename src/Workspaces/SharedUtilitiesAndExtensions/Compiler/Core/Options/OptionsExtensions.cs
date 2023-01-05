// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

internal static class OptionsExtensions
{
#if CODE_STYLE
#pragma warning disable IDE0060 // Remove unused parameter

    // Stubs to avoid #ifdefs at call sites.

    public static Option2<T> WithPublicOption<T, TPublicValue>(this Option2<T> option, string feature, string name, Func<T, TPublicValue> toPublicValue)
        => option;

    public static PerLanguageOption2<T> WithPublicOption<T, TPublicValue>(this PerLanguageOption2<T> option, string feature, string name, Func<T, TPublicValue> toPublicValue)
        => option;

    public static Option2<T> WithPublicOption<T>(this Option2<T> option, string feature, string name)
        => option;

    public static Option2<CodeStyleOption2<T>> WithPublicOption<T>(this Option2<CodeStyleOption2<T>> option, string feature, string name)
        => option;

    public static PerLanguageOption2<T> WithPublicOption<T>(this PerLanguageOption2<T> option, string feature, string name)
        => option;

    public static PerLanguageOption2<CodeStyleOption2<T>> WithPublicOption<T>(this PerLanguageOption2<CodeStyleOption2<T>> option, string feature, string name)
        => option;

#pragma warning restore
#else
#pragma warning disable RS0030 // Do not used banned APIs: Option<T>, PerLanguageOption<T>

    public static Option2<T> WithPublicOption<T, TPublicValue>(this Option2<T> option, string feature, string name, Func<T, TPublicValue> toPublicValue)
        => new(
            option.OptionDefinition,
            option.LanguageName,
            publicOption: new Option<TPublicValue>(
                // public option instances do not need to be serialized to editorconfig
                option.OptionDefinition.WithDefaultValue(toPublicValue(option.DefaultValue), EditorConfigStorageLocation<TPublicValue>.Unsupported),
                feature,
                name,
                ImmutableArray<OptionStorageLocation>.Empty));

    public static PerLanguageOption2<T> WithPublicOption<T, TPublicValue>(this PerLanguageOption2<T> option, string feature, string name, Func<T, TPublicValue> toPublicValue)
        => new(
            option.OptionDefinition,
            publicOption: new PerLanguageOption<TPublicValue>(
                // public option instances do not need to be serialized to editorconfig
                option.OptionDefinition.WithDefaultValue(toPublicValue(option.DefaultValue), EditorConfigStorageLocation<TPublicValue>.Unsupported),
                feature,
                name,
                ImmutableArray<OptionStorageLocation>.Empty));

    public static Option2<T> WithPublicOption<T>(this Option2<T> option, string feature, string name)
        => WithPublicOption(option, feature, name, static value => value);

    public static Option2<CodeStyleOption2<T>> WithPublicOption<T>(this Option2<CodeStyleOption2<T>> option, string feature, string name)
       => WithPublicOption(option, feature, name, static value => new CodeStyleOption<T>(value));

    public static PerLanguageOption2<T> WithPublicOption<T>(this PerLanguageOption2<T> option, string feature, string name)
        => WithPublicOption(option, feature, name, static value => value);

    public static PerLanguageOption2<CodeStyleOption2<T>> WithPublicOption<T>(this PerLanguageOption2<CodeStyleOption2<T>> option, string feature, string name)
        => WithPublicOption(option, feature, name, static value => new CodeStyleOption<T>(value));

    public static Option<T> ToPublicOption<T>(this Option2<T> option)
    {
        Contract.ThrowIfNull(option.PublicOption);
        return (Option<T>)option.PublicOption;
    }

    public static PerLanguageOption<T> ToPublicOption<T>(this PerLanguageOption2<T> option)
    {
        Contract.ThrowIfNull(option.PublicOption);
        return (PerLanguageOption<T>)option.PublicOption;
    }

    public static Option<CodeStyleOption<T>> ToPublicOption<T>(this Option2<CodeStyleOption2<T>> option)
    {
        Contract.ThrowIfNull(option.PublicOption);
        return (Option<CodeStyleOption<T>>)option.PublicOption;
    }

    public static PerLanguageOption<CodeStyleOption<T>> ToPublicOption<T>(this PerLanguageOption2<CodeStyleOption2<T>> option)
    {
        Contract.ThrowIfNull(option.PublicOption);
        return (PerLanguageOption<CodeStyleOption<T>>)option.PublicOption;
    }

    // The following are used only to implement equality/ToString of public Option<T> and PerLanguageOption<T> options.
    // Public options can be instantiated with non-unique config name and thus we need to include default value in the equality
    // to avoid collisions among them.

    public static string PublicOptionDefinitionToString(this IOption2 option)
        => $"{option.Feature} - {option.Name}";

    public static bool PublicOptionDefinitionEquals(this IOption2 x, IOption2 y)
    {
        var equals = x.OptionDefinition.ConfigName == y.OptionDefinition.ConfigName && x.OptionDefinition.Group == y.OptionDefinition.Group;

        // DefaultValue and Type can differ between different but equivalent implementations of "ICodeStyleOption".
        // So, we skip these fields for equality checks of code style options.
        if (equals && x.DefaultValue is not ICodeStyleOption)
        {
            equals = Equals(x.DefaultValue, y.DefaultValue) && x.Type == y.Type;
        }

        return equals;
    }

#pragma warning restore
#endif
}
