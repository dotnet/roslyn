// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Provides services for VS Code integration tests.
/// This is the main entry point for test code to interact with VS Code.
/// </summary>
public class IntegrationTestServices
{
    public IntegrationTestServices(ITestOutputHelper output, TestSettings settings)
    {
        Settings = settings;
        Logger = new LoggerService(output);
        Playwright = new PlaywrightService(this);
        Workspace = new WorkspaceService(this);
        VSCode = new VSCodeService(this);
        Editor = new EditorService(this);
        Input = new InputService(this);
        CodeAction = new CodeActionService(this);
        Hover = new HoverServices(this);
        Completion = new CompletionServices(this);
        Diagnostics = new DiagnosticsServices(this);
        Navigation = new NavigationServices(this);
        Formatting = new FormattingServices(this);
        Razor = new RazorService(this);
    }

    /// <summary>
    /// Gets the test settings.
    /// </summary>
    public TestSettings Settings { get; }

    /// <summary>
    /// Gets the logger service for test output.
    /// </summary>
    public LoggerService Logger { get; }

    /// <summary>
    /// Gets the Playwright service for browser automation.
    /// </summary>
    public PlaywrightService Playwright { get; }

    /// <summary>
    /// Gets the workspace service for test workspace management.
    /// </summary>
    public WorkspaceService Workspace { get; }

    /// <summary>
    /// Gets the VS Code service for lifecycle and file operations.
    /// </summary>
    public VSCodeService VSCode { get; }

    /// <summary>
    /// Gets the VS Code editor helper for editor operations.
    /// </summary>
    public EditorService Editor { get; }

    /// <summary>
    /// Gets the input service for keyboard and mouse input.
    /// </summary>
    public InputService Input { get; }

    /// <summary>
    /// Gets the code action (Quick Fix, refactoring) service.
    /// </summary>
    public CodeActionService CodeAction { get; }

    /// <summary>
    /// Gets hover (Quick Info) services.
    /// </summary>
    public HoverServices Hover { get; }

    /// <summary>
    /// Gets completion (IntelliSense) services.
    /// </summary>
    public CompletionServices Completion { get; }

    /// <summary>
    /// Gets diagnostics (error squiggles) services.
    /// </summary>
    public DiagnosticsServices Diagnostics { get; }

    /// <summary>
    /// Gets navigation (Go to Definition, Find References) services.
    /// </summary>
    public NavigationServices Navigation { get; }

    /// <summary>
    /// Gets formatting services.
    /// </summary>
    public FormattingServices Formatting { get; }

    /// <summary>
    /// Gets Razor language server services.
    /// </summary>
    public RazorService Razor { get; }
}
