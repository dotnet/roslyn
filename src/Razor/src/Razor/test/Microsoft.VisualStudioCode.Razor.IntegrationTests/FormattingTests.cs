// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests;

/// <summary>
/// E2E tests for formatting in Razor files.
/// </summary>
public class FormattingTests(ITestOutputHelper output) : VSCodeIntegrationTestBase(output)
{
    [Fact]
    public Task FormatDocument_BadlyIndentedCode_FixesIndentation() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange - Open Counter.razor and add badly indented code
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Go to end of line 14 (blank line before IncrementCount method) and add badly indented code
        await TestServices.Editor.GoToLineAsync(14);
        await TestServices.Input.PressAsync(SpecialKey.End);
        await TestServices.Input.PressAsync(SpecialKey.Enter);
        await TestServices.Input.TypeAsync("private string BadlyIndented=\"test\";");

        // Formatting could be async, so make sure we save after the edit, so WaitForEditorTextChangeAsync works correctly
        await TestServices.Editor.SaveAsync();

        // Act
        await TestServices.Formatting.FormatDocumentAsync();

        // Assert - the code should now be properly indented with spaces
        var afterFormat = await TestServices.Editor.WaitForEditorTextChangeAsync();
        // Formatting should have added proper indentation (4 spaces before "private")
        Assert.Contains("\n    private string BadlyIndented", afterFormat);
    });

    [Fact]
    public Task FormatDocument_MixedHtmlAndCSharp_FormatsCorrectly() => ScreenshotOnFailureAsync(async () =>
    {
        // Arrange
        await TestServices.Editor.OpenFileAsync("Components/Pages/Counter.razor");

        // Add unformatted mixed content at end of line 5 (after </h1>)
        await TestServices.Editor.GoToLineAsync(5);
        await TestServices.Input.PressAsync(SpecialKey.End);
        await TestServices.Input.PressAsync(SpecialKey.Enter);
        // Typing like this is weird, but seems to help reduce flakiness
        await TestServices.Input.TypeAsync("<div>");
        await Task.Delay(100);
        await TestServices.Input.TypeAsync("@");
        await Task.Delay(100);
        // Dismiss completion just in case it gets in the way
        await TestServices.Input.PressAsync(SpecialKey.Escape);
        await TestServices.Input.TypeAsync("{var x=1;}");

        // Act
        await TestServices.Formatting.FormatDocumentAsync();

        // Assert - C# code should be formatted with spaces around operators
        var afterFormat = await TestServices.Editor.WaitForEditorTextChangeAsync();
        Assert.Contains("var x = 1;", afterFormat);
    });
}

