---
name: run-vscode-local-testing
description: Build the local Roslyn language server and Razor VS Code extension outputs, write a VS Code workspace that points at them, and launch VS Code for manual testing.
argument-hint: Ask which project folder VS Code should open for local testing.
---

# Run VS Code for Local Testing

Use this skill when:
- manually testing Roslyn or Razor changes in VS Code against locally built bits
- asked to launch VS Code with `dotnet.server.path` pointing at the repo's `Microsoft.CodeAnalysis.LanguageServer.dll`
- asked to point the C# extension's Razor component at the locally built `Microsoft.VisualStudioCode.RazorExtension` output

This workflow is currently **Windows-only**. It assumes:
- `code` is available on `PATH`
- the target project folder already exists on disk

## First step

If the user has not already given you a project folder, ask them which folder VS Code should open and wait for an absolute path before running the helper script.

## Goal

1. Build the local Roslyn language server and Razor VS Code extension outputs.
2. Write a `.code-workspace` file under `artifacts\`.
3. Point that workspace at the requested project folder.
4. Set:
   - `dotnet.server.path`
   - `dotnet.server.componentPaths.razorExtension`
5. Launch VS Code with that workspace.

## Helper script

Use the helper script in this skill:

```powershell
.\.github\skills\run-vscode-local-testing\scripts\Start-LocalVsCode.ps1 `
  -ProjectPath C:\path\to\project
```

### Parameters

- `-ProjectPath` (required): absolute path to the folder VS Code should open.
- `-Configuration`: build configuration. Defaults to `Debug`.
- `-WorkspacePath`: optional path for the generated workspace file. Defaults to `artifacts\<folder-name>-local-test.code-workspace`.
- `-SkipBuild`: skip `dotnet build` if the outputs are already up to date.
- `-NoLaunch`: write the workspace file without launching VS Code.
- `-NewWindow`: open VS Code in a new window instead of reusing the current one.

## What the script builds

Unless `-SkipBuild` is passed, the script builds:

```powershell
dotnet build src\LanguageServer\Microsoft.CodeAnalysis.LanguageServer\Microsoft.CodeAnalysis.LanguageServer.csproj -c Debug
dotnet build src\Razor\src\Razor\src\Microsoft.VisualStudioCode.RazorExtension\Microsoft.VisualStudioCode.RazorExtension.csproj -c Debug
```

The generated workspace points at:

```text
artifacts\bin\Microsoft.CodeAnalysis.LanguageServer\<Configuration>\net10.0\Microsoft.CodeAnalysis.LanguageServer.dll
artifacts\bin\Microsoft.VisualStudioCode.RazorExtension\<Configuration>\net10.0
```

The Razor extension project has a local-only post-build target that copies its companion assemblies into that output folder for manual testing. That target is gated off in CI.

## Generated workspace shape

The script writes a workspace like this:

```json
{
  "folders": [
    {
      "path": "C:\\path\\to\\project"
    }
  ],
  "settings": {
    "dotnet.server.path": "D:\\repo\\artifacts\\bin\\Microsoft.CodeAnalysis.LanguageServer\\Debug\\net10.0\\Microsoft.CodeAnalysis.LanguageServer.dll",
    "dotnet.server.componentPaths": {
      "razorExtension": "D:\\repo\\artifacts\\bin\\Microsoft.VisualStudioCode.RazorExtension\\Debug\\net10.0"
    }
  }
}
```

## Troubleshooting

If VS Code starts but Razor does not behave correctly, inspect the latest C# LSP trace log:

```text
C:\Users\<user>\AppData\Roaming\Code\logs\<latest>\window*\exthost\ms-dotnettools.csharp\C# LSP Trace Logs.log
```

Useful checks:
- Success markers:
  - `Razor extension startup finished.`
  - `textDocument/selectionRange`
  - other `razor/log` activity for the opened `.razor` file
- Failure markers:
  - `FileNotFoundException`
  - `Could not load file or assembly`
  - `No method by the name 'razor/documentClosed' is found`

## Example follow-up

If the user asks to re-open the same local-test workspace after rebuilding, rerun:

```powershell
.\.github\skills\run-vscode-local-testing\scripts\Start-LocalVsCode.ps1 `
  -ProjectPath C:\path\to\project `
  -SkipBuild
```
