// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class OptionsExtensions
    {
        public static Option<CodeStyleOption<T>> ToPublicOption<T>(this Option2<CodeStyleOption2<T>> option)
        {
            RoslynDebug.Assert(option != null);

            var codeStyleOption = new CodeStyleOption<T>(option.DefaultValue);
            var optionDefinition = new OptionDefinition(option.Feature, option.Group, option.Name,
                defaultValue: codeStyleOption, type: typeof(CodeStyleOption<T>), isPerLanguage: false);
            return new Option<CodeStyleOption<T>>(optionDefinition, option.StorageLocations.As<OptionStorageLocation>());
        }

        public static PerLanguageOption<CodeStyleOption<T>> ToPublicOption<T>(this PerLanguageOption2<CodeStyleOption2<T>> option)
        {
            RoslynDebug.Assert(option != null);

            var codeStyleOption = new CodeStyleOption<T>(option.DefaultValue);
            var optionDefinition = new OptionDefinition(option.Feature, option.Group, option.Name,
                defaultValue: codeStyleOption, type: typeof(CodeStyleOption<T>), isPerLanguage: true);
            return new PerLanguageOption<CodeStyleOption<T>>(optionDefinition, option.StorageLocations.As<OptionStorageLocation>());
        }
    }
}
