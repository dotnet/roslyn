// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Services for Razor language server operations in integration tests.
/// </summary>
public class RazorService(IntegrationTestServices testServices) : ServiceBase(testServices)
{
    /// <summary>
    /// Waits for the Razor language server to be fully initialized by verifying semantic tokenization is working.
    /// This checks that Razor component tags are being colorized (shows "razorComponentElement" in token inspector).
    /// </summary>
    public async Task WaitForReadyAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(60);
        var attempt = 0;

        TestServices.Logger.Log("Waiting for Razor language server to be ready (checking semantic tokens)...");

        await Helper.WaitForConditionAsync(
            async () =>
            {
                attempt++;
                TestServices.Logger.Log($"Razor ready check attempt {attempt}...");

                try
                {
                    // Open Home.razor which contains <PageTitle> component
                    await TestServices.Editor.OpenFileAsync("Components/Pages/Home.razor");

                    // Navigate to PageTitle - it's typically on line 3
                    // Home.razor usually has: @page "/" then <PageTitle>Home</PageTitle>
                    await TestServices.Editor.GoToWordAsync("PageTitle", selectWord: false);

                    // Run "Developer: Inspect Editor Tokens and Scopes" command
                    await TestServices.Editor.ExecuteCommandAsync("Developer: Inspect Editor Tokens and Scopes");

                    // Wait for the token inspector popup to appear and contain razorComponentElement
                    var hasRazorToken = true;
                    try
                    {
                        await Helper.WaitForConditionAsync(CheckForRazorTokenAsync, TimeSpan.FromSeconds(3));
                    }
                    catch (TimeoutException)
                    {
                        // Token not found within timeout
                        hasRazorToken = false;
                        TestServices.Logger.Log("Token inspector did not show razorComponentElement");
                    }

                    // Close the token inspector by pressing Escape
                    await TestServices.Input.PressAsync(SpecialKey.Escape);

                    // Close the file and wait for tab to close
                    await TestServices.Input.PressWithPrimaryModifierAsync('w');
                    await Helper.WaitForConditionAsync(
                        async () =>
                        {
                            var fileName = await TestServices.Editor.GetCurrentFileNameAsync();
                            return fileName == null || !fileName.Contains("Home.razor", StringComparison.OrdinalIgnoreCase);
                        },
                        TimeSpan.FromSeconds(2));

                    if (hasRazorToken)
                    {
                        TestServices.Logger.Log($"Razor language server is ready - semantic tokens verified (attempt {attempt})");
                        return true;
                    }

                    TestServices.Logger.Log($"Razor tokens not yet available (attempt {attempt}), retrying...");

                    // Wait before next attempt
                    await Task.Delay(1000);
                    return false;
                }
                catch (Exception ex)
                {
                    TestServices.Logger.Log($"Razor ready check attempt {attempt} failed: {ex.Message}");

                    // Try to close any open file/dialog before retrying
                    try
                    {
                        await TestServices.Input.PressAsync(SpecialKey.Escape);
                        await TestServices.Input.PressWithPrimaryModifierAsync('w');
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }

                    await Task.Delay(1000);
                    return false;
                }
            },
            timeout.Value,
            initialDelayMs: 0); // No initial delay - the loop body handles delays
    }

    /// <summary>
    /// Checks if the token inspector popup contains "razorComponentElement".
    /// </summary>
    private async Task<bool> CheckForRazorTokenAsync()
    {
        // The token inspector shows in a hover-like widget
        // Look for the content that contains token scope information
        var tokenContent = await TestServices.Playwright.Page.EvaluateAsync<string?>(@"
            (() => {
                // The token inspector typically uses a hover widget
                // Try multiple selectors to find it
                const selectors = [
                    '.monaco-hover',
                    '.monaco-hover-content', 
                    '.editor-hover-content',
                    '.hover-row',
                    '.hover-contents',
                    '[class*=""hover""]'
                ];
                
                for (const selector of selectors) {
                    const elements = document.querySelectorAll(selector);
                    for (const el of elements) {
                        const text = el.textContent || '';
                        // Look for razorComponentElement anywhere in the text
                        if (text.includes('razorComponentElement')) {
                            return text;
                        }
                    }
                }
                
                // Fallback: search all visible elements for razorComponentElement
                const allElements = document.querySelectorAll('*');
                for (const el of allElements) {
                    const text = el.textContent || '';
                    if (text.includes('razorComponentElement')) {
                        return text.substring(0, 1000);
                    }
                }
                
                return null;
            })()
        ");

        if (string.IsNullOrEmpty(tokenContent))
        {
            TestServices.Logger.Log("Token inspector content not found or no razorComponentElement");
            return false;
        }

        TestServices.Logger.Log($"Found razorComponentElement in token inspector");
        return true;
    }
}
