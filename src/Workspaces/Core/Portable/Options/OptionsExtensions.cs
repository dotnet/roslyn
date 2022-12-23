// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: Option<T>, PerLanguageOption<T>

using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class OptionsExtensions
    {
        public static Option<T> ToPublicOption<T>(this Option2<T> option, string feature, string name)
            => new(option.OptionDefinition, feature, name, ((IOption2)option).StorageLocations);

        public static Option<CodeStyleOption<T>> ToPublicOption<T>(this Option2<CodeStyleOption2<T>> option, string feature, string name)
            => new(new OptionDefinition(
                option.OptionDefinition.Group,
                option.OptionDefinition.ConfigName,
                defaultValue: new CodeStyleOption<T>(option.DefaultValue),
                option.OptionDefinition.Type,
                option.OptionDefinition.IsEditorConfigOption),
                feature,
                name,
                ((IOption2)option).StorageLocations);

        public static PerLanguageOption<T> ToPublicOption<T>(this PerLanguageOption2<T> option, string feature, string name)
            => new(option.OptionDefinition, feature, name, ((IOption2)option).StorageLocations);

        public static PerLanguageOption<CodeStyleOption<T>> ToPublicOption<T>(this PerLanguageOption2<CodeStyleOption2<T>> option, string feature, string name)
            => new(new OptionDefinition(
                option.OptionDefinition.Group,
                option.OptionDefinition.ConfigName,
                defaultValue: new CodeStyleOption<T>(option.DefaultValue),
                option.OptionDefinition.Type,
                option.OptionDefinition.IsEditorConfigOption),
                feature,
                name,
                ((IOption2)option).StorageLocations);


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
    }
}
