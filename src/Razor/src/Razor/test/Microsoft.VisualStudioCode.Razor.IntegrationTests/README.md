# VS Code Razor E2E Tests

End-to-end tests for the Razor language experience in VS Code, using Playwright to automate VS Code via the Chrome DevTools Protocol (CDP).

## How It Works

1. **VS Code Installation**: Tests automatically download and install VS Code (stable) to a local directory if not already present
2. **Extension Installation**: The C# extension is installed into an isolated extensions directory
3. **Test Workspace**: Each test run creates a fresh Blazor project using `dotnet new blazor`
4. **Playwright Connection**: VS Code is launched with `--remote-debugging-port=9222` and Playwright connects via CDP
5. **UI Automation**: Tests interact with VS Code through keyboard/mouse simulation and DOM queries

## Directory Structure

All test artifacts are stored in the `.vscode-test` directory at the repository root:

```
<repo-root>/
└── .vscode-test/
    ├── vscode-win32-x64-*/     # VS Code installation (platform-specific)
    ├── extensions/              # Isolated extensions directory (C# extension)
    └── user-data/               # Isolated user data directory (settings, state)
```

Test workspaces are created in the system temp directory:
```
%TEMP%/vscode-razor-test-<guid>/    # Fresh Blazor project for each test run
```

## Running Tests

From Visual Studio:
- Open Test Explorer and run tests from the `Microsoft.VisualStudioCode.Razor.IntegrationTests` project

From command line:
```powershell
cd src\Razor\test\Microsoft.VisualStudioCode.Razor.IntegrationTests
dotnet test
```

> **Note**: Tests run with `parallelizeTestCollections: false` to ensure only one VS Code instance runs at a time.

## Clean Run / Fresh Install

To reset everything and start fresh, delete the `.vscode-test` directory:

```powershell
# From repository root
Remove-Item -Recurse -Force .\.vscode-test
```

This will:
- Remove the VS Code installation (will be re-downloaded on next run)
- Remove installed extensions (will be re-installed on next run)
- Remove all cached user data and window state

To keep VS Code but reset just the state/settings:

```powershell
# Keep VS Code and extensions, just reset state
Remove-Item -Recurse -Force .\.vscode-test\user-data
```

## Troubleshooting

### Multiple VS Code windows open
The tests configure VS Code to prevent window restoration, but if you see multiple windows:
1. Close all VS Code instances
2. Delete the user-data directory: `Remove-Item -Recurse -Force .\.vscode-test\user-data`
3. Run the test again

### Tests timeout waiting for LSP
The C# extension and Razor language server need time to initialize. If tests consistently timeout:
1. Check that the C# extension installed correctly in `.vscode-test\extensions`
2. Try a clean run by deleting `.vscode-test` entirely
3. Ensure no other VS Code instances are using port 9222

### Playwright can't connect
If Playwright fails to connect via CDP:
1. Ensure no other process is using port 9222
2. Check that VS Code actually launched (look for the process)
3. The test waits up to 10 seconds for VS Code to start - increase if needed

## Architecture

- **VSCodeService**: xUnit fixture that manages VS Code lifecycle (download, launch, connect, cleanup)
- **EditorService**: Helper methods for editor operations (typing, navigation, completions, hover, etc.)
- **IntegrationTestServices**: Razor-specific helpers for verifying IntelliSense, diagnostics, etc.
- **TestSettings**: Configuration for paths, timeouts, and options

Tests use smart polling with exponential backoff instead of fixed delays, making them more reliable and faster.
