// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Services for code action operations (Quick Fix, refactorings) in integration tests.
/// </summary>
public class CodeActionService(IntegrationTestServices testServices) : ServiceBase(testServices)
{
    /// <summary>
    /// Opens the Quick Fix menu (Ctrl+.) and waits for code actions to appear.
    /// </summary>
    /// <param name="waitForActions">If true, waits for the code actions menu to appear.</param>
    /// <param name="timeout">Timeout for waiting.</param>
    public async Task<bool> OpenQuickFixMenuAsync(bool waitForActions = true, TimeSpan? timeout = null)
    {
        await TestServices.Input.PressWithPrimaryModifierAsync('.');

        if (waitForActions)
        {
            return await WaitForCodeActionsAsync(timeout);
        }

        return true;
    }

    /// <summary>
    /// Waits for the code actions menu to appear.
    /// </summary>
    public async Task<bool> WaitForCodeActionsAsync(TimeSpan? timeout = null)
    {
        timeout ??= TestServices.Settings.LspTimeout;

        try
        {
            // VS Code uses different widgets for code actions depending on context
            // Try the action widget first (lightbulb menu), then fallback to context menu
            await TestServices.Playwright.Page.Locator(".action-widget, .context-view.monaco-menu-container")
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
}
