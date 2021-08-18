// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias WORKSPACES;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using WORKSPACES::Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorConfigSettings.Data
{
    public class CodeStyleSettingsTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void CodeStyleSettingBoolFactory(bool defaultValue)
        {
            var option = CreateBoolOption(defaultValue);
            var editorConfigOptions = new TestAnalyzerConfigOptions();
            var visualStudioOptions = new TestOptionSet<bool>(option.DefaultValue);
            var setting = CodeStyleSetting.Create(option, description: "TestDesciption", editorConfigOptions, visualStudioOptions, updater: null!);
            Assert.Equal(string.Empty, setting.Category);
            Assert.Equal("TestDesciption", setting.Description);
            Assert.False(setting.IsDefinedInEditorConfig);
            Assert.Equal(typeof(bool), setting.Type);
            Assert.Equal(defaultValue, setting.Value);
        }

        [Theory]
        [InlineData(DayOfWeek.Monday)]
        [InlineData(DayOfWeek.Friday)]
        public static void CodeStyleSettingEnumFactory(DayOfWeek defaultValue)
        {
            var option = CreateEnumOption(defaultValue);
            var editorConfigOptions = new TestAnalyzerConfigOptions();
            var visualStudioOptions = new TestOptionSet<DayOfWeek>(option.DefaultValue);
            var setting = CodeStyleSetting.Create(option,
                                                  description: "TestDesciption",
                                                  enumValues: (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)),
                                                  valueDescriptions: Enum.GetNames(typeof(DayOfWeek)),
                                                  editorConfigOptions,
                                                  visualStudioOptions,
                                                  updater: null!);
            Assert.Equal(string.Empty, setting.Category);
            Assert.Equal("TestDesciption", setting.Description);
            Assert.False(setting.IsDefinedInEditorConfig);
            Assert.Equal(typeof(DayOfWeek), setting.Type);
            Assert.Equal(defaultValue, setting.Value);
        }

        private static Option2<CodeStyleOption2<bool>> CreateBoolOption(bool @default = false)
        {
            var option = CodeStyleOption2<bool>.Default;
            option = (CodeStyleOption2<bool>)((ICodeStyleOption)option).WithValue(@default);
            return new Option2<CodeStyleOption2<bool>>(feature: "TestFeature",
                                                       name: "TestOption",
                                                       defaultValue: option);
        }

        private static Option2<CodeStyleOption2<T>> CreateEnumOption<T>(T @default)
            where T : notnull, Enum
        {
            var option = CodeStyleOption2<T>.Default;
            option = (CodeStyleOption2<T>)((ICodeStyleOption)option).WithValue(@default);
            return new Option2<CodeStyleOption2<T>>(feature: "TestFeature",
                                                    name: "TestOption",
                                                    defaultValue: option);
        }

        private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly IDictionary<string, string> _dictionary;
            public TestAnalyzerConfigOptions((string, string)[]? options = null)
                => _dictionary = options?.ToDictionary(x => x.Item1, x => x.Item2) ?? new Dictionary<string, string>();
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
                => _dictionary.TryGetValue(key, out value);
        }

        private class TestOptionSet<T> : OptionSet
        {
            private readonly object? _value;
            public TestOptionSet(CodeStyleOption2<T> value) => _value = value;
            public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value) => this;
            internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet) => Array.Empty<OptionKey>();
            private protected override object? GetOptionCore(OptionKey optionKey) => _value;
        }
    }
}
