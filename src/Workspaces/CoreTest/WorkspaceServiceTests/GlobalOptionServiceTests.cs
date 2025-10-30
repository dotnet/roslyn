// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Workspace)]
public sealed class GlobalOptionServiceTests
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

        Assert.Equal(1, optionService.GetExternallyDefinedOption(optionKey));

        optionService.SetOptions(
            [],
            [KeyValuePair.Create(optionKey, (object?)2)]);

        Assert.Equal(2, optionService.GetExternallyDefinedOption(optionKey));

        optionService.SetOptions(
            [],
            [KeyValuePair.Create(optionKey, (object?)3)]);

        Assert.Equal(3, optionService.GetExternallyDefinedOption(optionKey));
    }

    [Theory, CombinatorialData]
    public void ExternallyDefinedOption(bool subclass)
    {
        using var workspace1 = new AdhocWorkspace();
        using var workspace2 = new AdhocWorkspace();
        var optionService = GetLegacyGlobalOptionService(workspace1.Services);
        var optionSet = new SolutionOptionSet(optionService);

        var option = subclass ? (IOption)new TestOption<int>(defaultValue: 1) : new TestOption() { DefaultValue = 1 };
        var perLanguageOption = subclass ? (IOption)new PerLanguageTestOption<int>(defaultValue: 1) : new TestOption() { IsPerLanguage = true, DefaultValue = 1 };

        var optionKey = new OptionKey(option);
        var perLanguageOptionKey = new OptionKey(perLanguageOption, "lang");

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
            [KeyValuePair.Create(new OptionKey2(FormattingOptions2.NewLine, "lang"), (object?)"NEW_LINE")],
            []);

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
        var option1 = new Option2<int>("test_option1", defaultValue: 1);
        var option2 = new Option2<int>("test_option2", defaultValue: 2);
        var option3 = new Option2<int>("test_option3", defaultValue: 3);

        var events = new List<OptionChangedEventArgs>();

        var handler = new WeakEventHandler<OptionChangedEventArgs>((_, _, e) => events.Add(e));
        globalOptions.AddOptionChangedHandler(this, handler);

        var values = globalOptions.GetOptions([new OptionKey2(option1), new OptionKey2(option2)]);
        Assert.Equal(1, values[0]);
        Assert.Equal(2, values[1]);

        globalOptions.SetGlobalOptions(
        [
            KeyValuePair.Create(new OptionKey2(option1), (object?)5),
            KeyValuePair.Create(new OptionKey2(option2), (object?)6),
            KeyValuePair.Create(new OptionKey2(option3), (object?)3),
        ]);

        AssertEx.Equal(
        [
            "test_option1=5",
            "test_option2=6",
        ], events.Single().ChangedOptions.Select(e => $"{e.key.Option.Definition.ConfigName}={e.newValue}"));

        values = globalOptions.GetOptions([new OptionKey2(option1), new OptionKey2(option2), new OptionKey2(option3)]);
        Assert.Equal(5, values[0]);
        Assert.Equal(6, values[1]);
        Assert.Equal(3, values[2]);

        Assert.Equal(5, globalOptions.GetOption(option1));
        Assert.Equal(6, globalOptions.GetOption(option2));
        Assert.Equal(3, globalOptions.GetOption(option3));

        globalOptions.RemoveOptionChangedHandler(this, handler);
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
        var optionSet = new SolutionOptionSet(GetLegacyGlobalOptionService(workspace.Services));

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
        var perLanguageOption2 = new PerLanguageOption2<CodeStyleOption2<bool>>("test", new CodeStyleOption2<bool>(false, NotificationOption2.Warning)).WithPublicOption("test", "test");
        var perLanguageOption = perLanguageOption2.ToPublicOption();
        var newValueCodeStyleOption2 = new CodeStyleOption2<bool>(!perLanguageOption2.DefaultValue.Value, perLanguageOption2.DefaultValue.Notification);
        var newValueCodeStyleOption = (CodeStyleOption<bool>)newValueCodeStyleOption2!;

        TestPublicOption(workspace, perLanguageOption, language: LanguageNames.CSharp, newValueCodeStyleOption);
        TestInternalOption(workspace, perLanguageOption2, language: LanguageNames.CSharp, newValueCodeStyleOption2);

        var optionService = GetLegacyGlobalOptionService(workspace.Services);
        var originalOptionSet = new SolutionOptionSet(optionService);
    }

    [Fact]
    public void TestLanguageSpecificCodeStyleOptions()
    {
        using var workspace = new AdhocWorkspace();
        var option2 = new Option2<CodeStyleOption2<bool>>("test", new CodeStyleOption2<bool>(false, NotificationOption2.Warning)).WithPublicOption("test", "test");
        var option = option2.ToPublicOption();
        var newValueCodeStyleOption2 = new CodeStyleOption2<bool>(!option2.DefaultValue.Value, option2.DefaultValue.Notification);
        var newValueCodeStyleOption = (CodeStyleOption<bool>)newValueCodeStyleOption2!;

        TestPublicOption(workspace, option, language: null, newValueCodeStyleOption);
        TestInternalOption(workspace, option2, language: null, newValueCodeStyleOption2);
    }

    private static void TestPublicOption(Workspace workspace, IPublicOption option, string? language, CodeStyleOption<bool> newPublicValue)
    {
        var optionService = GetLegacyGlobalOptionService(workspace.Services);
        var originalOptionSet = new SolutionOptionSet(optionService);

        var optionKey = new OptionKey(option, language);

        //  1. WithChangedOption(OptionKey), GetOption(OptionKey)/GetOption<T>(OptionKey)
        var newOptionSet = originalOptionSet.WithChangedOption(optionKey, newPublicValue);
        Assert.Equal(newPublicValue, newOptionSet.GetOption(optionKey));
        Assert.IsType<CodeStyleOption<bool>>(newOptionSet.GetOption(optionKey));

        // Verify "T GetOption<T>(OptionKey)" works for T being a public code style option type.
        Assert.Equal(newPublicValue, newOptionSet.GetOption<CodeStyleOption<bool>>(optionKey));
    }

    private static void TestInternalOption(Workspace workspace, IOption2 option, string? language, CodeStyleOption2<bool> newValue)
    {
        var optionService = GetLegacyGlobalOptionService(workspace.Services);
        var optionKey = new OptionKey2(option, language);

        optionService.SetOptions([new KeyValuePair<OptionKey2, object?>(optionKey, newValue)], []);
        Assert.Equal(newValue, optionService.GlobalOptions.GetOption<CodeStyleOption2<bool>>(optionKey));
    }
}
