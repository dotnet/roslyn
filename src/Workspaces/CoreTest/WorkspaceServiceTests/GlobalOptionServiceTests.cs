﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public class GlobalOptionServiceTests
    {
        private static IGlobalOptionService GetGlobalOptionService(HostWorkspaceServices services)
            => services.SolutionServices.ExportProvider.GetExportedValue<IGlobalOptionService>();

        private static ILegacyGlobalOptionService GetLegacyGlobalOptionService(HostWorkspaceServices services)
            => services.SolutionServices.ExportProvider.GetExportedValue<ILegacyGlobalOptionService>();

        [Fact]
        public void LegacyGlobalOptions_SetGet()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);

            var optionKey = new OptionKey(new TestOption() { DefaultValue = 1 });

            Assert.Equal(1, optionService.GetOption(optionKey));

            optionService.SetOptions(
                ImmutableArray<KeyValuePair<OptionKey2, object?>>.Empty,
                ImmutableArray.Create(KeyValuePairUtil.Create(optionKey, (object?)2)));

            Assert.Equal(2, optionService.GetOption(optionKey));

            optionService.SetOptions(
                ImmutableArray<KeyValuePair<OptionKey2, object?>>.Empty,
                ImmutableArray.Create(KeyValuePairUtil.Create(optionKey, (object?)3)));

            Assert.Equal(3, optionService.GetOption(optionKey));
        }

        [Fact]
        public void ExternallyDefinedOption()
        {
            using var workspace1 = new AdhocWorkspace();
            using var workspace2 = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace1.Services);
            var optionSet = new SolutionOptionSet(optionService);

            var optionKey = new OptionKey(new TestOption());
            var perLanguageOptionKey = new OptionKey(new TestOption() { IsPerLanguage = true }, "lang");

            Assert.Equal(optionKey.Option.DefaultValue, optionSet.GetOption<int>(optionKey));
            Assert.Equal(perLanguageOptionKey.Option.DefaultValue, optionSet.GetOption<int>(perLanguageOptionKey));

            var newSet = optionSet.WithChangedOption(optionKey, 2).WithChangedOption(perLanguageOptionKey, 3);
            Assert.Equal(2, newSet.GetOption<int>(optionKey));
            Assert.Equal(3, newSet.GetOption<int>(perLanguageOptionKey));

            var newSolution1 = workspace1.CurrentSolution.WithOptions(newSet);
            Assert.Equal(2, newSolution1.Options.GetOption<int>(optionKey));
            Assert.Equal(3, newSolution1.Options.GetOption<int>(perLanguageOptionKey));

            // TryApplyChanges propagates the option to all workspaces:
            var oldSolution2 = workspace2.CurrentSolution;
            Assert.Equal(1, oldSolution2.Options.GetOption<int>(optionKey));
            Assert.Equal(1, oldSolution2.Options.GetOption<int>(perLanguageOptionKey));

            Assert.True(workspace1.TryApplyChanges(newSolution1));

            var newSolution2 = workspace2.CurrentSolution;
            Assert.Equal(2, newSolution2.Options.GetOption<int>(optionKey));
            Assert.Equal(3, newSolution2.Options.GetOption<int>(perLanguageOptionKey));
        }

        [Fact]
        public void InternallyDefinedOption()
        {
            using var workspace1 = new AdhocWorkspace();
            using var workspace2 = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace1.Services);
            var optionSet = new SolutionOptionSet(optionService);

            var perLanguageOptionKey = new OptionKey(FormattingOptions.NewLine, "lang");

            Assert.Equal(perLanguageOptionKey.Option.DefaultValue, optionSet.GetOption<string>(perLanguageOptionKey));

            var newSet = optionSet.WithChangedOption(perLanguageOptionKey, "EOLN");
            Assert.Equal("EOLN", newSet.GetOption<string>(perLanguageOptionKey));

            var newSolution1 = workspace1.CurrentSolution.WithOptions(newSet);
            Assert.Equal("EOLN", newSolution1.Options.GetOption<string>(perLanguageOptionKey));

            // TryApplyChanges propagates the option to all workspaces:
            var oldSolution2 = workspace2.CurrentSolution;
            Assert.Equal(perLanguageOptionKey.Option.DefaultValue, oldSolution2.Options.GetOption<string>(perLanguageOptionKey));

            Assert.True(workspace1.TryApplyChanges(newSolution1));

            Assert.Equal("EOLN", workspace1.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));
            Assert.Equal("EOLN", workspace2.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));

            // Update global option directly (after the value is cached in the above current solution snapshots).
            // Doing so does NOT update current solutions.
            optionService.GlobalOptions.SetGlobalOption(FormattingOptions2.NewLine, "lang", "NEW_LINE");

            Assert.Equal("EOLN", workspace1.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));
            Assert.Equal("EOLN", workspace2.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));

            // Update global option indirectly via legacy service updates current solutions.
            optionService.SetOptions(
                ImmutableArray.Create(KeyValuePairUtil.Create(new OptionKey2(FormattingOptions2.NewLine, "lang"), (object?)"NEW_LINE")),
                ImmutableArray<KeyValuePair<OptionKey, object?>>.Empty);

            Assert.Equal("NEW_LINE", workspace1.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));
            Assert.Equal("NEW_LINE", workspace2.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));

            // Set the option directly again and trigger workspace update:
            optionService.GlobalOptions.SetGlobalOption(FormattingOptions2.NewLine, "lang", "NEW_LINE2");

            Assert.Equal("NEW_LINE", workspace1.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));
            Assert.Equal("NEW_LINE", workspace2.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));

            optionService.UpdateRegisteredWorkspaces();

            Assert.Equal("NEW_LINE2", workspace1.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));
            Assert.Equal("NEW_LINE2", workspace2.CurrentSolution.Options.GetOption<string>(perLanguageOptionKey));
        }

        [Fact]
        public void OptionPerLanguageOption()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var optionSet = new SolutionOptionSet(optionService);

            var optionvalid = new PerLanguageOption<bool>("Test Feature", "Test Name", false);
            Assert.False(optionSet.GetOption(optionvalid, "CS"));
        }

        [Fact]
        public void GettingOptionReturnsOption()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var optionSet = new SolutionOptionSet(optionService);
            var option = new Option<bool>("Test Feature", "Test Name", false);
            Assert.False(optionSet.GetOption(option));
        }

        [Fact]
        public void GlobalOptions()
        {
            using var workspace = new AdhocWorkspace();
            var globalOptions = GetGlobalOptionService(workspace.Services);
            var option1 = new Option2<int>("Feature1", "Name1", defaultValue: 1);
            var option2 = new Option2<int>("Feature2", "Name2", defaultValue: 2);
            var option3 = new Option2<int>("Feature3", "Name3", defaultValue: 3);

            var changedOptions = new List<OptionChangedEventArgs>();

            var handler = new EventHandler<OptionChangedEventArgs>((_, e) => changedOptions.Add(e));
            globalOptions.OptionChanged += handler;

            var values = globalOptions.GetOptions(ImmutableArray.Create(new OptionKey2(option1), new OptionKey2(option2)));
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);

            globalOptions.SetGlobalOptions(ImmutableArray.Create(
                KeyValuePairUtil.Create(new OptionKey2(option1), (object?)5),
                KeyValuePairUtil.Create(new OptionKey2(option2), (object?)6),
                KeyValuePairUtil.Create(new OptionKey2(option3), (object?)3)));

            AssertEx.Equal(new[]
            {
                "Name1=5",
                "Name2=6",
            }, changedOptions.Select(e => $"{e.OptionKey.Option.Name}={e.Value}"));

            values = globalOptions.GetOptions(ImmutableArray.Create(new OptionKey2(option1), new OptionKey2(option2), new OptionKey2(option3)));
            Assert.Equal(5, values[0]);
            Assert.Equal(6, values[1]);
            Assert.Equal(3, values[2]);

            Assert.Equal(5, globalOptions.GetOption(option1));
            Assert.Equal(6, globalOptions.GetOption(option2));
            Assert.Equal(3, globalOptions.GetOption(option3));

            globalOptions.OptionChanged -= handler;
        }

        [Fact]
        public void GettingOptionWithChangedOption()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            OptionSet optionSet = new SolutionOptionSet(optionService);
            var option = new Option<bool>("Test Feature", "Test Name", false);
            var key = new OptionKey(option);
            Assert.False(optionSet.GetOption(option));
            optionSet = optionSet.WithChangedOption(key, true);
            Assert.True((bool?)optionSet.GetOption(key));
        }

        [Fact]
        public void GettingOptionWithoutChangedOption()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var optionSet = new SolutionOptionSet(optionService);

            var optionFalse = new Option<bool>("Test Feature", "Test Name", false);
            Assert.False(optionSet.GetOption(optionFalse));

            var optionTrue = new Option<bool>("Test Feature", "Test Name", true);
            Assert.True(optionSet.GetOption(optionTrue));

            var falseKey = new OptionKey(optionFalse);
            Assert.False((bool?)optionSet.GetOption(falseKey));

            var trueKey = new OptionKey(optionTrue);
            Assert.True((bool?)optionSet.GetOption(trueKey));
        }

        [Fact]
        public void GetKnownOptions()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            optionService.GetOption(option);

            var optionSet = new SolutionOptionSet(optionService);
            var value = optionSet.GetOption((Option<bool>)option);
            Assert.True(value);
        }

        [Fact]
        public void GetKnownOptionsKey()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            optionService.GetOption(option);

            var optionSet = new SolutionOptionSet(optionService);
            var optionKey = new OptionKey(option);
            var value = (bool?)optionSet.GetOption(optionKey);
            Assert.True(value);
        }

        [Fact]
        public void SetKnownOptions()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var optionSet = new SolutionOptionSet(optionService);

            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            var optionKey = new OptionKey(option);
            var newOptionSet = optionSet.WithChangedOption(optionKey, false);
            var changedOptions = ((SolutionOptionSet)newOptionSet).GetChangedOptions();
            optionService.SetOptions(changedOptions.internallyDefined, changedOptions.externallyDefined);
            var isOptionSet = (bool?)new SolutionOptionSet(optionService).GetOption(optionKey);
            Assert.False(isOptionSet);
        }

        [Fact]
        public void OptionSetIsImmutable()
        {
            using var workspace = new AdhocWorkspace();
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var optionSet = new SolutionOptionSet(optionService);

            var option = new Option<bool>("Test Feature", "Test Name", defaultValue: true);
            var optionKey = new OptionKey(option);
            var newOptionSet = optionSet.WithChangedOption(optionKey, false);
            Assert.NotSame(optionSet, newOptionSet);
            Assert.NotEqual(optionSet, newOptionSet);
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

            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var originalOptionSet = new SolutionOptionSet(optionService);
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
        }

        private static void TestCodeStyleOptionsCommon<TCodeStyleOption>(Workspace workspace, IOption2 option, string? language, TCodeStyleOption newValue)
            where TCodeStyleOption : ICodeStyleOption
        {
            var optionService = GetLegacyGlobalOptionService(workspace.Services);
            var originalOptionSet = new SolutionOptionSet(optionService);

            var optionKey = new OptionKey(option, language);

            // Value return from "object GetOption(OptionKey)" should always be public CodeStyleOption type.
            var newPublicValue = newValue.AsPublicCodeStyleOption();

            //  1. WithChangedOption(OptionKey), GetOption(OptionKey)/GetOption<T>(OptionKey)
            var newOptionSet = originalOptionSet.WithChangedOption(optionKey, newValue);
            Assert.Equal(newPublicValue, newOptionSet.GetOption(optionKey));
            // Value returned from public API should always be castable to public CodeStyleOption type.
            Assert.NotNull((CodeStyleOption<bool>?)newOptionSet.GetOption(optionKey));

            // Verify "T GetOption<T>(OptionKey)" works for T being a public code style option type.
            Assert.Equal(newPublicValue, newOptionSet.GetOption<CodeStyleOption<bool>>(optionKey));

            //  2. IOptionService.GetOption(OptionKey)
            var changedOptions = ((SolutionOptionSet)newOptionSet).GetChangedOptions();
            optionService.SetOptions(changedOptions.internallyDefined, changedOptions.externallyDefined);
            Assert.Equal(newPublicValue, optionService.GetOption(optionKey));
        }
    }
}
