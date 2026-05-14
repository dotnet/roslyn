// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Base class for VS Code integration tests.
/// Manages the VS Code lifecycle - each test gets its own VS Code instance for isolation.
/// </summary>
public abstract class VSCodeIntegrationTestBase : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;

    protected VSCodeIntegrationTestBase(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Integration test services providing access to VS Code, Editor, and test helpers.
    /// </summary>
    protected IntegrationTestServices TestServices { get; private set; } = null!;

    /// <summary>
    /// Runs a test action and automatically takes a screenshot and collects logs if it fails.
    /// </summary>
    /// <param name="testAction">The test action to run.</param>
    /// <param name="testName">The test name (auto-populated by compiler).</param>
    protected async Task ScreenshotOnFailureAsync(Func<Task> testAction, [CallerMemberName] string testName = "")
    {
        try
        {
            await testAction();
        }
        catch (Exception ex)
        {
            // Take a screenshot and collect logs on failure
            TestServices.Logger.Log($"Test '{testName}' failed: {ex.Message}");
            await TestServices.Playwright.TakeScreenshotAsync($"FAILED_{testName}");
            TestServices.VSCode.CollectLogsOnFailure(testName);
            throw;
        }
    }

    public virtual async Task InitializeAsync()
    {
        // Create settings and test services
        var settings = TestSettings.CreateDefault();
        TestServices = new IntegrationTestServices(_output, settings);

        // Log environment info for debugging CI issues
        TestServices.Logger.Log($"Environment: DISPLAY={Environment.GetEnvironmentVariable("DISPLAY") ?? "(not set)"}");
        TestServices.Logger.Log($"OS: {Environment.OSVersion}");
        TestServices.Logger.Log($"Initialization timeout: {settings.InitializationTimeout}");

        // Clear VS Code logs before each test to ensure clean logs
        TestServices.VSCode.ClearLogs();

        // Run initialization with an overall timeout to prevent indefinite hangs
        using var cts = new CancellationTokenSource(settings.InitializationTimeout);
        var currentStep = "unknown";

        try
        {
            // Step 1: Ensure VS Code is installed with extensions
            currentStep = "Step 1/7: Ensuring VS Code is installed";
            TestServices.Logger.Log($"{currentStep}...");
            await TestServices.VSCode.EnsureInstalledAsync().WaitAsync(cts.Token);
            TestServices.Logger.Log($"{currentStep} - Complete");

            // Step 2: Create a test workspace
            currentStep = "Step 2/7: Creating test workspace";
            TestServices.Logger.Log($"{currentStep}...");
            await TestServices.Workspace.CreateAsync().WaitAsync(cts.Token);
            TestServices.Logger.Log($"{currentStep} - Complete");

            // Step 3: Initialize Playwright
            currentStep = "Step 3/7: Initializing Playwright";
            TestServices.Logger.Log($"{currentStep}...");
            await TestServices.Playwright.InitializeAsync().WaitAsync(cts.Token);
            TestServices.Logger.Log($"{currentStep} - Complete");

            // Step 4: Launch VS Code with the workspace
            currentStep = "Step 4/7: Launching VS Code";
            TestServices.Logger.Log($"{currentStep}...");
            await TestServices.VSCode.LaunchAsync(TestServices.Workspace.WorkspacePath).WaitAsync(cts.Token);
            TestServices.Logger.Log($"{currentStep} - Complete");

            // Step 5: Connect Playwright to VS Code
            currentStep = "Step 5/7: Connecting Playwright to VS Code via CDP";
            TestServices.Logger.Log($"{currentStep}...");
            await TestServices.Playwright.ConnectAsync(
                TestServices.Settings.RemoteDebuggingPort,
                TestServices.Workspace.Name).WaitAsync(cts.Token);
            TestServices.Logger.Log($"{currentStep} - Complete");

            // Step 6: Wait for VS Code UI to be ready
            currentStep = "Step 6/7: Waiting for VS Code UI";
            TestServices.Logger.Log($"{currentStep}...");
            await TestServices.VSCode.WaitForReadyAsync().WaitAsync(cts.Token);
            TestServices.Logger.Log($"{currentStep} - Complete");

            // Step 7: Wait for LSP and Razor to be ready
            currentStep = "Step 7/7: Waiting for LSP and Razor";
            TestServices.Logger.Log($"{currentStep}...");
            await TestServices.VSCode.WaitForLspReadyAsync().WaitAsync(cts.Token);
            await TestServices.Razor.WaitForReadyAsync().WaitAsync(cts.Token);
            TestServices.Logger.Log($"{currentStep} - Complete");

            TestServices.Logger.Log("Test initialization completed successfully");
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            var message = $"Test initialization timed out after {settings.InitializationTimeout} during: {currentStep}";
            TestServices.Logger.Log($"FATAL: {message}");
            var stepName = currentStep.Replace("/", "_").Replace(":", "").Replace(" ", "_");
            await TestServices.Playwright.TakeScreenshotAsync($"INIT_TIMEOUT_{stepName}");
            TestServices.VSCode.CollectLogsOnFailure($"INIT_TIMEOUT_{stepName}");
            throw new TimeoutException(message);
        }
        catch (Exception ex)
        {
            TestServices.Logger.Log($"FATAL: Test initialization failed during {currentStep}: {ex.Message}");
            var stepName = currentStep.Replace("/", "_").Replace(":", "").Replace(" ", "_");
            await TestServices.Playwright.TakeScreenshotAsync($"INIT_FAILED_{stepName}");
            TestServices.VSCode.CollectLogsOnFailure($"INIT_FAILED_{stepName}");
            throw;
        }
    }

    public virtual async Task DisposeAsync()
    {
        await TestServices.Playwright.DisposeAsync();
        TestServices.VSCode.Stop();
        TestServices.Workspace.Cleanup();
    }
}
