// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;

public abstract partial class AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    internal static (OptionKey2, object) SingleOption<T>(Option2<T> option, T enabled)
        => (new OptionKey2(option), enabled);

    internal (OptionKey2, object) SingleOption<T>(PerLanguageOption2<T> option, T value)
        => (new OptionKey2(option, this.GetLanguage()), value);

    internal static (OptionKey2, object) SingleOption<T>(Option2<CodeStyleOption2<T>> option, T enabled, NotificationOption2 notification)
        => SingleOption(option, new CodeStyleOption2<T>(enabled, notification));

    internal static (OptionKey2, object) SingleOption<T>(Option2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
        => (new OptionKey2(option), codeStyle);

    internal (OptionKey2, object) SingleOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T enabled, NotificationOption2 notification)
        => SingleOption(option, new CodeStyleOption2<T>(enabled, notification));

    internal (OptionKey2, object) SingleOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
        => SingleOption(option, codeStyle, language: GetLanguage());

    internal static (OptionKey2, object) SingleOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle, string language)
        => (new OptionKey2(option, language), codeStyle);

    internal OptionsCollection Option<T>(Option2<CodeStyleOption2<T>> option, T enabled, NotificationOption2 notification)
        => new(GetLanguage()) { { option, enabled, notification } };

    internal OptionsCollection Option<T>(Option2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
        => new(GetLanguage()) { { option, codeStyle } };

    internal OptionsCollection Option<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T enabled, NotificationOption2 notification)
        => new(GetLanguage()) { { option, enabled, notification } };

    internal OptionsCollection Option<T>(PerLanguageOption2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
        => new(GetLanguage()) { { option, codeStyle } };

    internal OptionsCollection Option<T>(Option2<T> option, T value)
        => new(GetLanguage()) { { option, value } };

    internal OptionsCollection Option<T>(PerLanguageOption2<T> option, T value)
        => new(GetLanguage()) { { option, value } };
}
