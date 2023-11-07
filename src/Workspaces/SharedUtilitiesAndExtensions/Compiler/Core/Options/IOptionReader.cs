// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options;

internal interface IOptionsReader
{
    bool TryGetOption<T>(OptionKey2 optionKey, out T value);
}

internal sealed class AnalyzerConfigOptionsReader(AnalyzerConfigOptions options) : IOptionsReader
{
    public readonly AnalyzerConfigOptions Options = options;

    public bool TryGetOption<T>(OptionKey2 optionKey, out T value)
        => Options.TryGetEditorConfigOption(optionKey.Option, out value);
}

internal static partial class Extensions
{
    public static IOptionsReader GetOptionsReader(this AnalyzerConfigOptions configOptions)
        => configOptions as IOptionsReader ?? new AnalyzerConfigOptionsReader(configOptions);

    public static T GetOption<T>(this IOptionsReader options, Option2<T> option)
        => options.TryGetOption<T>(new OptionKey2(option), out var value) ? value! : option.DefaultValue;

    public static T GetOption<T>(this IOptionsReader options, Option2<T> option, T defaultValue)
        => options.TryGetOption<T>(new OptionKey2(option), out var value) ? value! : defaultValue;

    public static T GetOption<T>(this IOptionsReader options, PerLanguageOption2<T> option, string language)
        => options.TryGetOption<T>(new OptionKey2(option, language), out var value) ? value! : option.DefaultValue;

    public static T GetOption<T>(this IOptionsReader options, PerLanguageOption2<T> option, string language, T defaultValue)
        => options.TryGetOption<T>(new OptionKey2(option, language), out var value) ? value! : defaultValue;

    public static T GetOptionValue<T>(this IOptionsReader options, Option2<CodeStyleOption2<T>> option, T defaultValue)
        => options.TryGetOption<CodeStyleOption2<T>>(new OptionKey2(option), out var style) ? style!.Value : defaultValue;

    public static T GetOptionValue<T>(this IOptionsReader options, PerLanguageOption2<CodeStyleOption2<T>> option, string language, T defaultValue)
        => options.TryGetOption<CodeStyleOption2<T>>(new OptionKey2(option, language), out var style) ? style!.Value : defaultValue;
}
