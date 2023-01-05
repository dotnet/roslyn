﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.UnitTests;
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

            var options = new TieredAnalyzerConfigOptions(
                new TestAnalyzerConfigOptions(),
                new TestAnalyzerConfigOptions(new[] { ("csharp_test_option", "default") }),
                LanguageNames.CSharp,
                ".editorconfig");

            var setting = CodeStyleSetting.Create(option, description: "TestDesciption", options, updater: null!);
            Assert.Equal(string.Empty, setting.Category);
            Assert.Equal("TestDesciption", setting.Description);
            Assert.False(setting.IsDefinedInEditorConfig);
            Assert.Equal(typeof(bool), setting.Type);
            Assert.Equal(defaultValue, setting.GetCodeStyle().Value);
        }

        [Theory]
        [InlineData(DayOfWeek.Monday)]
        [InlineData(DayOfWeek.Friday)]
        public static void CodeStyleSettingEnumFactory(DayOfWeek defaultValue)
        {
            var option = CreateEnumOption(defaultValue);

            var options = new TieredAnalyzerConfigOptions(
                new TestAnalyzerConfigOptions(),
                new TestAnalyzerConfigOptions(new[] { ("csharp_test_option", "default") }),
                LanguageNames.CSharp,
                ".editorconfig");

            var setting = CodeStyleSetting.Create(
                option,
                description: "TestDesciption",
                options,
                updater: null!,
                enumValues: (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)),
                valueDescriptions: Enum.GetNames(typeof(DayOfWeek)));

            Assert.Equal(string.Empty, setting.Category);
            Assert.Equal("TestDesciption", setting.Description);
            Assert.False(setting.IsDefinedInEditorConfig);
            Assert.Equal(typeof(DayOfWeek), setting.Type);
            Assert.Equal(defaultValue, setting.GetCodeStyle().Value);
        }

        private static Option2<CodeStyleOption2<bool>> CreateBoolOption(bool defaultValue = false)
        {
            var defaultCodeStyle = (CodeStyleOption2<bool>)((ICodeStyleOption)CodeStyleOption2<bool>.Default).WithValue(defaultValue);

            return new Option2<CodeStyleOption2<bool>>(
                feature: "TestFeature",
                name: "TestOption",
                defaultValue: defaultCodeStyle,
                new EditorConfigStorageLocation<CodeStyleOption2<bool>>("csharp_test_option", _ => defaultCodeStyle, _ => "default"));
        }

        private static Option2<CodeStyleOption2<T>> CreateEnumOption<T>(T defaultValue)
            where T : notnull, Enum
        {
            var defaultCodeStyle = (CodeStyleOption2<T>)((ICodeStyleOption)CodeStyleOption2<T>.Default).WithValue(defaultValue);
            return new Option2<CodeStyleOption2<T>>(
                feature: "TestFeature",
                name: "TestOption",
                defaultValue: defaultCodeStyle,
                new EditorConfigStorageLocation<CodeStyleOption2<T>>("csharp_test_option", _ => defaultCodeStyle, _ => "default"));
        }

        private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly IDictionary<string, string> _dictionary;
            public TestAnalyzerConfigOptions((string, string)[]? options = null)
                => _dictionary = options?.ToDictionary(x => x.Item1, x => x.Item2) ?? new Dictionary<string, string>();
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
                => _dictionary.TryGetValue(key, out value);
        }
    }
}
