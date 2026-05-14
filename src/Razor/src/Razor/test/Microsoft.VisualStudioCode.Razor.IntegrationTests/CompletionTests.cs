// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests;

/// <summary>
/// E2E tests for IntelliSense completion in Razor files.
/// </summary>
public class CompletionTests(ITestOutputHelper output) : VSCodeIntegrationTestBase(output)
{
    [Fact]
    public Task CSharpCompletion_InCodeBlock_ShowsCSharpItems() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Navigate to inside the IncrementCount method (line 17: currentCount++;)
        // and add a new line to type code
        await TestServices.Editor.GoToLineAsync(17);
        await TestServices.Input.PressAsync(SpecialKey.End); // Go to end of line
        await TestServices.Input.PressAsync(SpecialKey.Enter); // New line

        // Type a partial identifier - this naturally triggers completions
        await TestServices.Input.TypeAsync("current");

        // Wait for completion list to appear
        var hasCompletions = await TestServices.Completion.WaitForListAsync();
        if (!hasCompletions)
        {
            // If not visible yet, trigger explicitly
            hasCompletions = await TestServices.Completion.TriggerAsync();
        }

        Assert.True(hasCompletions, "Expected completion list to appear");

        // Assert - look for currentCount in completions
        var items = await TestServices.Completion.GetItemsAsync();
        Assert.True(
            items.Any(i => i == "currentCount"),
            $"Expected C# completions in @code block. Found: {string.Join(", ", items.Take(10))}");
    });

    [Fact]
    public Task HtmlCompletion_InMarkup_ShowsHtmlElements() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Navigate to the end of the h1 tag line (line 5: <h1>Counter</h1>)
        await TestServices.Editor.GoToLineAsync(5);
        await TestServices.Input.PressAsync(SpecialKey.End);
        await TestServices.Input.PressAsync(SpecialKey.Enter);

        // Type a partial HTML tag - VS Code should show completions
        await TestServices.Input.TypeAsync("<di");

        // Wait for completion list to appear
        var hasCompletions = await TestServices.Completion.WaitForListAsync();
        if (!hasCompletions)
        {
            // If not visible yet, trigger explicitly
            await TestServices.Completion.TriggerAsync();
        }

        // Get completion items
        var items = await TestServices.Completion.GetItemsAsync();

        // Assert - look for div in completions
        Assert.True(
            items.Any(i => i.Contains("div", StringComparison.OrdinalIgnoreCase)),
            $"Expected HTML element completions. Found {items.Count} items: {string.Join(", ", items.Take(10))}");
    });

    [Fact]
    public Task RazorDirectiveCompletion_AfterAt_ShowsDirectives() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Navigate to line 2 (after @rendermode) and add a new line
        await TestServices.Editor.GoToLineAsync(2);
        await TestServices.Input.PressAsync(SpecialKey.End);
        await TestServices.Input.PressAsync(SpecialKey.Enter);

        // Type @ to trigger Razor completions
        await TestServices.Input.TypeAsync("@in"); // Partial "@inject"

        // Wait for completion list to appear
        var hasCompletions = await TestServices.Completion.WaitForListAsync();
        if (!hasCompletions)
        {
            // If not visible yet, trigger explicitly
            await TestServices.Completion.TriggerAsync();
        }

        // Get completion items
        var items = await TestServices.Completion.GetItemsAsync();

        // Assert - look for Razor directives starting with "in"
        var hasRazorDirectives = items.Any(i =>
            i.Contains("inject", StringComparison.OrdinalIgnoreCase) ||
            i.Contains("inherits", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasRazorDirectives, $"Expected Razor directive completions after @. Found: {string.Join(", ", items.Take(10))}");
    });
}

