// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests;

/// <summary>
/// E2E tests for Find All References in Razor files.
/// </summary>
public class FindReferencesTests(ITestOutputHelper output) : VSCodeIntegrationTestBase(output)
{
    [Fact]
    public Task FindReferences_Field_ShowsMultipleUsages() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange - Open Counter.razor
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Navigate to the 'currentCount' field definition
        // Line 13: "private int currentCount = 0;"
        await TestServices.Editor.GoToLineAsync(13, column: 17);

        // Act - Find all references
        await TestServices.Navigation.FindAllReferencesAsync();

        // Assert - verify the references panel/peek view shows references
        // currentCount should have at least 3 references:
        // 1. Definition: "private int currentCount = 0;"
        // 2. Usage in markup: "@currentCount"
        // 3. Usage in method: "currentCount++;"
        var referencesCount = await TestServices.Navigation.GetReferencesCountAsync();
        Assert.True(referencesCount >= 3, $"Expected at least 3 references for currentCount, found {referencesCount}");
    });

    [Fact]
    public Task FindReferences_Method_ShowsCallSites() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange - Open Counter.razor
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Navigate to the IncrementCount method definition
        // Line 15: "private void IncrementCount()"
        await TestServices.Editor.GoToLineAsync(15, column: 18);

        // Act - Find all references
        await TestServices.Navigation.FindAllReferencesAsync();

        // Assert - should show at least 2 references:
        // 1. The definition
        // 2. The @onclick="IncrementCount" usage
        var referencesCount = await TestServices.Navigation.GetReferencesCountAsync();
        Assert.True(referencesCount >= 2, $"Expected at least 2 references for IncrementCount, found {referencesCount}");
    });
}

