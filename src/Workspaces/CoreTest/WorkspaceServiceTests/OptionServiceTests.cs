// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    [UseExportProvider]
    public class OptionServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void OptionWithNullOrWhitespace()
        {
            var optionService = TestOptionService.GetService();
            var optionSet = optionService.GetOptions();

            Assert.Throws<System.ArgumentException>(delegate
            {
                var option = new Option<bool>("Test Feature", "", false);
            });

            Assert.Throws<System.ArgumentException>(delegate
            {
                var option2 = new Option<bool>("Test Feature", null, false);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var option3 = new Option<bool>(" ", "Test Name", false);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var option4 = new Option<bool>(null, "Test Name", false);
            });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void OptionPerLanguageOption()
        {
            var optionService = TestOptionService.GetService();
            var optionSet = optionService.GetOptions();

            Assert.Throws<System.ArgumentException>(delegate
            {
                var option = new PerLanguageOption<bool>("Test Feature", "", false);
            });

            Assert.Throws<System.ArgumentException>(delegate
            {
                var option2 = new PerLanguageOption<bool>("Test Feature", null, false);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var option3 = new PerLanguageOption<bool>(" ", "Test Name", false);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var option4 = new PerLanguageOption<bool>(null, "Test Name", false);
            });

            var optionvalid = new PerLanguageOption<bool>("Test Feature", "Test Name", false);
            Assert.False(optionSet.GetOption(optionvalid, "CS"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void GettingOptionReturnsOption()
        {
            var optionService = TestOptionService.GetService();
            var optionSet = optionService.GetOptions();
            var option = new Option<bool>("Test Feature", "Test Name", false);
            Assert.False(optionSet.GetOption(option));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void GettingOptionWithChangedOption()
        {
            var optionService = TestOptionService.GetService();
            var optionSet = optionService.GetOptions();
            var option = new Option<bool>("Test Feature", "Test Name", false);
            var key = new OptionKey(option);
            Assert.False(optionSet.GetOption(option));
            optionSet = optionSet.WithChangedOption(key, true);
            Assert.True((bool)optionSet.GetOption(key));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void GettingOptionWithoutChangedOption()
        {
            var optionService = TestOptionService.GetService();
            var optionSet = optionService.GetOptions();

            var optionFalse = new Option<bool>("Test Feature", "Test Name", false);
            Assert.False(optionSet.GetOption(optionFalse));

            var optionTrue = new Option<bool>("Test Feature", "Test Name", true);
            Assert.True(optionSet.GetOption(optionTrue));

            var falseKey = new OptionKey(optionFalse);
            Assert.False((bool)optionSet.GetOption(falseKey));

            var trueKey = new OptionKey(optionTrue);
            Assert.True((bool)optionSet.GetOption(trueKey));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void GetKnownOptions()
        {
            var optionService = TestOptionService.GetService();
            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            optionService.GetOption(option);

            var optionSet = optionService.GetOptions();
            var value = optionSet.GetOption(option);
            Assert.True(value);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void GetKnownOptionsKey()
        {
            var optionService = TestOptionService.GetService();
            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            optionService.GetOption(option);

            var optionSet = optionService.GetOptions();
            var optionKey = new OptionKey(option);
            var value = (bool)optionSet.GetOption(optionKey);
            Assert.True(value);
        }

        [Fact]
        public void SetKnownOptions()
        {
            var optionService = TestOptionService.GetService();
            var optionSet = optionService.GetOptions();

            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            var optionKey = new OptionKey(option);
            var newOptionSet = optionSet.WithChangedOption(optionKey, false);
            optionService.SetOptions(newOptionSet);
            var isOptionSet = (bool)optionService.GetOptions().GetOption(optionKey);
            Assert.False(isOptionSet);
        }

        [Fact]
        public void OptionSetIsImmutable()
        {
            var optionService = TestOptionService.GetService();
            var optionSet = optionService.GetOptions();

            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            var optionKey = new OptionKey(option);
            var newOptionSet = optionSet.WithChangedOption(optionKey, false);
            Assert.NotSame(optionSet, newOptionSet);
            Assert.NotEqual(optionSet, newOptionSet);
        }
    }
}
