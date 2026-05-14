// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Playwright;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Manages the VS Code lifecycle for E2E tests.
/// Downloads VS Code, installs extensions, launches the process, and waits for it to be ready.
/// </summary>
public partial class VSCodeService(IntegrationTestServices testServices)
{
    private readonly Installer _installer = new(testServices);
    private System.Diagnostics.Process? _vsCodeProcess;
    private string? _vsCodeExecutablePath;

    /// <summary>
    /// Ensures VS Code is installed and extensions are ready.
    /// </summary>
    public async Task EnsureInstalledAsync()
    {
        await EnsureVSCodeInstalledAsync();
        await EnsureExtensionsInstalledAsync();
    }

    /// <summary>
    /// Launches VS Code with the specified workspace.
    /// </summary>
    public async Task LaunchAsync(string workspacePath)
    {
        ConfigureWorkspaceSettings(workspacePath);
        await LaunchVSCodeAsync(workspacePath);
    }

    /// <summary>
    /// Waits for VS Code UI to be ready.
    /// </summary>
    public async Task WaitForReadyAsync()
    {
        await WaitForVSCodeReadyAsync();
    }

    private async Task EnsureVSCodeInstalledAsync()
    {
        var installDir = testServices.Settings.VSCodeInstallDir ?? throw new InvalidOperationException("VSCodeInstallDir not configured");

        Directory.CreateDirectory(installDir);

        _vsCodeExecutablePath = await _installer.EnsureVSCodeInstalledAsync(installDir);
    }

    private async Task EnsureExtensionsInstalledAsync()
    {
        var extensionsDir = testServices.Settings.ExtensionsDir
            ?? throw new InvalidOperationException("ExtensionsDir not configured");

        Directory.CreateDirectory(extensionsDir);

        // Check if C# extension is already installed
        if (!await _installer.IsExtensionInstalledAsync(
            _vsCodeExecutablePath!,
            "ms-dotnettools.csharp",
            extensionsDir))
        {
            await _installer.InstallCSharpExtensionAsync(_vsCodeExecutablePath!, extensionsDir);
        }
        else
        {
            testServices.Logger.Log("C# extension already installed");
        }
    }

    private async Task LaunchVSCodeAsync(string workspacePath)
    {
        // Verify workspace exists
        if (!Directory.Exists(workspacePath))
        {
            throw new InvalidOperationException($"Workspace directory does not exist: {workspacePath}");
        }

        testServices.Logger.Log($"Workspace directory verified: {workspacePath}");
        testServices.Logger.Log($"Workspace contents: {string.Join(", ", Directory.GetFileSystemEntries(workspacePath).Select(Path.GetFileName))}");

        // Use the CLI (code.cmd) instead of the Electron executable for proper folder opening
        var cliPath = Installer.GetCliPathForExecutable(_vsCodeExecutablePath!);

        if (!File.Exists(cliPath))
        {
            throw new InvalidOperationException($"VS Code CLI not found: {cliPath}");
        }

        var args = BuildVSCodeArgs(workspacePath);
        var processArgs = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));

        testServices.Logger.Log($"Launching VS Code: {cliPath} {processArgs}");

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = processArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.Start();

        // Poll for the CDP endpoint to become available instead of fixed delay
        testServices.Logger.Log("Waiting for VS Code debugging port to open...");
        var cdpUrl = $"http://127.0.0.1:{testServices.Settings.RemoteDebuggingPort}";
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        await Helper.WaitForConditionAsync(
            async () =>
            {
                // Check if process exited with an error
                if (process.HasExited && process.ExitCode != 0)
                {
                    var stdout = await process.StandardOutput.ReadToEndAsync();
                    var stderr = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException(
                        $"VS Code CLI exited with error code {process.ExitCode}. " +
                        $"stdout: {stdout}, stderr: {stderr}");
                }

                try
                {
                    // Check if the CDP endpoint is responding
                    var response = await httpClient.GetAsync($"{cdpUrl}/json/version");
                    return response.IsSuccessStatusCode;
                }
                catch (Exception)
                {
                    // Not ready yet, continue polling
                    return false;
                }
            },
            TimeSpan.FromSeconds(30),
            initialDelayMs: 500);

        testServices.Logger.Log("VS Code debugging port is ready");
        _vsCodeProcess = process;

        // Check if process exited with an error
        // Note: On Windows, code.cmd is a wrapper that spawns Electron and exits immediately with code 0.
        // This is expected behavior - we only error if exit code is non-zero.
        if (process.HasExited)
        {
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            testServices.Logger.Log($"VS Code CLI exited with code {process.ExitCode}");
            testServices.Logger.Log($"stdout: {stdout}");
            testServices.Logger.Log($"stderr: {stderr}");

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"VS Code CLI exited with error code {process.ExitCode}. " +
                    $"stdout: {stdout}, stderr: {stderr}");
            }

            // Exit code 0 is fine - the CLI spawned VS Code successfully
            testServices.Logger.Log("VS Code CLI exited successfully after spawning the application");
        }
    }

    /// <summary>
    /// Stops the VS Code process.
    /// </summary>
    public void Stop()
    {
        if (_vsCodeProcess != null && !_vsCodeProcess.HasExited)
        {
            try
            {
                testServices.Logger.Log("Stopping VS Code process...");
                _vsCodeProcess.Kill(entireProcessTree: true);
                _vsCodeProcess.Dispose();
                testServices.Logger.Log("VS Code process stopped.");
            }
            catch
            {
                // Ignore cleanup errors
            }

            _vsCodeProcess = null;
        }
    }

    /// <summary>
    /// Clears the VS Code logs directory to ensure clean logs for each test.
    /// </summary>
    public void ClearLogs()
    {
        var logsDir = GetVSCodeLogsDirectory();
        if (logsDir != null && Directory.Exists(logsDir))
        {
            try
            {
                testServices.Logger.Log($"Clearing VS Code logs directory: {logsDir}");
                Directory.Delete(logsDir, recursive: true);
                testServices.Logger.Log("VS Code logs cleared.");
            }
            catch (Exception ex)
            {
                testServices.Logger.Log($"Warning: Failed to clear logs directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Collects VS Code extension logs to a test-specific folder for debugging failures.
    /// </summary>
    /// <param name="testName">The name of the test (used for the folder name).</param>
    public void CollectLogsOnFailure(string testName)
    {
        var failureLogsDir = testServices.Settings.FailureLogsDir;
        if (string.IsNullOrEmpty(failureLogsDir))
        {
            testServices.Logger.Log("Cannot collect logs - FailureLogsDir not configured");
            return;
        }

        var logsDir = GetVSCodeLogsDirectory();
        if (logsDir == null || !Directory.Exists(logsDir))
        {
            testServices.Logger.Log($"No VS Code logs found at: {logsDir ?? "(null)"}");
            return;
        }

        try
        {
            // Sanitize the test name for use in a folder name
            var sanitizedName = string.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
            var timestamp = DateTime.Now.ToString("HHmmss");
            var testLogsDir = Path.Combine(failureLogsDir, $"FAILED_{timestamp}_{sanitizedName}");

            testServices.Logger.Log($"Collecting logs for failed test '{testName}' to: {testLogsDir}");

            CopyDirectory(logsDir, testLogsDir);

            // Log what we collected
            var files = Directory.GetFiles(testLogsDir, "*", SearchOption.AllDirectories);
            testServices.Logger.Log($"Collected {files.Length} log files for test '{testName}'");

            // Specifically look for C# extension logs
            foreach (var file in files)
            {
                if (file.Contains("ms-dotnettools.csharp", StringComparison.OrdinalIgnoreCase))
                {
                    testServices.Logger.Log($"  C# Extension log: {Path.GetRelativePath(testLogsDir, file)}");
                }
            }
        }
        catch (Exception ex)
        {
            testServices.Logger.Log($"Warning: Failed to collect logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the VS Code logs directory path.
    /// </summary>
    private string? GetVSCodeLogsDirectory()
    {
        if (string.IsNullOrEmpty(testServices.Settings.UserDataDir))
        {
            return null;
        }

        return Path.Combine(testServices.Settings.UserDataDir, "logs");
    }

    /// <summary>
    /// Recursively copies a directory.
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    private void ConfigureWorkspaceSettings(string workspacePath)
    {
        // Configure user-level settings to prevent session restore and unwanted windows
        ConfigureUserSettings();

        if (string.IsNullOrEmpty(testServices.Settings.RazorExtensionPath))
        {
            return; // Use bundled extension
        }

        var vscodeDir = Path.Combine(workspacePath, ".vscode");
        Directory.CreateDirectory(vscodeDir);

        var settingsPath = Path.Combine(vscodeDir, "settings.json");
        var settings = new Dictionary<string, object>();

        if (File.Exists(settingsPath))
        {
            var existing = File.ReadAllText(settingsPath);
            settings = JsonSerializer.Deserialize<Dictionary<string, object>>(existing) ?? [];
        }

        // Add the Razor extension path
        settings["dotnet.server.componentPaths"] = new Dictionary<string, string>
        {
            ["razorExtension"] = testServices.Settings.RazorExtensionPath
        };

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json);
    }

    private void ConfigureUserSettings()
    {
        if (string.IsNullOrEmpty(testServices.Settings.UserDataDir))
        {
            return;
        }

        // Clear any cached window state to prevent window restoration
        ClearWindowState();

        // Create the User settings directory
        var userSettingsDir = Path.Combine(testServices.Settings.UserDataDir, "User");
        Directory.CreateDirectory(userSettingsDir);

        var settingsPath = Path.Combine(userSettingsDir, "settings.json");
        var settings = new Dictionary<string, object>
        {
            // Disable session restore - this prevents VS Code from opening previous windows
            ["window.restoreWindows"] = "none",
            // Don't reopen folders
            ["window.reopenFolders"] = "none",
            // Open files in the same window
            ["window.openFilesInNewWindow"] = "off",
            // Open folders in the same window
            ["window.openFoldersInNewWindow"] = "off",
            // Don't open untitled editors
            ["workbench.startupEditor"] = "none",
            // Don't restore editors from previous session
            ["workbench.editor.restoreViewState"] = false,
            // Disable telemetry
            ["telemetry.telemetryLevel"] = "off",
            // Disable update checks
            ["update.mode"] = "none",
            // Disable extension recommendations
            ["extensions.ignoreRecommendations"] = true,
            // Go to Definition: go directly instead of peeking when there's a single result
            ["editor.gotoLocation.multipleDefinitions"] = "goto",
            ["editor.gotoLocation.multipleTypeDefinitions"] = "goto",
            ["editor.gotoLocation.multipleDeclarations"] = "goto",
            ["editor.gotoLocation.multipleImplementations"] = "goto",
            // Find All References: use peek view so tests can count references
            ["editor.gotoLocation.multipleReferences"] = "peek",
        };

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json);

        testServices.Logger.Log($"Configured user settings at: {settingsPath}");
    }

    private void ClearWindowState()
    {
        if (string.IsNullOrEmpty(testServices.Settings.UserDataDir))
        {
            return;
        }

        // Clear the storage directory which contains window state
        var storagePath = Path.Combine(testServices.Settings.UserDataDir, "User", "globalStorage");
        if (Directory.Exists(storagePath))
        {
            try
            {
                Directory.Delete(storagePath, recursive: true);
                testServices.Logger.Log("Cleared global storage");
            }
            catch (Exception ex)
            {
                testServices.Logger.Log($"Could not clear global storage: {ex.Message}");
            }
        }

        // Also clear the workspaceStorage
        var workspaceStoragePath = Path.Combine(testServices.Settings.UserDataDir, "User", "workspaceStorage");
        if (Directory.Exists(workspaceStoragePath))
        {
            try
            {
                Directory.Delete(workspaceStoragePath, recursive: true);
                testServices.Logger.Log("Cleared workspace storage");
            }
            catch (Exception ex)
            {
                testServices.Logger.Log($"Could not clear workspace storage: {ex.Message}");
            }
        }

        // Clear the backup directory (can contain old window states)
        var backupPath = Path.Combine(testServices.Settings.UserDataDir, "Backups");
        if (Directory.Exists(backupPath))
        {
            try
            {
                Directory.Delete(backupPath, recursive: true);
                testServices.Logger.Log("Cleared backups");
            }
            catch (Exception ex)
            {
                testServices.Logger.Log($"Could not clear backups: {ex.Message}");
            }
        }
    }

    private string[] BuildVSCodeArgs(string workspacePath)
    {
        var args = new List<string>
        {
            workspacePath,
            $"--remote-debugging-port={testServices.Settings.RemoteDebuggingPort}",
            "--disable-gpu",
            "--no-sandbox",
            "--skip-welcome",
            "--skip-release-notes",
            "--disable-workspace-trust",
            "--new-window",
            // Enable trace-level logging for extensions (C# and Razor) for CI debugging
            "--log", "trace",
        };

        // Use isolated user data and extensions directories to prevent interference
        // with any existing VS Code instances
        if (!string.IsNullOrEmpty(testServices.Settings.UserDataDir))
        {
            Directory.CreateDirectory(testServices.Settings.UserDataDir);
            args.Add($"--user-data-dir={testServices.Settings.UserDataDir}");
        }

        if (!string.IsNullOrEmpty(testServices.Settings.ExtensionsDir))
        {
            args.Add($"--extensions-dir={testServices.Settings.ExtensionsDir}");
        }

        return [.. args];
    }

    private async Task WaitForVSCodeReadyAsync()
    {
        // Wait for the VS Code window to be visible and the workbench to load
        var timeout = testServices.Settings.StartupTimeout;

        testServices.Logger.Log("Waiting for VS Code workbench to load...");

        // Wait for the main VS Code container
        await testServices.Playwright.Page.Locator(".monaco-workbench")
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = (float)timeout.TotalMilliseconds
            });

        // Give extensions a moment to initialize
        testServices.Logger.Log("Workbench loaded, waiting for extensions...");

        // Wait for the status bar to be visible as a sign of full initialization
        try
        {
            await testServices.Playwright.Page.Locator(".statusbar")
                .WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 10000
                });
        }
        catch (TimeoutException)
        {
            // Status bar should be there, but continue anyway
            testServices.Logger.Log("Warning: Status bar not found, continuing...");
        }
    }

    /// <summary>
    /// Waits for the C# extension and Razor to be ready.
    /// Uses multiple indicators to detect LSP readiness.
    /// </summary>
    public async Task WaitForLspReadyAsync()
    {
        var timeout = testServices.Settings.LspTimeout;
        testServices.Logger.Log("Waiting for C# LSP to be ready...");

        // Strategy 1: Look for C# status bar item (use CountAsync to avoid strict mode issues)
        var csharpReady = false;
        try
        {
            await Helper.WaitForConditionAsync(
                async () =>
                {
                    var count = await testServices.Playwright.Page.Locator("[aria-label*='C#']").CountAsync();
                    return count > 0;
                },
                TimeSpan.FromSeconds(timeout.TotalSeconds / 2));
            testServices.Logger.Log("C# status bar item found");
            csharpReady = true;
        }
        catch (TimeoutException)
        {
            testServices.Logger.Log("C# status bar item not found, trying alternative detection...");
        }

        // Strategy 2: Check for language mode indicator in status bar
        if (!csharpReady)
        {
            try
            {
                // Look for language mode indicator showing C# or Razor
                await Helper.WaitForConditionAsync(
                    async () =>
                    {
                        var languageModeLocator = testServices.Playwright.Page.Locator("[aria-label*='Select Language Mode']");
                        var count = await languageModeLocator.CountAsync();
                        if (count == 0)
                            return false;
                        var text = await languageModeLocator.First.TextContentAsync();
                        return text?.Contains("C#", StringComparison.OrdinalIgnoreCase) == true ||
                               text?.Contains("Razor", StringComparison.OrdinalIgnoreCase) == true ||
                               text?.Contains("ASP.NET", StringComparison.OrdinalIgnoreCase) == true;
                    },
                    TimeSpan.FromSeconds(timeout.TotalSeconds / 2));
                testServices.Logger.Log("Language mode indicator found");
                csharpReady = true;
            }
            catch (TimeoutException)
            {
                testServices.Logger.Log("Language mode indicator not found");
            }
        }

        // Strategy 3: Look for any loading indicators to disappear
        if (!csharpReady)
        {
            try
            {
                // Wait for any progress/loading indicators to disappear
                await Helper.WaitForConditionAsync(
                    async () =>
                    {
                        var loadingCount = await testServices.Playwright.Page.Locator(".progress-bit").CountAsync();
                        return loadingCount == 0;
                    },
                    TimeSpan.FromSeconds(10));
                testServices.Logger.Log("No loading indicators present");
            }
            catch (TimeoutException)
            {
                // Continue anyway
            }
        }

        testServices.Logger.Log("C# LSP ready check complete");
    }
}
