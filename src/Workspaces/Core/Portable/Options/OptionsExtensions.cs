// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: Option<T>, PerLanguageOption<T>

using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class OptionsExtensions
    {
        public static Option<T> ToPublicOption<T>(this Option2<T> option)
            => new(option.OptionDefinition, ((IOption2)option).StorageLocations);

        public static Option<CodeStyleOption<T>> ToPublicOption<T>(this Option2<CodeStyleOption2<T>> option)
            => new(
                option.OptionDefinition.Feature,
                option.OptionDefinition.Group,
                option.OptionDefinition.Name,
                defaultValue: new CodeStyleOption<T>(option.DefaultValue),
                ((IOption2)option).StorageLocations,
                option.OptionDefinition.IsEditorConfigOption);

        public static PerLanguageOption<T> ToPublicOption<T>(this PerLanguageOption2<T> option)
            => new(option.OptionDefinition, ((IOption2)option).StorageLocations);

        public static PerLanguageOption<CodeStyleOption<T>> ToPublicOption<T>(this PerLanguageOption2<CodeStyleOption2<T>> option)
            => new(
                option.OptionDefinition.Feature,
                option.OptionDefinition.Group,
                option.OptionDefinition.Name,
                defaultValue: new CodeStyleOption<T>(option.DefaultValue),
                ((IOption2)option).StorageLocations,
                option.OptionDefinition.IsEditorConfigOption);
    }
}
