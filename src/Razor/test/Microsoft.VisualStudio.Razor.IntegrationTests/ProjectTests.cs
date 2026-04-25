// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class ProjectTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task CreateFromTemplateAsync()
    {
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseSolutionAndWaitAsync(ControlledHangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/razor/issues/9200")]
    public async Task ChangeTargetFramework()
    {
        var solutionPath = await TestServices.SolutionExplorer.GetDirectoryNameAsync(ControlledHangMitigatingCancellationToken);

        Assert.Equal(1, GetProjectRazorJsonFileCount());

        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ProjectFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("<TargetFramework", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        var currentLine = await TestServices.Editor.GetCurrentLineTextAsync(ControlledHangMitigatingCancellationToken);
        var tfElement = currentLine.Contains("net6.0")
            ? "<TargetFramework>net7.0</TargetFramework>"
            : "<TargetFramework>net6.0</TargetFramework>";

        await TestServices.Editor.InvokeDeleteLineAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.InsertTextAsync(tfElement + Environment.NewLine, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ProjectFile, saveFile: true, ControlledHangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForProjectSystemAsync(ControlledHangMitigatingCancellationToken);

        // We open the Index.razor file, and wait for 3 RazorComponentElement's to be classified, as that
        // way we know the LSP server is up, running, and has processed both local and library-sourced Components
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForProjectSystemAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("</PageTitle>", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken, count: 3);

        // This is a little odd, but there is no "real" way to check this via VS, and one of the most important things this test can do
        // is ensure that each target framework gets its own project.razor.bin file, and doesn't share one from a cache or anything.
        Assert.Equal(2, GetProjectRazorJsonFileCount());

        int GetProjectRazorJsonFileCount()
            => Directory.EnumerateFiles(solutionPath, "project.razor.*.bin", SearchOption.AllDirectories).Count();
    }

    [IdeFact]
    public async Task OpenExistingProject()
    {
        var solutionPath = await TestServices.SolutionExplorer.GetDirectoryNameAsync(ControlledHangMitigatingCancellationToken);
        var expectedProjectFileName = await TestServices.SolutionExplorer.GetAbsolutePathForProjectRelativeFilePathAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ProjectFile, ControlledHangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.CloseSolutionAndWaitAsync(ControlledHangMitigatingCancellationToken);

        var solutionFileName = Path.Combine(solutionPath, RazorProjectConstants.BlazorSolutionName + ".sln");
        await TestServices.SolutionExplorer.OpenSolutionAsync(solutionFileName, ControlledHangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForProjectSystemAsync(ControlledHangMitigatingCancellationToken);

        // We open the Index.razor file, and wait for 3 RazorComponentElement's to be classified, as that
        // way we know the LSP server is up, running, and has processed both local and library-sourced Components
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForProjectSystemAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("</PageTitle>", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken, count: 3);

        // Make sure the test framework didn't do something weird and create new project
        var actualProjectFileName = await TestServices.SolutionExplorer.GetAbsolutePathForProjectRelativeFilePathAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ProjectFile, ControlledHangMitigatingCancellationToken);
        Assert.Equal(expectedProjectFileName, actualProjectFileName);
    }

    [IdeFact]
    public async Task OpenExistingProject_WithReopenedFile()
    {
        var solutionPath = await TestServices.SolutionExplorer.GetDirectoryNameAsync(ControlledHangMitigatingCancellationToken);
        var expectedProjectFileName = await TestServices.SolutionExplorer.GetAbsolutePathForProjectRelativeFilePathAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ProjectFile, ControlledHangMitigatingCancellationToken);

        // Open SurveyPrompt and make sure its all up and running
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForSemanticClassificationAsync("RazorTagHelperElement", ControlledHangMitigatingCancellationToken, count: 1);

        await TestServices.SolutionExplorer.CloseSolutionAndWaitAsync(ControlledHangMitigatingCancellationToken);

        var solutionFileName = Path.Combine(solutionPath, RazorProjectConstants.BlazorSolutionName + ".sln");
        await TestServices.SolutionExplorer.OpenSolutionAsync(solutionFileName, ControlledHangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForProjectSystemAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForSemanticClassificationAsync("RazorTagHelperElement", ControlledHangMitigatingCancellationToken, count: 1);

        TestServices.Input.Send("1");

        // Make sure the test framework didn't do something weird and create new project
        var actualProjectFileName = await TestServices.SolutionExplorer.GetAbsolutePathForProjectRelativeFilePathAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ProjectFile, ControlledHangMitigatingCancellationToken);
        Assert.Equal(expectedProjectFileName, actualProjectFileName);

        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, saveFile: false, ControlledHangMitigatingCancellationToken);
    }
}
