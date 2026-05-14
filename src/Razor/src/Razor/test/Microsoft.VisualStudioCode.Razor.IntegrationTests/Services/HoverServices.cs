// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Services for hover (Quick Info) operations in integration tests.
/// </summary>
public class HoverServices(IntegrationTestServices testServices) : ServiceBase(testServices)
{
    /// <summary>
    /// Triggers hover information at the current cursor position and waits for it to appear.
    /// </summary>
    /// <param name="waitForHover">If true, waits for the hover content to appear.</param>
    /// <param name="timeout">Timeout for waiting. Uses LspTimeout if not specified.</param>
    public async Task<bool> TriggerAsync(bool waitForHover = true, TimeSpan? timeout = null)
    {
        // Move mouse to the current cursor position and hover
        // Use First since there may be multiple cursor elements (e.g., in split editors or interactive window)
        var cursorLocator = TestServices.Playwright.Page.Locator(".cursor");
        if (await cursorLocator.CountAsync() == 0)
        {
            return false;
        }

        var box = await cursorLocator.First.BoundingBoxAsync();
        if (box == null)
        {
            return false;
        }

        await TestServices.Playwright.Page.Mouse.MoveAsync(box.X + (box.Width / 2), box.Y + (box.Height / 2));

        if (waitForHover)
        {
            return await WaitForAsync(timeout);
        }

        return true;
    }

    /// <summary>
    /// Waits for hover content to appear.
    /// </summary>
    public async Task<bool> WaitForAsync(TimeSpan? timeout = null)
    {
        timeout ??= TestServices.Settings.LspTimeout;

        try
        {
            // Use First since there may be multiple hover widgets
            await TestServices.Playwright.Page.Locator(".monaco-hover-content").First
                .WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = (float)timeout.Value.TotalMilliseconds
                });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the hover content text.
    /// </summary>
    public async Task<string?> GetContentAsync()
    {
        var hoverLocator = TestServices.Playwright.Page.Locator(".monaco-hover-content");
        if (await hoverLocator.CountAsync() == 0)
        {
            return null;
        }

        return await hoverLocator.First.TextContentAsync();
    }

    /// <summary>
    /// Triggers hover and waits for the hover content text to appear.
    /// Waits for actual content to appear (not "Loading...").
    /// </summary>
    /// <returns>The hover content text, or null if hover failed to appear.</returns>
    public async Task<string?> WaitForContentAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);

        var hasHover = await TriggerAsync();
        if (!hasHover)
        {
            return null;
        }

        // Wait for actual content, not "Loading..."
        string? content = null;
        try
        {
            await Helper.WaitForConditionAsync(
                async () =>
                {
                    content = await GetContentAsync();
                    return !string.IsNullOrEmpty(content) && !content.Equals("Loading...", StringComparison.OrdinalIgnoreCase);
                },
                timeout.Value);
        }
        catch (TimeoutException)
        {
            // Return whatever we have, even if it's still "Loading..."
        }

        return content ?? await GetContentAsync();
    }
}
