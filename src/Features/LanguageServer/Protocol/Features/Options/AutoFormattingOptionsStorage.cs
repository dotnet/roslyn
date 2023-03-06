// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class AutoFormattingOptionsStorage
{
    public static AutoFormattingOptions GetAutoFormattingOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            FormatOnReturn = globalOptions.GetOption(FormatOnReturn, language),
            FormatOnTyping = globalOptions.GetOption(FormatOnTyping, language),
            FormatOnSemicolon = globalOptions.GetOption(FormatOnSemicolon, language),
            FormatOnCloseBrace = globalOptions.GetOption(FormatOnCloseBrace, language)
        };

    private static readonly OptionGroup s_formattingGroup = new(name: "formatting", description: "");

    internal static readonly PerLanguageOption2<bool> FormatOnReturn = new(
        "csharp_format_on_return", AutoFormattingOptions.Default.FormatOnReturn, group: s_formattingGroup);

    public static readonly PerLanguageOption2<bool> FormatOnTyping = new(
        "csharp_format_on_typing", AutoFormattingOptions.Default.FormatOnTyping, group: s_formattingGroup);

    public static readonly PerLanguageOption2<bool> FormatOnSemicolon = new(
        "csharp_format_on_semicolon", AutoFormattingOptions.Default.FormatOnSemicolon, group: s_formattingGroup);

    public static readonly PerLanguageOption2<bool> FormatOnCloseBrace = new(
        "csharp_format_on_close_brace", AutoFormattingOptions.Default.FormatOnCloseBrace, group: s_formattingGroup);
}
