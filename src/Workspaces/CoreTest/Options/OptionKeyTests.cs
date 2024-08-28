// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Options
{
    public class OptionKeyTests
    {
        private sealed class TestOptionStorageLocation : OptionStorageLocation
        {
        }

        [Fact]
        public void OptionConstructor_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => new Option<bool>("Test Feature", null!, false));
            Assert.Throws<ArgumentNullException>(() => new Option<bool>(null!, "Test Name", false));
            Assert.Throws<ArgumentNullException>(() => new Option<bool>("X", "Test Name", false, storageLocations: null!));
            Assert.Throws<ArgumentNullException>(() => new Option<bool>("X", "Test Name", false, storageLocations: [null!]));
        }

        [Fact]
        public void PerLanguageOptionConstructor_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => new PerLanguageOption<bool>("Test Feature", null!, false));
            Assert.Throws<ArgumentNullException>(() => new PerLanguageOption<bool>(null!, "Test Name", false));
            Assert.Throws<ArgumentNullException>(() => new PerLanguageOption<bool>("X", "Test Name", false, storageLocations: null!));
            Assert.Throws<ArgumentNullException>(() => new PerLanguageOption<bool>("X", "Test Name", false, storageLocations: [null!]));
        }

        [Fact]
        public void OptionConstructor_MultipleStorages()
        {
            var storage1 = new TestOptionStorageLocation();
            var storage2 = new TestOptionStorageLocation();
            var storage3 = new TestOptionStorageLocation();

            var option = new Option<bool>("X", "Test Name", false, storage1, storage2, storage3);
            Assert.Same(storage1, option.StorageLocations[0]);
            Assert.Same(storage2, option.StorageLocations[1]);
            Assert.Same(storage3, option.StorageLocations[2]);
        }

        [Fact]
        public void PerLanguageOptionConstructor_MultipleStorages()
        {
            var storage1 = new TestOptionStorageLocation();
            var storage2 = new TestOptionStorageLocation();
            var storage3 = new TestOptionStorageLocation();

            var option = new PerLanguageOption<bool>("X", "Test Name", false, storage1, storage2, storage3);
            Assert.Same(storage1, option.StorageLocations[0]);
            Assert.Same(storage2, option.StorageLocations[1]);
            Assert.Same(storage3, option.StorageLocations[2]);
        }

        [Fact]
        public void Ctor_Language()
        {
            var optionKey = new OptionKey(new TestOption() { IsPerLanguage = false });
            Assert.Null(optionKey.Language);

            Assert.Throws<ArgumentNullException>(() => new OptionKey(null!));
            Assert.Throws<ArgumentNullException>(() => new OptionKey(null!, null!));
            Assert.Throws<ArgumentNullException>(() => new OptionKey(null!, "lang"));
            Assert.Throws<ArgumentNullException>(() => new OptionKey(new TestOption() { IsPerLanguage = true }));
            Assert.Throws<ArgumentException>(() => new OptionKey(new TestOption() { IsPerLanguage = false }, language: "lang"));
        }

        [Fact]
        public void Names()
        {
            var option1 = new Option2<bool>(name: "name", defaultValue: false);
            Assert.Equal("config", ((IOption)option1).Feature);
            Assert.Equal("name", ((IOption)option1).Name);
            Assert.Equal("name", option1.Definition.ConfigName);
            Assert.Equal("name", option1.ToString());

            var option2 = new PerLanguageOption2<bool>(name: "name", defaultValue: false);
            Assert.Equal("config", ((IOption)option2).Feature);
            Assert.Equal("name", ((IOption)option2).Name);
            Assert.Equal("name", option2.Definition.ConfigName);
            Assert.Equal("name", option2.ToString());
        }

        [Fact]
        [Obsolete]
        public void ToStringForObsoleteOption()
        {
            var option = new Option<bool>("FooFeature", "BarName");
            var optionKey = new OptionKey(option);

            var toStringResult = optionKey.ToString();

            Assert.Equal("FooFeature - BarName", toStringResult);
        }

        [Fact]
        public void ToStringForOption()
        {
            var option = new Option<bool>("FooFeature", "BarName", false);
            var optionKey = new OptionKey(option);

            var toStringResult = optionKey.ToString();

            Assert.Equal("FooFeature - BarName", toStringResult);
        }

        [Fact]
        public void ToStringForPerLanguageOption()
        {
            var option = new PerLanguageOption<bool>("FooFeature", "BarName", false);
            var optionKey = new OptionKey(option, "BazLanguage");

            var toStringResult = optionKey.ToString();

            Assert.Equal("(BazLanguage) FooFeature - BarName", toStringResult);
        }

        [Fact]
        public void ToStringForDefaultOption()
        {
            // Verify ToString works gracefully for default option key.
            var optionKey = default(OptionKey);
            var toStringResult = optionKey.ToString();
            Assert.Equal("", toStringResult);

            // Also verify GetHashCode does not throw.
            _ = optionKey.GetHashCode();
        }

        [Fact]
        public void Equals_PerLanguageOption_NonUniqueConfigName()
        {
            var option1 = new PerLanguageOption<bool>("FooFeature", "BarName", defaultValue: false);
            var option2 = new PerLanguageOption<bool>("FooFeature", "BarName", defaultValue: true);
            var option3 = new PerLanguageOption<bool>("FormattingOptions", "UseTabs", FormattingOptions.UseTabs.DefaultValue);

            Assert.False(option1.Equals(option2));
            Assert.False(option2.Equals(option1));
            Assert.False(FormattingOptions.UseTabs.Equals(option3));
            Assert.False(option3.Equals(FormattingOptions.UseTabs));
        }

        [Fact]
        public void Equals_Option_NonUniqueConfigName()
        {
            var option1 = new Option<bool>("FooFeature", "BarName", defaultValue: false);
            var option2 = new Option<bool>("FooFeature", "BarName", defaultValue: true);
            var option3 = new Option<bool>("CSharpFormattingOptions", "SpacingAfterMethodDeclarationName", CSharpFormattingOptions.SpacingAfterMethodDeclarationName.DefaultValue);

            Assert.False(option1.Equals(option2));
            Assert.False(option2.Equals(option1));
            Assert.False(CSharpFormattingOptions.SpacingAfterMethodDeclarationName.Equals(option3));
            Assert.False(option3.Equals(CSharpFormattingOptions.SpacingAfterMethodDeclarationName));
        }

        [Fact]
        public void ToPublicOption()
        {
            var option = CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess;
            var publicOption = CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess;

            Assert.True(option.Definition.Serializer.TryParseValue("true:suggestion", out var result));
            Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Suggestion.WithIsExplicitlySpecified(true)), result);

            Assert.Empty(publicOption.StorageLocations);
        }

        [Fact]
        public void IsEditorConfigOption()
        {
            Assert.All(FormattingOptions2.EditorConfigOptions, o => Assert.True(o.Definition.IsEditorConfigOption));
            Assert.All(FormattingOptions2.UndocumentedOptions, o => Assert.True(o.Definition.IsEditorConfigOption));

            Assert.False(FormattingOptions2.SmartIndent.Definition.IsEditorConfigOption);

            Assert.All(CSharpFormattingOptions2.EditorConfigOptions, o => Assert.True(o.Definition.IsEditorConfigOption));
            Assert.All(CSharpFormattingOptions2.UndocumentedOptions, o => Assert.True(o.Definition.IsEditorConfigOption));

            Assert.True(NamingStyleOptions.NamingPreferences.Definition.IsEditorConfigOption);
            Assert.True(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.Definition.IsEditorConfigOption);
        }
    }
}
