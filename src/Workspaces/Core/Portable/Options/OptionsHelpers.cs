// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class OptionsHelpers
    {
        public static T GetOption<T>(Option2<T> option, Func<OptionKey2, object?> getOption)
            => GetOption<T>(new OptionKey2(option), getOption);

        public static T GetOption<T>(PerLanguageOption2<T> option, string language, Func<OptionKey2, object?> getOption)
            => GetOption<T>(new OptionKey2(option, language), getOption);

        public static T GetOption<T>(OptionKey2 optionKey, Func<OptionKey2, object?> getOption)
        {
            var value = getOption(optionKey);
            if (value is ICodeStyleOption codeStyleOption)
            {
                return (T)codeStyleOption.AsCodeStyleOption<T>();
            }

            return (T)value!;
        }
    }
}
