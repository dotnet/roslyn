// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorConfigSettings.Data
{
    [UseExportProvider]
    public class CodeStyleSettingsTest
    {
        private static IGlobalOptionService GetGlobalOptions(Workspace workspace)
            => workspace.Services.SolutionServices.ExportProvider.GetExportedValue<IGlobalOptionService>();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void CodeStyleSettingBoolFactory(bool defaultValue)
        {
            using var workspace = new AdhocWorkspace();
            var globalOptions = GetGlobalOptions(workspace);

            var option = CreateBoolOption(defaultValue);

            var options = new TieredAnalyzerConfigOptions(
                new TestAnalyzerConfigOptions(),
                globalOptions,
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
            using var workspace = new AdhocWorkspace();
            var globalOptions = GetGlobalOptions(workspace);

            var option = CreateEnumOption(defaultValue);

            var options = new TieredAnalyzerConfigOptions(
                new TestAnalyzerConfigOptions(),
                globalOptions,
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
            var defaultCodeStyle = CodeStyleOption2<bool>.Default.WithValue(defaultValue);

            return new Option2<CodeStyleOption2<bool>>(
                name: "dotnet_test_option",
                defaultValue: defaultCodeStyle,
                serializer: new EditorConfigValueSerializer<CodeStyleOption2<bool>>(_ => defaultCodeStyle, _ => "default"),
                isEditorConfigOption: true);
        }

        private static Option2<CodeStyleOption2<T>> CreateEnumOption<T>(T defaultValue)
            where T : notnull, Enum
        {
            var defaultCodeStyle = CodeStyleOption2<T>.Default.WithValue(defaultValue);
            return new Option2<CodeStyleOption2<T>>(
                name: "dotnet_test_option",
                defaultValue: defaultCodeStyle,
                serializer: new EditorConfigValueSerializer<CodeStyleOption2<T>>(_ => defaultCodeStyle, _ => "default"),
                isEditorConfigOption: true);
        }

        private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly IDictionary<string, string> _dictionary;
            public TestAnalyzerConfigOptions((string, string)[]? options = null)
                => _dictionary = options?.ToDictionary(x => x.Item1, x => x.Item2) ?? [];
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
                => _dictionary.TryGetValue(key, out value);
        }
    }
}
