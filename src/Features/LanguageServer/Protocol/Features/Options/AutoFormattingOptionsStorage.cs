// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class AutoFormattingOptionsStorage
{
    public static AutoFormattingOptions GetAutoFormattingOptions(this IGlobalOptionService globalOptions, string language)
        => new(
            FormatOnReturn: globalOptions.GetOption(FormatOnReturn, language),
            FormatOnTyping: globalOptions.GetOption(FormatOnTyping, language),
            FormatOnSemicolon: globalOptions.GetOption(FormatOnSemicolon, language),
            FormatOnCloseBrace: globalOptions.GetOption(FormatOnCloseBrace, language));

    internal static readonly PerLanguageOption2<bool> FormatOnReturn = new(
        "FormattingOptions", OptionGroup.Default, "AutoFormattingOnReturn", AutoFormattingOptions.Default.FormatOnReturn,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Return"));

    public static readonly PerLanguageOption2<bool> FormatOnTyping = new(
        "FormattingOptions", OptionGroup.Default, "AutoFormattingOnTyping", AutoFormattingOptions.Default.FormatOnTyping,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing"));

    public static readonly PerLanguageOption2<bool> FormatOnSemicolon = new(
        "FormattingOptions", OptionGroup.Default, "AutoFormattingOnSemicolon", AutoFormattingOptions.Default.FormatOnSemicolon,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon"));

    public static readonly PerLanguageOption2<bool> FormatOnCloseBrace = new(
        "BraceCompletionOptions", "AutoFormattingOnCloseBrace", defaultValue: AutoFormattingOptions.Default.FormatOnCloseBrace,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Close Brace"));
}
