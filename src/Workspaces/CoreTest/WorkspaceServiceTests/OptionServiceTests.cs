// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    [UseExportProvider]
    public class OptionServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void OptionWithNullOrWhitespace()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
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
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
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
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
            var optionSet = optionService.GetOptions();
            var option = new Option<bool>("Test Feature", "Test Name", false);
            Assert.False(optionSet.GetOption(option));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void GlobalOptions()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetGlobalOptionService(workspace.Services);
            var option1 = new Option<int>("Feature1", "Name1", defaultValue: 1);
            var option2 = new Option<int>("Feature2", "Name2", defaultValue: 2);
            var option3 = new Option<int>("Feature3", "Name3", defaultValue: 3);

            var changedOptions = new List<OptionChangedEventArgs>();

            var handler = new EventHandler<OptionChangedEventArgs>((_, e) => changedOptions.Add(e));
            optionService.OptionChanged += handler;

            var values = optionService.GetOptions(ImmutableArray.Create<OptionKey>(option1, option2));
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);

            optionService.SetGlobalOptions(
                ImmutableArray.Create<OptionKey>(option1, option2, option3),
                ImmutableArray.Create<object?>(5, 6, 3));

            AssertEx.Equal(new[]
            {
                "Name1=5",
                "Name2=6",
            }, changedOptions.Select(e => $"{e.OptionKey.Option.Name}={e.Value}"));

            values = optionService.GetOptions(ImmutableArray.Create<OptionKey>(option1, option2, option3));
            Assert.Equal(5, values[0]);
            Assert.Equal(6, values[1]);
            Assert.Equal(3, values[2]);

            Assert.Equal(5, optionService.GetOption(option1));
            Assert.Equal(6, optionService.GetOption(option2));
            Assert.Equal(3, optionService.GetOption(option3));

            optionService.OptionChanged -= handler;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void GettingOptionWithChangedOption()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
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
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
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
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            optionService.GetOption(option);

            var optionSet = optionService.GetOptions();
            var value = optionSet.GetOption(option);
            Assert.True(value);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void GetKnownOptionsKey()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
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
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
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
            using var workspace = new AdhocWorkspace();
            var optionService = TestOptionService.GetService(workspace);
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
            using var workspace = new AdhocWorkspace();

            // Apply a serializable changed option to the option service
            // and verify that serializable options snapshot contains this changed option.
            TestChangedOptionsCore(
                workspace,
                GenerationOptions.PlaceSystemNamespaceFirst,
                optionProvider: ((IMefHostExportProvider)workspace.Services.HostServices).GetExportedValues<IOptionProvider>().OfType<GenerationOptionsProvider>().Single(),
                isSerializable: true);

            // Apply a non-serializable changed option to the option service
            // and verify that serializable options snapshot does not contain this changed option
            TestChangedOptionsCore(
                workspace,
                new PerLanguageOption2<bool>("Test Feature", "Test Name", defaultValue: true),
                optionProvider: new TestOptionService.TestOptionsProvider(),
                isSerializable: false);

            return;

            static void TestChangedOptionsCore(Workspace workspace, PerLanguageOption2<bool> option, IOptionProvider optionProvider, bool isSerializable)
            {
                var optionService = TestOptionService.GetService(workspace, optionProvider);
                var optionSet = optionService.GetOptions();
                var optionKey = new OptionKey(option, LanguageNames.CSharp);

                var currentOptionValue = optionSet.GetOption(option, LanguageNames.CSharp);
                var newOptionValue = !currentOptionValue;
                var newOptionSet = (SerializableOptionSet)optionSet.WithChangedOption(optionKey, newOptionValue);

                optionService.SetOptions(newOptionSet);
                var isOptionSet = (bool?)optionService.GetOptions().GetOption(optionKey);
                Assert.Equal(newOptionValue, isOptionSet);

                // Verify the serializable option snapshot obtained option service has the changed option only if the option key is serializable.
                var languages = ImmutableHashSet.Create(LanguageNames.CSharp);
                var serializableOptionSet = optionService.GetSerializableOptionsSnapshot(languages);
                VerifyChangedOptionsCore(serializableOptionSet, optionKey, expectedChangedOption: isSerializable);

                // Serialize/deserialize the option set to test round tripping.
                serializableOptionSet = (SerializableOptionSet)serializableOptionSet.WithChangedOption(optionKey, newOptionValue);
                using var memoryStream = new MemoryStream();
                using var writer = new ObjectWriter(memoryStream, leaveOpen: true);
                serializableOptionSet.Serialize(writer, CancellationToken.None);

                memoryStream.Position = 0;
                var originalChecksum = Checksum.Create(memoryStream);

                memoryStream.Position = 0;
                using var reader = ObjectReader.TryGetReader(memoryStream);
                serializableOptionSet = SerializableOptionSet.Deserialize(reader, optionService, CancellationToken.None);

                // Verify the option set obtained from round trip has the changed option only if the option key is serializable.
                VerifyChangedOptionsCore(serializableOptionSet, optionKey, expectedChangedOption: isSerializable);

                using var newMemoryStream = new MemoryStream();
                using var newWriter = new ObjectWriter(newMemoryStream, leaveOpen: true);
                serializableOptionSet.Serialize(newWriter, CancellationToken.None);
                newMemoryStream.Position = 0;
                var newChecksum = Checksum.Create(newMemoryStream);

                Assert.Equal(originalChecksum, newChecksum);
                return;

                static void VerifyChangedOptionsCore(SerializableOptionSet serializableOptionSet, OptionKey optionKey, bool expectedChangedOption)
                {
                    var changedOptions = serializableOptionSet.GetChangedOptions();
                    if (expectedChangedOption)
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
        }

        [Fact]
        public void TestPerLanguageCodeStyleOptions()
        {
            using var workspace = new AdhocWorkspace();
            var perLanguageOption2 = new PerLanguageOption2<CodeStyleOption2<bool>>("test", "test", new CodeStyleOption2<bool>(false, NotificationOption2.Warning));
            var perLanguageOption = perLanguageOption2.ToPublicOption();
            var newValueCodeStyleOption2 = new CodeStyleOption2<bool>(!perLanguageOption2.DefaultValue.Value, perLanguageOption2.DefaultValue.Notification);
            var newValueCodeStyleOption = (CodeStyleOption<bool>)newValueCodeStyleOption2!;

            // Test "OptionKey" based overloads for get/set options on OptionSet and OptionService using different public and internal type combinations.

            //  1. { PerLanguageOption, CodeStyleOption }
            TestCodeStyleOptionsCommon(workspace, perLanguageOption, LanguageNames.CSharp, newValueCodeStyleOption);

            //  2. { PerLanguageOption2, CodeStyleOption }
            TestCodeStyleOptionsCommon(workspace, perLanguageOption2, LanguageNames.CSharp, newValueCodeStyleOption);

            //  3. { PerLanguageOption, CodeStyleOption2 }
            TestCodeStyleOptionsCommon(workspace, perLanguageOption, LanguageNames.CSharp, newValueCodeStyleOption2);

            //  4. { PerLanguageOption2, CodeStyleOption2 }
            TestCodeStyleOptionsCommon(workspace, perLanguageOption2, LanguageNames.CSharp, newValueCodeStyleOption2);

            var optionService = TestOptionService.GetService(workspace);
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
            using var workspace = new AdhocWorkspace();
            var option2 = new Option2<CodeStyleOption2<bool>>("test", "test", new CodeStyleOption2<bool>(false, NotificationOption2.Warning));
            var option = option2.ToPublicOption();
            var newValueCodeStyleOption2 = new CodeStyleOption2<bool>(!option2.DefaultValue.Value, option2.DefaultValue.Notification);
            var newValueCodeStyleOption = (CodeStyleOption<bool>)newValueCodeStyleOption2!;

            // Test "OptionKey" based overloads for get/set options on OptionSet and OptionService using different public and internal type combinations.

            //  1. { Option, CodeStyleOption }
            TestCodeStyleOptionsCommon(workspace, option, language: null, newValueCodeStyleOption);

            //  2. { Option2, CodeStyleOption }
            TestCodeStyleOptionsCommon(workspace, option2, language: null, newValueCodeStyleOption);

            //  3. { Option, CodeStyleOption2 }
            TestCodeStyleOptionsCommon(workspace, option, language: null, newValueCodeStyleOption2);

            //  4. { Option2, CodeStyleOption2 }
            TestCodeStyleOptionsCommon(workspace, option2, language: null, newValueCodeStyleOption2);

            var optionService = TestOptionService.GetService(workspace);
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

        private static void TestCodeStyleOptionsCommon<TCodeStyleOption>(Workspace workspace, IOption2 option, string? language, TCodeStyleOption newValue)
            where TCodeStyleOption : ICodeStyleOption
        {
            var optionService = TestOptionService.GetService(workspace);
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
