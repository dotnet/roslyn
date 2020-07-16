// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
                var option2 = new Option<bool>("Test Feature", null!, false);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var option3 = new Option<bool>(" ", "Test Name", false);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var option4 = new Option<bool>(null!, "Test Name", false);
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
                var option2 = new PerLanguageOption<bool>("Test Feature", null!, false);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var option3 = new PerLanguageOption<bool>(" ", "Test Name", false);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var option4 = new PerLanguageOption<bool>(null!, "Test Name", false);
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
            OptionSet optionSet = optionService.GetOptions();
            var option = new Option<bool>("Test Feature", "Test Name", false);
            var key = new OptionKey(option);
            Assert.False(optionSet.GetOption(option));
            optionSet = optionSet.WithChangedOption(key, true);
            Assert.True((bool?)optionSet.GetOption(key));
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
            Assert.False((bool?)optionSet.GetOption(falseKey));

            var trueKey = new OptionKey(optionTrue);
            Assert.True((bool?)optionSet.GetOption(trueKey));
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
            var value = (bool?)optionSet.GetOption(optionKey);
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
            var isOptionSet = (bool?)optionService.GetOptions().GetOption(optionKey);
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

        [Fact]
        public void TestChangedOptions()
        {
            // Apply a serializable changed option to the option service
            // and verify that serializable options snapshot contains this changed option.
            TestChangedOptionsCore(
                GenerationOptions.PlaceSystemNamespaceFirst,
                optionProvider: new GenerationOptionsProvider(),
                isSerializable: true);

            // Apply a non-serializable changed option to the option service
            // and verify that serializable options snapshot does not contain this changed option
            TestChangedOptionsCore(
                new PerLanguageOption2<bool>("Test Feature", "Test Name", defaultValue: true),
                optionProvider: new TestOptionService.TestOptionsProvider(),
                isSerializable: false);

            return;

            static void TestChangedOptionsCore(PerLanguageOption2<bool> option, IOptionProvider optionProvider, bool isSerializable)
            {
                var optionService = TestOptionService.GetService(optionProvider);
                var optionSet = optionService.GetOptions();
                var optionKey = new OptionKey(option, LanguageNames.CSharp);

                var currentOptionValue = optionSet.GetOption(option, LanguageNames.CSharp);
                var newOptionValue = !currentOptionValue;
                var newOptionSet = optionSet.WithChangedOption(optionKey, newOptionValue);

                optionService.SetOptions(newOptionSet);
                var isOptionSet = (bool?)optionService.GetOptions().GetOption(optionKey);
                Assert.Equal(newOptionValue, isOptionSet);

                var languages = ImmutableHashSet.Create(LanguageNames.CSharp);
                var serializableOptionSet = optionService.GetSerializableOptionsSnapshot(languages);
                var changedOptions = serializableOptionSet.GetChangedOptions();
                if (isSerializable)
                {
                    var changedOptionKey = Assert.Single(changedOptions);
                    Assert.Equal(optionKey, changedOptionKey);
                }
                else
                {
                    Assert.Empty(changedOptions);
                }
            }
        }

        [Fact, WorkItem(43788, "https://github.com/dotnet/roslyn/issues/43788")]
        public void TestChangedTodoCommentOptions()
        {
            var option = TodoCommentOptions.TokenList;
            var optionService = TestOptionService.GetService(GetOptionProvider<TodoCommentOptionsProvider>());
            var optionSet = optionService.GetOptions();
            var optionKey = new OptionKey(option);

            var currentOptionValue = optionSet.GetOption(option);
            var newOptionValue = currentOptionValue + "newValue";
            var newOptionSet = optionSet.WithChangedOption(optionKey, newOptionValue);

            optionService.SetOptions(newOptionSet);
            Assert.Equal(newOptionValue, (string?)optionService.GetOptions().GetOption(optionKey));

            var languages = ImmutableHashSet.Create(LanguageNames.CSharp);
            var serializableOptionSet = optionService.GetSerializableOptionsSnapshot(languages);
            var changedOptions = serializableOptionSet.GetChangedOptions();
            var changedOptionKey = Assert.Single(changedOptions);
            Assert.Equal(optionKey, changedOptionKey);
            Assert.Equal(newOptionValue, serializableOptionSet.GetOption(changedOptionKey));
        }

        private static TOptionProvider GetOptionProvider<TOptionProvider>()
            where TOptionProvider : IOptionProvider
        {
            var composition = FeaturesTestCompositions.Features.WithAdditionalParts(
                typeof(TestOptionsServiceFactory));

            return composition.ExportProviderFactory.CreateExportProvider().GetExportedValues<IOptionProvider>().OfType<TOptionProvider>().FirstOrDefault();
        }

        [Fact]
        public void TestPerLanguageCodeStyleOptions()
        {
            var perLanguageOption2 = new PerLanguageOption2<CodeStyleOption2<bool>>("test", "test", new CodeStyleOption2<bool>(false, NotificationOption2.Warning));
            var perLanguageOption = perLanguageOption2.ToPublicOption();
            var newValueCodeStyleOption2 = new CodeStyleOption2<bool>(!perLanguageOption2.DefaultValue.Value, perLanguageOption2.DefaultValue.Notification);
            var newValueCodeStyleOption = (CodeStyleOption<bool>)newValueCodeStyleOption2!;

            // Test "OptionKey" based overloads for get/set options on OptionSet and OptionService using different public and internal type combinations.

            //  1. { PerLanguageOption, CodeStyleOption }
            TestCodeStyleOptionsCommon(perLanguageOption, LanguageNames.CSharp, newValueCodeStyleOption);

            //  2. { PerLanguageOption2, CodeStyleOption }
            TestCodeStyleOptionsCommon(perLanguageOption2, LanguageNames.CSharp, newValueCodeStyleOption);

            //  3. { PerLanguageOption, CodeStyleOption2 }
            TestCodeStyleOptionsCommon(perLanguageOption, LanguageNames.CSharp, newValueCodeStyleOption2);

            //  4. { PerLanguageOption2, CodeStyleOption2 }
            TestCodeStyleOptionsCommon(perLanguageOption2, LanguageNames.CSharp, newValueCodeStyleOption2);

            var optionService = TestOptionService.GetService();
            var originalOptionSet = optionService.GetOptions();

            // Test "PerLanguageOption" and "PerLanguageOption2" overloads for OptionSet and OptionService.

            //  1. Verify default value.
            Assert.Equal(perLanguageOption.DefaultValue, originalOptionSet.GetOption(perLanguageOption, LanguageNames.CSharp));
            Assert.Equal(perLanguageOption2.DefaultValue, originalOptionSet.GetOption(perLanguageOption2, LanguageNames.CSharp));

            //  2. OptionSet validations.
            var newOptionSet = originalOptionSet.WithChangedOption(perLanguageOption, LanguageNames.CSharp, newValueCodeStyleOption);
            Assert.Equal(newValueCodeStyleOption, newOptionSet.GetOption(perLanguageOption, LanguageNames.CSharp));
            Assert.Equal(newValueCodeStyleOption2, newOptionSet.GetOption(perLanguageOption2, LanguageNames.CSharp));

            newOptionSet = originalOptionSet.WithChangedOption(perLanguageOption2, LanguageNames.CSharp, newValueCodeStyleOption2);
            Assert.Equal(newValueCodeStyleOption, newOptionSet.GetOption(perLanguageOption, LanguageNames.CSharp));
            Assert.Equal(newValueCodeStyleOption2, newOptionSet.GetOption(perLanguageOption2, LanguageNames.CSharp));

            //  3. IOptionService validation
            optionService.SetOptions(newOptionSet);
            Assert.Equal(newValueCodeStyleOption, optionService.GetOption(perLanguageOption, LanguageNames.CSharp));
            Assert.Equal(newValueCodeStyleOption2, optionService.GetOption(perLanguageOption2, LanguageNames.CSharp));
        }

        [Fact]
        public void TestLanguageSpecificCodeStyleOptions()
        {
            var option2 = new Option2<CodeStyleOption2<bool>>("test", "test", new CodeStyleOption2<bool>(false, NotificationOption2.Warning));
            var option = option2.ToPublicOption();
            var newValueCodeStyleOption2 = new CodeStyleOption2<bool>(!option2.DefaultValue.Value, option2.DefaultValue.Notification);
            var newValueCodeStyleOption = (CodeStyleOption<bool>)newValueCodeStyleOption2!;

            // Test "OptionKey" based overloads for get/set options on OptionSet and OptionService using different public and internal type combinations.

            //  1. { Option, CodeStyleOption }
            TestCodeStyleOptionsCommon(option, language: null, newValueCodeStyleOption);

            //  2. { Option2, CodeStyleOption }
            TestCodeStyleOptionsCommon(option2, language: null, newValueCodeStyleOption);

            //  3. { Option, CodeStyleOption2 }
            TestCodeStyleOptionsCommon(option, language: null, newValueCodeStyleOption2);

            //  4. { Option2, CodeStyleOption2 }
            TestCodeStyleOptionsCommon(option2, language: null, newValueCodeStyleOption2);

            var optionService = TestOptionService.GetService();
            var originalOptionSet = optionService.GetOptions();

            // Test "Option" and "Option2" overloads for OptionSet and OptionService.

            //  1. Verify default value.
            Assert.Equal(option.DefaultValue, originalOptionSet.GetOption(option));
            Assert.Equal(option2.DefaultValue, originalOptionSet.GetOption(option2));

            //  2. OptionSet validations.
            var newOptionSet = originalOptionSet.WithChangedOption(option, newValueCodeStyleOption);
            Assert.Equal(newValueCodeStyleOption, newOptionSet.GetOption(option));
            Assert.Equal(newValueCodeStyleOption2, newOptionSet.GetOption(option2));

            newOptionSet = originalOptionSet.WithChangedOption(option2, newValueCodeStyleOption2);
            Assert.Equal(newValueCodeStyleOption, newOptionSet.GetOption(option));
            Assert.Equal(newValueCodeStyleOption2, newOptionSet.GetOption(option2));

            //  3. IOptionService validation
            optionService.SetOptions(newOptionSet);
            Assert.Equal(newValueCodeStyleOption, optionService.GetOption(option));
            Assert.Equal(newValueCodeStyleOption2, optionService.GetOption(option2));
        }

        private static void TestCodeStyleOptionsCommon<TCodeStyleOption>(IOption2 option, string? language, TCodeStyleOption newValue)
            where TCodeStyleOption : ICodeStyleOption
        {
            var optionService = TestOptionService.GetService();
            var originalOptionSet = optionService.GetOptions();

            //  Test matrix using different OptionKey and OptionKey2 get/set operations.
            var optionKey = new OptionKey(option, language);
            var optionKey2 = new OptionKey2(option, language);

            // Value return from "object GetOption(OptionKey)" should always be public CodeStyleOption type.
            var newPublicValue = newValue.AsPublicCodeStyleOption();

            //  1. WithChangedOption(OptionKey), GetOption(OptionKey)/GetOption<T>(OptionKey)
            var newOptionSet = originalOptionSet.WithChangedOption(optionKey, newValue);
            Assert.Equal(newPublicValue, newOptionSet.GetOption(optionKey));
            // Value returned from public API should always be castable to public CodeStyleOption type.
            Assert.NotNull((CodeStyleOption<bool>)newOptionSet.GetOption(optionKey)!);
            // Verify "T GetOption<T>(OptionKey)" works for both cases of T being a public and internal code style option type.
            Assert.Equal(newPublicValue, newOptionSet.GetOption<CodeStyleOption<bool>>(optionKey));
            Assert.Equal(newValue, newOptionSet.GetOption<TCodeStyleOption>(optionKey));

            //  2. WithChangedOption(OptionKey), GetOption(OptionKey2)/GetOption<T>(OptionKey2)
            newOptionSet = originalOptionSet.WithChangedOption(optionKey, newValue);
            Assert.Equal(newPublicValue, newOptionSet.GetOption(optionKey2));
            // Verify "T GetOption<T>(OptionKey2)" works for both cases of T being a public and internal code style option type.
            Assert.Equal(newPublicValue, newOptionSet.GetOption<CodeStyleOption<bool>>(optionKey2));
            Assert.Equal(newValue, newOptionSet.GetOption<TCodeStyleOption>(optionKey2));

            //  3. WithChangedOption(OptionKey2), GetOption(OptionKey)/GetOption<T>(OptionKey)
            newOptionSet = originalOptionSet.WithChangedOption(optionKey2, newValue);
            Assert.Equal(newPublicValue, newOptionSet.GetOption(optionKey));
            // Value returned from public API should always be castable to public CodeStyleOption type.
            Assert.NotNull((CodeStyleOption<bool>)newOptionSet.GetOption(optionKey)!);
            // Verify "T GetOption<T>(OptionKey)" works for both cases of T being a public and internal code style option type.
            Assert.Equal(newPublicValue, newOptionSet.GetOption<CodeStyleOption<bool>>(optionKey));
            Assert.Equal(newValue, newOptionSet.GetOption<TCodeStyleOption>(optionKey));

            //  4. WithChangedOption(OptionKey2), GetOption(OptionKey2)/GetOption<T>(OptionKey2)
            newOptionSet = originalOptionSet.WithChangedOption(optionKey2, newValue);
            Assert.Equal(newPublicValue, newOptionSet.GetOption(optionKey2));
            // Verify "T GetOption<T>(OptionKey2)" works for both cases of T being a public and internal code style option type.
            Assert.Equal(newPublicValue, newOptionSet.GetOption<CodeStyleOption<bool>>(optionKey2));
            Assert.Equal(newValue, newOptionSet.GetOption<TCodeStyleOption>(optionKey2));

            //  5. IOptionService.GetOption(OptionKey)
            optionService.SetOptions(newOptionSet);
            Assert.Equal(newPublicValue, optionService.GetOption(optionKey));
        }
    }
}
