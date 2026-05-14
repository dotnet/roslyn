// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests;

/// <summary>
/// E2E tests for code actions (Quick Fix, refactoring) in Razor files.
/// </summary>
public class CodeActionsTests(ITestOutputHelper output) : VSCodeIntegrationTestBase(output)
{
    [Fact]
    public Task CodeAction_AddUsing_AddsUsingDirective() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange - Open Counter.razor and add an unresolved type
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Go to the @code block and type an unresolved type
        await TestServices.Editor.GoToLineAsync(15);
        await TestServices.Input.TypeAsync("StringBuilder sb;");
        await TestServices.Editor.SaveAsync();

        // Wait for diagnostics (error squiggle on StringBuilder)
        await TestServices.Diagnostics.WaitForDiagnosticsAsync(expectErrors: true, timeout: TimeSpan.FromSeconds(10));

        // Position cursor on StringBuilder
        await TestServices.Editor.GoToWordAsync("StringBuilder");

        // Act - Open Quick Fix menu and select first action (should be "using System.Text;")
        var hasCodeActions = await TestServices.CodeAction.OpenQuickFixMenuAsync();
        Assert.True(hasCodeActions, "Expected code actions for unresolved type");

        // Select the first code action (Add using)
        await TestServices.Input.PressAsync(SpecialKey.Enter);

        // Wait for the code action to be applied by checking for editor dirty state then save
        await TestServices.Editor.WaitForEditorDirtyAsync();

        // Assert - verify the using directive was added
        var text = await TestServices.Editor.WaitForEditorTextChangeAsync();
        Assert.Contains("using System.Text", text);

        // Also verify the error is gone
        await TestServices.Diagnostics.WaitForDiagnosticsAsync(expectErrors: false, timeout: TimeSpan.FromSeconds(10));
    });
}
