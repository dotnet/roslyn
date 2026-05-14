// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests;

/// <summary>
/// E2E tests for Go to Definition in Razor files.
/// </summary>
public class GoToDefinitionTests(ITestOutputHelper output) : VSCodeIntegrationTestBase(output)
{
    [Fact]
    public Task GoToDefinition_MethodReference_NavigatesToMethodDefinition() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange - Open Counter.razor from the dotnet new blazor template
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Navigate to the IncrementCount method reference in @onclick
        // Line 10: <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>
        await TestServices.Editor.GoToWordAsync("IncrementCount");

        // Get position before Go to Definition (should be at line 10, the usage site)
        var beforePosition = await TestServices.Editor.GetCursorPositionAsync();
        Assert.NotNull(beforePosition);
        Assert.Equal(10, beforePosition.Value.Line);

        // Act - Go to Definition (no expected file - navigating within same file)
        await TestServices.Navigation.GoToDefinitionAsync();

        // Assert - cursor should now be at the method definition (line 15)
        var afterPosition = await TestServices.Editor.GetCursorPositionAsync();
        Assert.NotNull(afterPosition);
        Assert.Equal(15, afterPosition.Value.Line); // Method definition line
    });

    [Fact]
    public Task GoToDefinition_FieldUsage_NavigatesToFieldDeclaration() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange - Open Counter.razor
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Navigate to line 17 which contains "currentCount++;"
        await TestServices.Editor.GoToLineAsync(17, column: 9);

        var beforePosition = await TestServices.Editor.GetCursorPositionAsync();
        Assert.NotNull(beforePosition);
        Assert.Equal(17, beforePosition.Value.Line);

        // Act - Go to Definition should navigate to the field declaration
        await TestServices.Navigation.GoToDefinitionAsync();

        // Assert - cursor should now be at the field declaration (line 13)
        var afterPosition = await TestServices.Editor.GetCursorPositionAsync();
        Assert.NotNull(afterPosition);
        Assert.Equal(13, afterPosition.Value.Line); // Field declaration line
    });

    [Fact]
    public Task GoToDefinition_ComponentTag_NavigatesToComponentFile() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange - Open Home.razor and add a Counter component reference
        await TestServices.Editor.OpenFileAsync("Components/Pages/Home.razor");

        // Go to end of file and add a Counter component
        await TestServices.Input.PressWithPrimaryModifierAsync(SpecialKey.End);
        await TestServices.Input.TypeAsync("\n<Counter />");
        await TestServices.Editor.SaveAsync();

        // Position cursor on Counter
        await TestServices.Editor.GoToWordAsync("Counter");

        // Act - Go to Definition should open Counter.razor
        await TestServices.Navigation.GoToDefinitionAsync("Counter.razor");

        // Assert - should have navigated to Counter.razor
        var currentFile = await TestServices.Editor.GetCurrentFileNameAsync();
        Assert.NotNull(currentFile);
        Assert.Contains("Counter.razor", currentFile, StringComparison.OrdinalIgnoreCase);
    });
}

