// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics
{
    public abstract class AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest : AbstractDiagnosticProviderBasedUserDiagnosticTest
    {
        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
#if CODE_STYLE
        internal (OptionKey2, object) SingleOption<T>(Option2<T> option, T enabled)
            => (new OptionKey2(option), enabled);

        private protected (OptionKey2, object) SingleOption<T>(PerLanguageOption2<T> option, T value)
            => (new OptionKey2(option, this.GetLanguage()), value);

        private protected (OptionKey2, object) SingleOption<T>(Option2<CodeStyleOption2<T>> option, T enabled, NotificationOption2 notification)
            => SingleOption(option, new CodeStyleOption2<T>(enabled, notification));

        private protected (OptionKey2, object) SingleOption<T>(Option2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
            => (new OptionKey2(option), codeStyle);

        private protected (OptionKey2, object) SingleOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T enabled, NotificationOption2 notification)
            => SingleOption(option, new CodeStyleOption2<T>(enabled, notification));

        private protected (OptionKey2, object) SingleOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
            => SingleOption(option, codeStyle, language: GetLanguage());

        private protected static (OptionKey2, object) SingleOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle, string language)
            => (new OptionKey2(option, language), codeStyle);

        private protected IOptionsCollection Option<T>(Option2<CodeStyleOption2<T>> option, T enabled, NotificationOption2 notification)
            => OptionsSet(SingleOption(option, enabled, notification));

        private protected IOptionsCollection Option<T>(Option2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
            => OptionsSet(SingleOption(option, codeStyle));

        private protected IOptionsCollection Option<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T enabled, NotificationOption2 notification)
            => OptionsSet(SingleOption(option, enabled, notification));

        private protected IOptionsCollection Option<T>(PerLanguageOption2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
            => OptionsSet(SingleOption(option, codeStyle));

        private protected IOptionsCollection Option<T>(Option2<T> option, T value)
            => OptionsSet(SingleOption(option, value));

        private protected IOptionsCollection Option<T>(PerLanguageOption2<T> option, T value)
            => OptionsSet(SingleOption(option, value));

        internal static IOptionsCollection OptionsSet(params (OptionKey2 key, object value)[] options)
            => new OptionsCollection(LanguageNames.CSharp, options);
#endif
    }
}
