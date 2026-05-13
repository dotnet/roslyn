// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests;

/// <summary>
/// E2E tests for diagnostics (error squiggles) in Razor files.
/// </summary>
public class DiagnosticsTests(ITestOutputHelper output) : VSCodeIntegrationTestBase(output)
{
    [Fact]
    public Task Diagnostics_CSharpSyntaxError_ShowsInProblemsPanel() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange
        await TestServices.Editor.OpenFileAsync("Components/Pages/Home.razor");
        await TestServices.Diagnostics.OpenProblemsPanelAsync();

        // Act - type something that will cause a C# error (missing semicolon)
        await TestServices.Editor.SelectAllAsync();
        await TestServices.Input.TypeAsync("@{ int x = 5 }"); // Missing semicolon

        // Assert - wait for CS1002 (semicolon expected) to appear in Problems panel
        await TestServices.Diagnostics.WaitForProblemAsync("CS1002", timeout: TimeSpan.FromSeconds(5));
    });

    [Fact]
    public Task Diagnostics_FixError_RemovesFromProblemsPanel() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange
        await TestServices.Editor.OpenFileAsync("Components/Pages/Home.razor");
        await TestServices.Diagnostics.OpenProblemsPanelAsync();

        // Introduce an error
        await TestServices.Editor.SelectAllAsync();
        await TestServices.Input.TypeAsync("@{ int x = }"); // Missing value - CS1525

        // Wait for error to appear
        await TestServices.Diagnostics.WaitForDiagnosticsAsync(expectErrors: true, timeout: TimeSpan.FromSeconds(5));

        // Act - fix the error by adding the missing value
        await TestServices.Input.PressAsync(SpecialKey.Backspace);
        await TestServices.Input.TypeAsync("5; }");

        // Assert - wait for errors to disappear
        await TestServices.Diagnostics.WaitForDiagnosticsAsync(expectErrors: false, timeout: TimeSpan.FromSeconds(5));
    });

    [Fact]
    public Task Diagnostics_ValidFile_NoErrors() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");
        await TestServices.Diagnostics.OpenProblemsPanelAsync();

        // Wait for diagnostics to settle - expect no errors on a valid file
        await TestServices.Diagnostics.WaitForDiagnosticsAsync(expectErrors: false, timeout: TimeSpan.FromSeconds(10));

        // Act
        var hasErrors = await TestServices.Diagnostics.HasErrorsAsync();

        // Assert
        Assert.False(hasErrors, "Valid Razor file should have no error diagnostics");
    });

    [Fact]
    public Task Diagnostics_UnclosedTag_ShowsRZ9980() => ScreenshotOnFailureAsync(async () =>
    {
        var fileName = Path.Combine(TestServices.Workspace.WorkspacePath, "Components/Pages/Home.razor");
        var contents = File.ReadAllText(fileName);
        contents = contents.Replace("</PageTitle>", ""); // Remove closing tag to introduce error
        File.WriteAllText(fileName, contents);

        await TestServices.Editor.OpenFileAsync("Components/Pages/Home.razor");

        await TestServices.Diagnostics.OpenProblemsPanelAsync();

        await TestServices.Diagnostics.WaitForProblemAsync("RZ1034", timeout: TimeSpan.FromSeconds(5));

        var problems = await TestServices.Diagnostics.GetProblemsAsync();
        Assert.Contains(problems, p => p.Contains("RZ1034"));
    });

    [Fact]
    public Task Diagnostics_UnclosedComponentTag_ShowsRZ9980() => ScreenshotOnFailureAsync(async () =>
    {
        // Typing is problematic, and automatic close tag insertion gets in the way, so it's easier
        // to just modify the file on disk directly.
        var fileName = Path.Combine(TestServices.Workspace.WorkspacePath, "Components/Pages/Home.razor");
        var contents = File.ReadAllText(fileName);
        contents = contents.Replace("</h1>", ""); // Remove closing tag to introduce error
        File.WriteAllText(fileName, contents);

        await TestServices.Editor.OpenFileAsync("Components/Pages/Home.razor");

        await TestServices.Diagnostics.OpenProblemsPanelAsync();

        await TestServices.Diagnostics.WaitForProblemAsync("RZ9980", timeout: TimeSpan.FromSeconds(5));

        var problems = await TestServices.Diagnostics.GetProblemsAsync();
        Assert.Contains(problems, p => p.Contains("RZ9980"));
    });

    [Fact]
    public Task Diagnostics_UnusedDirective_IsFadedWithoutWarningSquiggle() => ScreenshotOnFailureAsync(async () =>
    {
        var fileName = Path.Combine(TestServices.Workspace.WorkspacePath, "UnusedDirective.cshtml");
        await File.WriteAllTextAsync(fileName, """
            @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

            <p>Hello</p>
            """);

        await TestServices.Editor.OpenFileAsync("UnusedDirective.cshtml");

        await TestServices.Diagnostics.WaitForUnnecessaryCodeAsync(timeout: TimeSpan.FromSeconds(10));

        var hasWarnings = await TestServices.Diagnostics.HasWarningsAsync();
        Assert.False(hasWarnings, "Unused directives should be faded in VS Code, not shown with warning squiggles.");

        await TestServices.Diagnostics.OpenProblemsPanelAsync();
        var problems = await TestServices.Diagnostics.GetProblemsAsync();
        Assert.DoesNotContain(problems, p => p.Contains("RZ0005", StringComparison.OrdinalIgnoreCase));
    });
}

