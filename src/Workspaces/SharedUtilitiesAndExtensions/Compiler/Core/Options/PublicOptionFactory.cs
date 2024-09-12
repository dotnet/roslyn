// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

internal static class PublicOptionFactory
{
#if CODE_STYLE
#pragma warning disable IDE0060 // Remove unused parameter

    // Stubs to avoid #ifdefs at call sites.

    public static Option2<T> WithPublicOption<T, TPublicValue>(this Option2<T> option, string feature, string name, Func<T, TPublicValue> toPublicValue, Func<TPublicValue, T> toInternalValue)
        => option;

    public static PerLanguageOption2<T> WithPublicOption<T, TPublicValue>(this PerLanguageOption2<T> option, string feature, string name, Func<T, TPublicValue> toPublicValue, Func<TPublicValue, T> toInternalValue)
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
    private sealed class StorageMapping(IOption2 internalOption, Func<object?, object?> toPublicValue, Func<object?, object?> toInternalValue) : OptionStorageMapping(internalOption)
    {
        public override object? ToPublicOptionValue(object? internalValue)
            => toPublicValue(internalValue);

        public override object? UpdateInternalOptionValue(object? currentInternalValue, object? newPublicValue)
            => toInternalValue(newPublicValue);
    }

    private static OptionDefinition<TPublicValue> ToPublicOptionDefinition<T, TPublicValue>(this OptionDefinition<T> definition, IOption2 internalOption, Func<T, TPublicValue> toPublicValue, Func<TPublicValue, T> toInternalValue)
        => new(
            toPublicValue(definition.DefaultValue),
            serializer: EditorConfigValueSerializer<TPublicValue>.Unsupported, // public option instances do not need to be serialized to editorconfig
            definition.Group,
            definition.ConfigName,
            new StorageMapping(internalOption, value => toPublicValue((T)value!), value => toInternalValue((TPublicValue)value!)),
            definition.IsEditorConfigOption);

    public static Option2<T> WithPublicOption<T, TPublicValue>(this Option2<T> option, string feature, string name, Func<T, TPublicValue> toPublicValue, Func<TPublicValue, T> toInternalValue)
        => new(
            option.Definition,
            option.LanguageName,
            publicOptionFactory: internalOption => new Option<TPublicValue>(
                option.Definition.ToPublicOptionDefinition(internalOption, toPublicValue, toInternalValue),
                feature,
                name,
                ImmutableArray<OptionStorageLocation>.Empty));

    public static PerLanguageOption2<T> WithPublicOption<T, TPublicValue>(this PerLanguageOption2<T> option, string feature, string name, Func<T, TPublicValue> toPublicValue, Func<TPublicValue, T> toInternalValue)
        => new(
            option.Definition,
            publicOptionFactory: internalOption => new PerLanguageOption<TPublicValue>(
                option.Definition.ToPublicOptionDefinition(internalOption, toPublicValue, toInternalValue),
                feature,
                name,
                ImmutableArray<OptionStorageLocation>.Empty));

    public static Option2<T> WithPublicOption<T>(this Option2<T> option, string feature, string name)
        => WithPublicOption(option, feature, name, static value => value, static value => value);

    public static Option2<CodeStyleOption2<T>> WithPublicOption<T>(this Option2<CodeStyleOption2<T>> option, string feature, string name)
       => WithPublicOption(option, feature, name, static value => new CodeStyleOption<T>(value), static value => value.UnderlyingOption);

    public static PerLanguageOption2<T> WithPublicOption<T>(this PerLanguageOption2<T> option, string feature, string name)
        => WithPublicOption(option, feature, name, static value => value, static value => value);

    public static PerLanguageOption2<CodeStyleOption2<T>> WithPublicOption<T>(this PerLanguageOption2<CodeStyleOption2<T>> option, string feature, string name)
        => WithPublicOption(option, feature, name, static value => new CodeStyleOption<T>(value), static value => value.UnderlyingOption);

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
        var equals = x.Definition.ConfigName == y.Definition.ConfigName && x.Definition.Group == y.Definition.Group;

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
