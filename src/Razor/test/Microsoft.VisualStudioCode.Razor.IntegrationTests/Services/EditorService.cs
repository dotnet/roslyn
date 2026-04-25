// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Helper methods for interacting with VS Code's editor in Playwright tests.
/// Provides high-level abstractions for common editor operations.
/// </summary>
public class EditorService(IntegrationTestServices testServices) : ServiceBase(testServices)
{
    // Track the currently open file's relative path
    private string? _currentOpenFile;

    /// <summary>
    /// Gets the active editor's text content by saving the file and reading from disk.
    /// Waits for the file contents to change from their on-disk state to ensure VS Code has flushed updates.
    /// Use this when you expect the editor buffer to differ from what's currently on disk.
    /// </summary>
    public async Task<string> WaitForEditorTextChangeAsync()
    {
        if (_currentOpenFile == null)
        {
            TestServices.Logger.Log("WaitForEditorTextChangeAsync: No file currently open");
            return string.Empty;
        }

        var filePath = Path.Combine(TestServices.Workspace.WorkspacePath, _currentOpenFile);

        // Read the original file contents before saving
        var originalContents = "";
        try
        {
            originalContents = await ReadFileExclusiveAsync(filePath);
            TestServices.Logger.Log($"WaitForEditorTextChangeAsync: BEFORE contents ({originalContents.Length} chars):\n{originalContents}");
        }
        catch (IOException ex)
        {
            TestServices.Logger.Log($"WaitForEditorTextChangeAsync: Failed to read original file: {ex.Message}");
        }

        // Trigger save
        await SaveAsync();

        // Wait for the file contents to change, indicating VS Code has flushed to disk
        var currentContents = "";
        try
        {
            currentContents = await Helper.WaitForConditionAsync(
                async () =>
                {
                    try
                    {
                        return await ReadFileExclusiveAsync(filePath);
                    }
                    catch (IOException)
                    {
                        // File might be locked by VS Code, continue waiting
                        return originalContents;
                    }
                },
                contents => contents != originalContents,
                TimeSpan.FromSeconds(5),
                initialDelayMs: 50);

            TestServices.Logger.Log($"WaitForEditorTextChangeAsync: AFTER contents ({currentContents.Length} chars):\n{currentContents}");
            return currentContents;
        }
        catch (TimeoutException)
        {
            // Timeout: return the last read contents (file may not have changed)
            TestServices.Logger.Log($"WaitForEditorTextChangeAsync: Timeout waiting for contents change, returning current contents");
            try
            {
                currentContents = await ReadFileExclusiveAsync(filePath);
            }
            catch (IOException)
            {
                // Use original if we can't read
            }

            TestServices.Logger.Log($"WaitForEditorTextChangeAsync: AFTER contents (fallback, {currentContents.Length} chars):\n{currentContents}");
            return currentContents;
        }
    }

    /// <summary>
    /// Reads a file using ReadWrite access to ensure VS Code has finished writing.
    /// Opening with ReadWrite will fail if another process has the file open for writing.
    /// Retries for a few seconds if the file is locked.
    /// </summary>
    private static async Task<string> ReadFileExclusiveAsync(string filePath)
    {
        var result = await Helper.WaitForConditionAsync(
            async () =>
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    using var reader = new StreamReader(stream);
                    return (Content: await reader.ReadToEndAsync(), Success: true);
                }
                catch (IOException)
                {
                    return (Content: string.Empty, Success: false);
                }
            },
            result => result.Success,
            TimeSpan.FromSeconds(3),
            initialDelayMs: 50);

        return result.Content;
    }

    /// <summary>
    /// Waits for the VS Code quick input widget (command palette, go to line, etc.) to appear.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public async Task WaitForQuickInputAsync(int timeoutMs = 5000)
    {
        // Wait for the input field itself which is more reliable than the container
        await TestServices.Playwright.Page.Locator(".quick-input-widget .quick-input-box input")
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });
    }

    /// <summary>
    /// Waits for the VS Code quick input widget to close.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public async Task WaitForQuickInputToCloseAsync(int timeoutMs = 2000)
    {
        try
        {
            await TestServices.Playwright.Page.Locator(".quick-input-widget")
                .WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = timeoutMs
                });
        }
        catch (TimeoutException)
        {
            // Widget may already be hidden
        }
    }

    /// <summary>
    /// Gets the current file name from the tab.
    /// </summary>
    public async Task<string?> GetCurrentFileNameAsync()
    {
        var activeTabLocator = TestServices.Playwright.Page.Locator(".tab.active .monaco-icon-label-container");
        if (await activeTabLocator.CountAsync() == 0)
        {
            return null;
        }

        return await activeTabLocator.First.TextContentAsync();
    }

    /// <summary>
    /// Gets the current cursor position (line and column) from the VS Code status bar.
    /// </summary>
    /// <returns>A tuple of (line, column) or null if position cannot be determined.</returns>
    public async Task<(int Line, int Column)?> GetCursorPositionAsync()
    {
        // VS Code shows cursor position in the status bar as "Ln X, Col Y"
        // The element has class "editor-status-selection" or similar
        var statusText = await TestServices.Playwright.Page.EvaluateAsync<string?>(@"
            (() => {
                // Try multiple selectors for the cursor position in status bar
                const selectors = [
                    '.editor-status-selection',
                    '[aria-label*=""Go to Line""]',
                    '.statusbar-item a[aria-label*=""Ln""]',
                    '.statusbar-item:has-text(""Ln"")'
                ];
                
                for (const selector of selectors) {
                    const el = document.querySelector(selector);
                    if (el && el.textContent) {
                        return el.textContent;
                    }
                }
                
                // Fallback: search all status bar items
                const items = document.querySelectorAll('.statusbar-item');
                for (const item of items) {
                    const text = item.textContent || '';
                    if (text.includes('Ln') && text.includes('Col')) {
                        return text;
                    }
                }
                
                return null;
            })()
        ");

        if (string.IsNullOrEmpty(statusText))
        {
            return null;
        }

        // Parse "Ln X, Col Y" format
        // Example: "Ln 13, Col 17" or "Ln 13, Col 17 (5 selected)"
        var match = System.Text.RegularExpressions.Regex.Match(
            statusText,
            @"Ln\s*(\d+),\s*Col\s*(\d+)");

        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out var line) &&
            int.TryParse(match.Groups[2].Value, out var column))
        {
            return (line, column);
        }

        return null;
    }

    /// <summary>
    /// Moves the cursor to a specific line and column.
    /// </summary>
    public async Task GoToLineAsync(int line, int column = 1)
    {
        // Ctrl+G opens Go to Line dialog (Control on all platforms, including macOS)
        await TestServices.Input.PressWithControlAsync('g');
        await WaitForQuickInputAsync();

        await TestServices.Input.TypeAsync($"{line}:{column}");
        await TestServices.Input.PressAsync(SpecialKey.Enter);

        await WaitForQuickInputToCloseAsync();

        // Wait for the cursor position to actually update in the status bar.
        // The status bar update can lag behind the actual cursor movement.
        await Helper.WaitForConditionAsync(
            GetCursorPositionAsync,
            pos => pos?.Line == line,
            TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Selects all text in the editor.
    /// </summary>
    public async Task SelectAllAsync()
    {
        await TestServices.Input.PressWithPrimaryModifierAsync('a');
        // Selection is synchronous, minimal wait
        await Task.Delay(50);
    }

    /// <summary>
    /// Saves the current file and waits for the save to complete.
    /// </summary>
    public async Task SaveAsync()
    {
        TestServices.Logger.Log("Saving document.");
        await TestServices.Input.PressWithPrimaryModifierAsync('s');

        // Wait for the "dirty" indicator to disappear from the tab
        try
        {
            await WaitForEditorDirtyAsync(expectDirty: false);
        }
        catch (TimeoutException)
        {
            // File may not have been dirty, or indicator differs
        }
    }

    /// <summary>
    /// Waits for the editor dirty state to match the expected value.
    /// </summary>
    /// <param name="expectDirty">If true, waits for unsaved changes. If false, waits for clean state.</param>
    public async Task WaitForEditorDirtyAsync(bool expectDirty = true)
    {
        await Helper.WaitForConditionAsync(
            async () =>
            {
                var dirtyCount = await TestServices.Playwright.Page.Locator(".tab.active.dirty").CountAsync();
                var isDirty = dirtyCount > 0;
                TestServices.Logger.Log("Dirty indicator: " + (isDirty ? "present" : "not present"));
                return isDirty == expectDirty;
            },
            TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Opens the command palette and waits for it to appear.
    /// </summary>
    public async Task OpenCommandPaletteAsync()
    {
        await TestServices.Input.PressWithShiftPrimaryModifierAsync('p');
        await WaitForQuickInputAsync();
    }

    /// <summary>
    /// Executes a command via the command palette.
    /// </summary>
    public async Task ExecuteCommandAsync(string command)
    {
        await OpenCommandPaletteAsync();
        await TestServices.Input.TypeAsync(command);

        // Wait for the command to appear in the list
        await Helper.WaitForConditionAsync(
            async () =>
            {
                var itemCount = await TestServices.Playwright.Page.Locator(".quick-input-list .monaco-list-row").CountAsync();
                return itemCount > 0;
            },
            TimeSpan.FromSeconds(5));

        await TestServices.Input.PressAsync(SpecialKey.Enter);
        await WaitForQuickInputToCloseAsync();
    }

    /// <summary>
    /// Navigates to a specific word/symbol in the file and positions the cursor on it.
    /// Uses Find to locate the word, then closes the find dialog and optionally selects the word.
    /// </summary>
    /// <param name="word">The word to navigate to.</param>
    /// <param name="selectWord">If true, selects the found text after navigating.</param>
    public async Task GoToWordAsync(string word, bool selectWord = false)
    {
        // Use Find to navigate to the word
        await TestServices.Input.PressWithPrimaryModifierAsync('f');

        // Wait for the find widget to appear
        await TestServices.Playwright.Page.Locator(".editor-widget.find-widget")
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });

        await TestServices.Input.TypeAsync(word);

        await Task.Delay(100);

        // Close the find dialog - press Escape twice:
        // First Escape unfocuses the find input, second closes the widget
        await TestServices.Input.PressAsync(SpecialKey.Escape);
        await Task.Delay(50);
        await TestServices.Input.PressAsync(SpecialKey.Escape);

        // Wait for find widget to close
        try
        {
            await TestServices.Playwright.Page.Locator(".editor-widget.find-widget.visible")
                .WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = 2000
                });
        }
        catch (TimeoutException)
        {
            // Widget still visible - try one more escape and take screenshot for debugging
            await TestServices.Input.PressAsync(SpecialKey.Escape);
            await Task.Delay(100);
            await TestServices.Playwright.TakeScreenshotAsync($"GoToWord_{word}_StillVisible");
        }

        if (selectWord)
        {
            // Select the word by using Ctrl+D (Add Selection To Next Find Match)
            // which selects the word at cursor, or we can use Shift+Arrow keys
            // Use Ctrl+Shift+Left to select word to the left (cursor is at end of match)
            for (var i = 0; i < word.Length; i++)
            {
                await TestServices.Input.PressWithShiftAsync(SpecialKey.ArrowLeft);
            }
        }
        else
        {
            // GoToWord leaves cursor at end, move to start of word
            await TestServices.Input.PressAsync(SpecialKey.ArrowLeft);
        }
    }

    /// <summary>
    /// Opens a file in the editor and waits for it to be active.
    /// </summary>
    public async Task OpenFileAsync(string relativePath)
    {
        TestServices.Logger.Log($"Opening file: {relativePath}");

        // Use the Quick Open dialog (Ctrl+P / Cmd+P) to open the file
        await TestServices.Input.PressWithPrimaryModifierAsync('p');

        // Wait for Quick Open input to appear - wait for the input field itself which is more reliable
        await TestServices.Playwright.Page.Locator(".quick-input-widget .quick-input-box input")
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });

        await TestServices.Input.TypeAsync(relativePath);

        // Wait for the file list to populate by checking for list items
        await Helper.WaitForConditionAsync(
            async () =>
            {
                var listItemCount = await TestServices.Playwright.Page.Locator(".quick-input-list .monaco-list-row").CountAsync();
                return listItemCount > 0;
            },
            TimeSpan.FromSeconds(5));

        await TestServices.Input.PressAsync(SpecialKey.Enter);

        // Wait for the file to be open by checking the active tab
        var expectedFileName = Path.GetFileName(relativePath);
        await Helper.WaitForConditionAsync(
            async () =>
            {
                var activeTabLocator = TestServices.Playwright.Page.Locator(".tab.active .monaco-icon-label-container");
                if (await activeTabLocator.CountAsync() == 0)
                    return false;
                var tabText = await activeTabLocator.First.TextContentAsync();
                return tabText?.Contains(expectedFileName, StringComparison.OrdinalIgnoreCase) == true;
            },
            TestServices.Settings.LspTimeout);

        // Track the currently open file for GetEditorTextAsync
        _currentOpenFile = relativePath;

        TestServices.Logger.Log($"File opened: {relativePath}");
    }
}
