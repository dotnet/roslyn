// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

using Playwright = Playwright.Playwright;

/// <summary>
/// Manages the Playwright browser connection to VS Code via Chrome DevTools Protocol.
/// </summary>
public class PlaywrightService(IntegrationTestServices testServices) : ServiceBase(testServices)
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private int _screenshotCounter = 0;

    /// <summary>
    /// The Playwright page connected to VS Code.
    /// </summary>
    public IPage Page => _page ?? throw new InvalidOperationException("Playwright not connected. Call ConnectAsync first.");

    /// <summary>
    /// Takes a screenshot of the VS Code window and saves it to the screenshots directory.
    /// </summary>
    /// <param name="name">A descriptive name for the screenshot (will be sanitized for filename).</param>
    /// <returns>The path to the saved screenshot.</returns>
    public async Task<string> TakeScreenshotAsync(string name)
    {
        if (_page == null)
        {
            TestServices.Logger.Log("Cannot take screenshot - page not connected");
            return string.Empty;
        }

        var screenshotsDir = TestServices.Settings.ScreenshotsDir;
        if (string.IsNullOrEmpty(screenshotsDir))
        {
            TestServices.Logger.Log("Cannot take screenshot - ScreenshotsDir not configured");
            return string.Empty;
        }

        try
        {
            Directory.CreateDirectory(screenshotsDir);

            // Sanitize the name for use in a filename
            var sanitizedName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            var timestamp = DateTime.Now.ToString("HHmmss");
            var filename = $"{++_screenshotCounter:D3}_{timestamp}_{sanitizedName}.png";
            var filepath = Path.Combine(screenshotsDir, filename);

            await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = filepath,
                FullPage = true
            });

            TestServices.Logger.Log($"Screenshot saved: {filepath}");
            return filepath;
        }
        catch (Exception ex)
        {
            TestServices.Logger.Log($"Failed to take screenshot: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Initializes Playwright and ensures browsers are installed.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Ensure Chromium browser is installed (similar to VS Code auto-install pattern)
        TestServices.Logger.Log("Ensuring Playwright browsers are installed...");
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright browser installation failed with exit code {exitCode}");
        }

        _playwright = await Playwright.CreateAsync();
    }

    /// <summary>
    /// Connects to VS Code via Chrome DevTools Protocol.
    /// </summary>
    /// <param name="port">The remote debugging port.</param>
    /// <param name="workspaceName">The workspace folder name to find.</param>
    public async Task ConnectAsync(int port, string workspaceName)
    {
        var cdpUrl = $"http://localhost:{port}";
        TestServices.Logger.Log($"Connecting to VS Code via CDP: {cdpUrl}");

        var retries = 10; // Increased retries for Linux/CI
        var retryDelay = 3000; // 3 seconds between retries

        while (retries > 0)
        {
            try
            {
                TestServices.Logger.Log($"CDP connection attempt ({11 - retries}/10)...");

                // Add timeout to the CDP connection
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var connectTask = _playwright!.Chromium.ConnectOverCDPAsync(cdpUrl);
                var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("CDP connection timed out after 30 seconds");
                }

                _browser = await connectTask;
                TestServices.Logger.Log("CDP connection established, looking for workspace page...");

                // Find the page that has the workspace open (look for the workbench with our folder)
                _page = await FindWorkspacePageAsync(workspaceName);

                if (_page != null)
                {
                    _context = _page.Context;
                    TestServices.Logger.Log("Connected to VS Code workspace window successfully");
                    return;
                }

                // Fallback to first available page if we couldn't find the workspace
                _context = _browser.Contexts.FirstOrDefault() ?? await _browser.NewContextAsync();
                _page = _context.Pages.FirstOrDefault() ?? await _context.NewPageAsync();
                TestServices.Logger.Log("Connected to VS Code (fallback to first page)");
                return;
            }
            catch (Exception ex)
            {
                TestServices.Logger.Log($"Failed to connect (attempt {11 - retries}), retrying... ({ex.GetType().Name}: {ex.Message})");
                await Task.Delay(retryDelay);
                retries--;
            }
        }

        throw new InvalidOperationException("Failed to connect to VS Code via CDP after multiple retries");
    }

    private async Task<IPage?> FindWorkspacePageAsync(string workspaceName)
    {
        TestServices.Logger.Log($"Looking for workspace page with folder: {workspaceName}");

        var pagesWithWorkbench = new List<IPage>();
        foreach (var context in _browser!.Contexts)
        {
            foreach (var page in context.Pages)
            {
                try
                {
                    // Check if this page has the VS Code workbench
                    var workbenchLocator = page.Locator(".monaco-workbench");
                    if (await workbenchLocator.CountAsync() == 0)
                    {
                        continue;
                    }

                    pagesWithWorkbench.Add(page);

                    // Check if the title or explorer contains our workspace name
                    var title = await page.TitleAsync();
                    TestServices.Logger.Log($"Found VS Code page with title: {title}");

                    if (title.Contains(workspaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        TestServices.Logger.Log("Matched workspace by title");
                        return page;
                    }

                    // Also check for explorer view showing the folder (use First since multiple elements match)
                    var explorerTitleLocator = page.Locator(".explorer-folders-view .monaco-icon-label");
                    if (await explorerTitleLocator.CountAsync() > 0)
                    {
                        var explorerText = await explorerTitleLocator.First.TextContentAsync();
                        TestServices.Logger.Log($"Explorer shows: {explorerText}");
                        if (explorerText?.Contains(workspaceName, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            TestServices.Logger.Log("Matched workspace by explorer");
                            return page;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TestServices.Logger.Log($"Error checking page: {ex.Message}");
                }
            }
        }

        if (pagesWithWorkbench.Count == 1)
        {
            TestServices.Logger.Log("Only one VS Code page found, using it");
            return pagesWithWorkbench[0];
        }

        TestServices.Logger.Log($"Found {pagesWithWorkbench.Count} VS Code pages, could not determine correct one");
        return null;
    }

    /// <summary>
    /// Disposes the Playwright resources.
    /// </summary>
    public async Task DisposeAsync()
    {
        // VS Code may have already closed (crashed or exited early).
        // Wrap each close in try-catch to ensure we clean up as much as possible.
        if (_page != null)
        {
            try
            {
                await _page.CloseAsync();
            }
            catch (PlaywrightException)
            {
                // Page already closed or connection lost, ignore
            }

            _page = null;
        }

        if (_context != null)
        {
            try
            {
                await _context.CloseAsync();
            }
            catch (PlaywrightException)
            {
                // Context already closed or connection lost, ignore
            }

            _context = null;
        }

        if (_browser != null)
        {
            try
            {
                await _browser.CloseAsync();
            }
            catch (PlaywrightException)
            {
                // Browser already closed or connection lost, ignore
            }

            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
    }
}
