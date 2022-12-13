// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Options
{
    public class OptionKeyTests
    {
        [Fact]
        public void OptionConstructor_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => new Option<bool>("Test Feature", null!, false));
            Assert.Throws<ArgumentNullException>(() => new Option<bool>(null!, "Test Name", false));
            Assert.Throws<ArgumentNullException>(() => new Option<bool>("X", "Test Name", false, storageLocations: null!));
        }

        [Fact]
        public void PerLanguageOptionConstructor_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => new PerLanguageOption<bool>("Test Feature", null!, false));
            Assert.Throws<ArgumentNullException>(() => new PerLanguageOption<bool>(null!, "Test Name", false));
            Assert.Throws<ArgumentNullException>(() => new PerLanguageOption<bool>("X", "Test Name", false, storageLocations: null!));
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
    }
}
