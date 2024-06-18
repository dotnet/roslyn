// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Options.UnitTests;

[UseExportProvider]
public class SolutionAnalyzerConfigOptionsUpdaterTests
{
    private static TestWorkspace CreateWorkspace(
        out SolutionAnalyzerConfigOptionsUpdater updater,
        out IAsynchronousOperationWaiter workspaceOperations)
    {
        var workspace = new TestWorkspace(EditorTestCompositions.LanguageServerProtocol);

        updater = (SolutionAnalyzerConfigOptionsUpdater)workspace.ExportProvider.GetExports<IEventListener>().Single(e => e.Value is SolutionAnalyzerConfigOptionsUpdater).Value;
        var listenerProvider = workspace.GetService<MockWorkspaceEventListenerProvider>();
        listenerProvider.EventListeners = [updater];

        Assert.NotNull(workspace.Services.GetService<IWorkspaceEventListenerService>());
        workspaceOperations = workspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace);

        return workspace;
    }

    [Fact]
    public async Task FlowsGlobalOptionsToWorkspace()
    {
        var workspace = CreateWorkspace(out var updater, out var workspaceOperations);

        var globalOptions = workspace.GetService<IGlobalOptionService>();

        // default value is false:
        Assert.False(globalOptions.GetOption(FormattingOptions2.InsertFinalNewLine));

        // C# project hasn't been loaded to the workspace yet:
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        var project = new TestHostProject(workspace, "proj1", LanguageNames.CSharp);
        workspace.AddTestProject(project);
        await workspaceOperations.ExpeditedWaitAsync();

        AssertOptionValue(LanguageNames.CSharp, "false");

        globalOptions.SetGlobalOption(FormattingOptions2.InsertFinalNewLine, true);

        // editorconfig option set as a global option should flow to the solution snapshot:
        AssertOptionValue(LanguageNames.CSharp, "true");

        workspace.OnProjectRemoved(project.Id);
        await workspaceOperations.ExpeditedWaitAsync();

        // last C# project removed -> fallback options removed:
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        workspace.AddTestProject(new TestHostProject(workspace, "proj2", LanguageNames.VisualBasic));
        await workspaceOperations.ExpeditedWaitAsync();

        AssertOptionValue(LanguageNames.VisualBasic, "true");

        Assert.False(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(LanguageNames.CSharp, out _));

        // VB and C# projects added:

        workspace.AddTestProject(new TestHostProject(workspace, "proj3", LanguageNames.CSharp));
        await workspaceOperations.ExpeditedWaitAsync();

        AssertOptionValue(LanguageNames.VisualBasic, "true");
        AssertOptionValue(LanguageNames.CSharp, "true");

        globalOptions.SetGlobalOption(FormattingOptions2.InsertFinalNewLine, false);

        AssertOptionValue(LanguageNames.VisualBasic, "false");
        AssertOptionValue(LanguageNames.CSharp, "false");

        workspace.Dispose();

        // updater should be removed on workspace disposal:
        Assert.False(updater.GetTestAccessor().HasWorkspaceUpdaters);

        void AssertOptionValue(string language, string expectedValue)
        {
            Assert.True(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(language, out var fallbackOptions));
            Assert.True(fallbackOptions!.TryGetValue(FormattingOptions2.InsertFinalNewLine.Definition.ConfigName, out var configValue));
            Assert.Equal(expectedValue, configValue);
        }
    }

    [Fact]
    public async Task IgnoresNonEditorConfigOptions()
    {
        var workspace = CreateWorkspace(out var updater, out var workspaceOperations);

        var globalOptions = workspace.GetService<IGlobalOptionService>();

        var option = new Option2<bool>("test_option", defaultValue: false, isEditorConfigOption: false);

        Assert.False(globalOptions.GetOption(option));
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        var project = new TestHostProject(workspace, "proj1", LanguageNames.CSharp);
        workspace.AddTestProject(project);
        await workspaceOperations.ExpeditedWaitAsync();

        var optionsAfterProjectAdded = workspace.CurrentSolution.FallbackAnalyzerOptions;

        Assert.NotEmpty(optionsAfterProjectAdded);
        Assert.False(optionsAfterProjectAdded.ContainsKey("test_option"));

        globalOptions.SetGlobalOption(option, true);

        Assert.Same(optionsAfterProjectAdded, workspace.CurrentSolution.FallbackAnalyzerOptions);
    }
}
