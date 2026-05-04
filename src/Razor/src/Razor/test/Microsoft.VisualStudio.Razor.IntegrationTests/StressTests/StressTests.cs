// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebTools.Languages.Shared.Editor.EditorHelpers;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class StressTests(ITestOutputHelper testOutputHelper) : AbstractStressTest(testOutputHelper)
{
    [ManualRunOnlyIdeFact]
    public async Task AddAndRemoveComponent()
    {
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("h1", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.InvokeDeleteLineAsync(ControlledHangMitigatingCancellationToken);

        await RunStressTestAsync(RunIterationAsync);

        async Task RunIterationAsync(int index, CancellationToken cancellationToken)
        {
            await TestServices.Editor.InsertTextAsync($"<h1>Iteration {index}</h1>{{ENTER}}", cancellationToken);

            await TestServices.Editor.PlaceCaretAsync("h1", charsOffset: -1, cancellationToken);

            await TestServices.Editor.WaitForComponentClassificationAsync(cancellationToken, count: 2, exact: true);

            await TestServices.Editor.InvokeCodeActionAsync("Extract element to new component", cancellationToken);

            await TestServices.Editor.WaitForActiveWindowByFileAsync("Component.razor", cancellationToken);

            var componentFileName = (await TestServices.Editor.GetActiveTextViewAsync(cancellationToken)).TextBuffer.GetFileName();

            await TestServices.Editor.CloseCurrentlyFocusedWindowAsync(cancellationToken, save: true);

            await TestServices.Editor.WaitForActiveWindowByFileAsync("Counter.razor", cancellationToken);

            await TestServices.Editor.WaitForComponentClassificationAsync(cancellationToken, count: 3, exact: true);

            await Task.Delay(500);

            File.Delete(componentFileName);

            await TestServices.Editor.WaitForComponentClassificationAsync(cancellationToken, count: 2, exact: true);

            await Task.Delay(500);

            await TestServices.Editor.PlaceCaretAsync("Component", charsOffset: -1, cancellationToken);

            await TestServices.Editor.InvokeDeleteLineAsync(cancellationToken);
        }
    }

    [ManualRunOnlyIdeFact]
    public async Task RenameComponentAttribute()
    {
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        var attributeName = "Title";

        await TestServices.Editor.PlaceCaretAsync($"{attributeName}=", charsOffset: -1, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);
        // Make sure the test file is still what we expect, with one attribute
        await TestServices.Editor.WaitForSemanticClassificationAsync("RazorComponentAttribute", ControlledHangMitigatingCancellationToken, count: 1, exact: true);

        await RunStressTestAsync(RunIterationAsync);

        async Task RunIterationAsync(int index, CancellationToken cancellationToken)
        {
            attributeName = $"RenamedTitle{index}";

            await Task.Delay(500);

            await TestServices.Editor.InvokeRenameAsync(cancellationToken);
            TestServices.Input.Send($"{attributeName}{{ENTER}}");

            // The rename operation causes SurveyPrompt.razor to be opened
            await TestServices.Editor.WaitForActiveWindowByFileAsync("SurveyPrompt.razor", cancellationToken);
            await TestServices.Editor.VerifyTextContainsAsync($"public string? {attributeName} {{ get; set; }}", cancellationToken);
            await TestServices.Editor.VerifyTextContainsAsync($"@{attributeName}", cancellationToken);

            await TestServices.Editor.ValidateNoDiscoColorsAsync(cancellationToken);

            await TestServices.Editor.CloseCurrentlyFocusedWindowAsync(HangMitigatingCancellationToken, save: true);

            await TestServices.Editor.WaitForActiveWindowByFileAsync("Index.razor", cancellationToken);
            await TestServices.Editor.VerifyTextContainsAsync($"<SurveyPrompt {attributeName}=", cancellationToken);

            // Wait for our new attribute to color correctly
            await TestServices.Editor.WaitForSemanticClassificationAsync("RazorComponentAttribute", cancellationToken, count: 1, exact: true);

            await TestServices.Editor.ValidateNoDiscoColorsAsync(cancellationToken);

            await Task.Delay(500);

            // Reset the editor state for the next iteration
            await TestServices.Editor.PlaceCaretAsync($"{attributeName}=", charsOffset: -1, cancellationToken);
        }
    }
}
