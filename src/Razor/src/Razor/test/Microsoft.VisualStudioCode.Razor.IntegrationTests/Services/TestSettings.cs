// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Configuration settings for E2E tests.
/// </summary>
public class TestSettings
{
    /// <summary>
    /// Path to the locally-built Razor extension DLLs.
    /// </summary>
    public string? RazorExtensionPath { get; set; }

    /// <summary>
    /// Timeout for waiting for VS Code to start and initialize.
    /// </summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Timeout for waiting for LSP operations to complete.
    /// </summary>
    public TimeSpan LspTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Overall timeout for test initialization (downloading VS Code, launching, connecting, etc.).
    /// If this is exceeded, the test will fail with a clear message about which step timed out.
    /// </summary>
    public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Port for VS Code's remote debugging protocol.
    /// </summary>
    public int RemoteDebuggingPort { get; set; } = 9222;

    /// <summary>
    /// Path to the local VS Code installation directory.
    /// VS Code will be downloaded here if not already present.
    /// </summary>
    public string? VSCodeInstallDir { get; set; }

    /// <summary>
    /// Path to the extensions directory for the isolated VS Code instance.
    /// </summary>
    public string? ExtensionsDir { get; set; }

    /// <summary>
    /// Path to the user data directory for the isolated VS Code instance.
    /// </summary>
    public string? UserDataDir { get; set; }

    /// <summary>
    /// Path to the screenshots directory for test debugging.
    /// </summary>
    public string? ScreenshotsDir { get; set; }

    /// <summary>
    /// Path to the directory where logs are collected on test failure.
    /// </summary>
    public string? FailureLogsDir { get; set; }

    /// <summary>
    /// The repository root path.
    /// </summary>
    public string? RepoRoot { get; set; }

    /// <summary>
    /// Creates settings with paths resolved relative to the repository root.
    /// </summary>
    public static TestSettings CreateDefault()
    {
        var settings = new TestSettings();

        // Try to find repository root by looking for Razor.slnx
        var currentDir = Directory.GetCurrentDirectory();
        var repoRoot = FindRepoRoot(currentDir);

        if (repoRoot != null)
        {
            settings.RepoRoot = repoRoot;

            settings.RazorExtensionPath = Path.Combine(
                repoRoot,
                "artifacts",
                "bin",
                "Microsoft.VisualStudioCode.RazorExtension",
                "Debug",
                "net10.0");

            // Use .vscode-test directory in repo for isolated VS Code
            var vscodeTestDir = Path.Combine(repoRoot, ".vscode-test");
            settings.VSCodeInstallDir = vscodeTestDir;
            settings.ExtensionsDir = Path.Combine(vscodeTestDir, "extensions");
            settings.UserDataDir = Path.Combine(vscodeTestDir, "user-data");
            settings.ScreenshotsDir = Path.Combine(vscodeTestDir, "screenshots");
            settings.FailureLogsDir = Path.Combine(vscodeTestDir, "failure-logs");
        }

        return settings;
    }

    private static string? FindRepoRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Razor.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
