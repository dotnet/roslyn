﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Options.UnitTests;

[UseExportProvider]
public class SolutionAnalyzerConfigOptionsUpdaterTests
{
    private static TestWorkspace CreateWorkspace()
    {
        var workspace = new TestWorkspace(LspTestCompositions.LanguageServerProtocol
            .RemoveParts(typeof(MockFallbackAnalyzerConfigOptionsProvider)));

        var updater = (SolutionAnalyzerConfigOptionsUpdater)workspace.ExportProvider.GetExports<IEventListener>().Single(e => e.Value is SolutionAnalyzerConfigOptionsUpdater).Value;
        var listenerProvider = workspace.GetService<MockWorkspaceEventListenerProvider>();
        listenerProvider.EventListeners = [updater];

        return workspace;
    }

    [Fact]
    public void FlowsGlobalOptionsToWorkspace()
    {
        using var workspace = CreateWorkspace();

        var globalOptions = workspace.GetService<IGlobalOptionService>();

        // default values:
        Assert.False(globalOptions.GetOption(FormattingOptions2.InsertFinalNewLine));
        Assert.Equal(4, globalOptions.GetOption(FormattingOptions2.IndentationSize, LanguageNames.CSharp));
        Assert.Equal(4, globalOptions.GetOption(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic));

        // C# project hasn't been loaded to the workspace yet:
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        var project = new TestHostProject(workspace, "proj1", LanguageNames.CSharp);
        workspace.AddTestProject(project);

        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.CSharp, "false");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.CSharp, "4");

        globalOptions.SetGlobalOptions(
        [
            new KeyValuePair<OptionKey2, object?>(FormattingOptions2.InsertFinalNewLine, true),
            new KeyValuePair<OptionKey2, object?>(new OptionKey2(FormattingOptions2.IndentationSize, LanguageNames.CSharp), 3),
            new KeyValuePair<OptionKey2, object?>(new OptionKey2(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic), 5)
        ]);

        // editorconfig option set as a global option should flow to the solution snapshot:
        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.CSharp, "true");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.CSharp, "3");

        workspace.OnProjectRemoved(project.Id);

        // last C# project removed -> fallback options removed:
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        workspace.AddTestProject(new TestHostProject(workspace, "proj2", LanguageNames.VisualBasic));

        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.VisualBasic, "true");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic, "5");

        Assert.False(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(LanguageNames.CSharp, out _));

        // VB and C# projects added:

        workspace.AddTestProject(new TestHostProject(workspace, "proj3", LanguageNames.CSharp));

        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.VisualBasic, "true");
        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.CSharp, "true");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic, "5");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.CSharp, "3");

        globalOptions.SetGlobalOption(FormattingOptions2.InsertFinalNewLine, false);

        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.VisualBasic, "false");
        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.CSharp, "false");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic, "5");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.CSharp, "3");

        void AssertOptionValue(IOption2 option, string language, string expectedValue)
        {
            Assert.True(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(language, out var fallbackOptions));
            Assert.True(fallbackOptions!.TryGetValue(option.Definition.ConfigName, out var configValue));
            Assert.Equal(expectedValue, configValue);
        }
    }

    [Fact]
    public void IgnoresNonEditorConfigOptions()
    {
        using var workspace = CreateWorkspace();

        var globalOptions = workspace.GetService<IGlobalOptionService>();

        var option = new Option2<bool>("test_option", defaultValue: false, isEditorConfigOption: false);

        Assert.False(globalOptions.GetOption(option));
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        var project = new TestHostProject(workspace, "proj1", LanguageNames.CSharp);
        workspace.AddTestProject(project);

        var optionsAfterProjectAdded = workspace.CurrentSolution.FallbackAnalyzerOptions;

        Assert.NotEmpty(optionsAfterProjectAdded);
        Assert.False(optionsAfterProjectAdded.ContainsKey("test_option"));

        globalOptions.SetGlobalOption(option, true);

        Assert.Same(optionsAfterProjectAdded, workspace.CurrentSolution.FallbackAnalyzerOptions);
    }
}
