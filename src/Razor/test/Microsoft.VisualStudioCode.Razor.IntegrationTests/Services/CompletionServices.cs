// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Services for completion (IntelliSense) operations in integration tests.
/// </summary>
public class CompletionServices(IntegrationTestServices testServices) : ServiceBase(testServices)
{

    /// <summary>
    /// Triggers IntelliSense completion and waits for it to appear.
    /// </summary>
    /// <param name="waitForList">If true, waits for the completion list to appear.</param>
    /// <param name="timeout">Timeout for waiting. Uses LspTimeout if not specified.</param>
    public async Task<bool> TriggerAsync(bool waitForList = true, TimeSpan? timeout = null)
    {
        await TestServices.Input.PressWithPrimaryModifierAsync(SpecialKey.Space);

        if (waitForList)
        {
            return await WaitForListAsync(timeout);
        }

        return true;
    }

    /// <summary>
    /// Waits for the completion list to appear.
    /// </summary>
    public async Task<bool> WaitForListAsync(TimeSpan? timeout = null)
    {
        timeout ??= TestServices.Settings.LspTimeout;

        try
        {
            // The suggest widget has class "visible" when shown
            await TestServices.Playwright.Page.Locator(".suggest-widget.visible")
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
    /// Gets the items in the completion list.
    /// </summary>
    public async Task<List<string>> GetItemsAsync()
    {
        var results = new List<string>();

        // Wait a moment for completion items to populate
        await WaitForListAsync();

        // Try to get items using JavaScript which can access more of the DOM
        var itemTexts = await TestServices.Playwright.Page.EvaluateAsync<string[]>(@"
            (() => {
                const widget = document.querySelector('.suggest-widget');
                if (!widget) return [];
                
                // Look for the list container - VS Code uses virtualized lists
                const listContainer = widget.querySelector('.monaco-list');
                if (!listContainer) return [];
                
                // Get visible row contents
                const rows = listContainer.querySelectorAll('.monaco-list-row');
                const items = [];
                
                for (const row of rows) {
                    // Try different ways to get the text
                    const label = row.querySelector('.monaco-icon-label-container');
                    if (label) {
                        items.push(label.textContent || '');
                        continue;
                    }
                    
                    const labelName = row.querySelector('.label-name');
                    if (labelName) {
                        items.push(labelName.textContent || '');
                        continue;
                    }
                    
                    // Fallback to row text
                    items.push(row.textContent || '');
                }
                
                return items;
            })()
        ") ?? [];

        if (itemTexts.Length > 0)
        {
            results.AddRange(itemTexts.Where(t => !string.IsNullOrWhiteSpace(t)));
            TestServices.Logger.Log($"Found {results.Count} completion items via JS");
            if (results.Count > 0)
            {
                TestServices.Logger.Log($"First few items: {string.Join(", ", results.Take(5))}");
            }

            return results;
        }

        // Debug: dump the full suggest widget structure
        var debugInfo = await TestServices.Playwright.Page.EvaluateAsync<string>(@"
            (() => {
                const widget = document.querySelector('.suggest-widget');
                if (!widget) return 'No suggest widget found';
                
                let info = `Widget classes: ${widget.className}. `;
                
                // Check for message element (means no suggestions)
                const message = widget.querySelector('.message');
                if (message) {
                    info += `Message: '${message.textContent}'. `;
                }
                
                const listElement = widget.querySelector('.monaco-list');
                if (listElement) {
                    const rows = listElement.querySelectorAll('.monaco-list-row');
                    info += `Rows: ${rows.length}. `;
                    if (rows.length > 0) {
                        info += `First row: '${rows[0].textContent?.substring(0, 50)}'. `;
                    }
                }
                
                return info;
            })()
        ");
        TestServices.Logger.Log($"Suggest widget debug: {debugInfo}");

        return results;
    }
}
