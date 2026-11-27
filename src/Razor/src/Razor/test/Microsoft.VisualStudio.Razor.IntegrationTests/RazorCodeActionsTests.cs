// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class RazorCodeActionsTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task RazorCodeActions_AddUsing()
    {
        // Create Warnings by removing usings
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ImportsRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("", ControlledHangMitigatingCancellationToken);

        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        var position = await TestServices.Editor.SetTextAsync("<Su$$rveyPrompt></SurveyPrompt>", ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync(position, ControlledHangMitigatingCancellationToken);

        // Act
        var codeActions = await TestServices.Editor.InvokeCodeActionListAsync(ControlledHangMitigatingCancellationToken);

        // Assert

        // We expect two groups, one for Razor, one for Html
        Assert.Equal(2, codeActions.Count());
        // Razor should be first
        var codeActionSet = codeActions.First();
        var usingString = $"@using {RazorProjectConstants.BlazorProjectName}.Shared";
        var codeAction = Assert.Single(codeActionSet.Actions, a => a.DisplayText.Equals(usingString));

        await TestServices.Editor.InvokeCodeActionAsync(codeAction, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.VerifyTextContainsAsync(usingString, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task RazorCodeActions_ExtractToCodeBehind()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("@code", 1, ControlledHangMitigatingCancellationToken);

        // Act
        var codeActions = await TestServices.Editor.InvokeCodeActionListAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var codeActionSet = Assert.Single(codeActions);
        var codeAction = Assert.Single(codeActionSet.Actions, a => a.DisplayText.Equals("Extract block to code behind"));

        await TestServices.Editor.InvokeCodeActionAsync(codeAction, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForActiveWindowByFileAsync("Counter.razor.cs", ControlledHangMitigatingCancellationToken);
    }
}
