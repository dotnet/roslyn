// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class OptionsHelpers
    {
        public static T GetOption<T>(Option<T> option, Func<OptionKey, object?> getOption)
            => GetOption<T>(new OptionKey(option), getOption);

        public static T GetOption<T>(Option2<T> option, Func<OptionKey, object?> getOption)
            => GetOption<T>(new OptionKey(option), getOption);

        public static T GetOption<T>(PerLanguageOption<T> option, string? language, Func<OptionKey, object?> getOption)
            => GetOption<T>(new OptionKey(option, language), getOption);

        public static T GetOption<T>(PerLanguageOption2<T> option, string? language, Func<OptionKey, object?> getOption)
            => GetOption<T>(new OptionKey(option, language), getOption);

        public static T GetOption<T>(OptionKey2 optionKey, Func<OptionKey, object?> getOption)
            => GetOption<T>(new OptionKey(optionKey.Option, optionKey.Language), getOption);

        public static T GetOption<T>(OptionKey optionKey, Func<OptionKey, object?> getOption)
        {
            var value = getOption(optionKey);
            if (value is ICodeStyleOption codeStyleOption)
            {
                return (T)codeStyleOption.AsCodeStyleOption<T>();
            }

            return (T)value!;
        }

        public static object? GetPublicOption(OptionKey optionKey, Func<OptionKey, object?> getOption)
        {
            var value = getOption(optionKey);
            if (value is ICodeStyleOption codeStyleOption)
            {
                return codeStyleOption.AsPublicCodeStyleOption();
            }

            return value;
        }
    }
}
