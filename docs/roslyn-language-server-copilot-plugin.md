# Roslyn Language Server - Copilot Plugin

The `roslyn-language-server` is a .NET tool that provides C# language intelligence (go-to-definition, find references, diagnostics, etc.) to AI coding agents via the [Language Server Protocol (LSP)](https://microsoft.github.io/language-server-protocol/). It is distributed as a plugin through the [dotnet/skills](https://github.com/dotnet/skills) marketplace, enabling automatic installation and use in tools like GitHub Copilot CLI.

## Quick Start

For this repository specifically, the equivalent Copilot CLI LSP configuration is already checked in at `.github/lsp.json`, and `.vscode/settings.json` points the server at `Roslyn.slnx`. That means opening this repo in Copilot CLI is enough to have the C# LSP configured automatically.

### Install the plugin

In your agent (Copilot CLI, etc.), add the marketplace and install the `dotnet` plugin:

```
/plugin marketplace add dotnet/skills
/plugin install dotnet@dotnet-agent-skills
```

Restart the agent to load the plugin. Once installed, the agent will automatically have access to C# language intelligence for `.cs` files through the LSP.

### What it provides

With the plugin active, the agent gains LSP-powered capabilities for C# code:

- **Go to Definition** — navigate to where a symbol is defined
- **Find References** — find all usages of a symbol
- **Hover** — get type information and documentation
- **Diagnostics** — see compiler errors and warnings
- **Document Symbols** — list all symbols in a file
- **And more** — any LSP feature supported by the Roslyn language server

## Prerequisites

- **.NET 10 SDK** must be installed and available on `PATH`.  Your project can still use an older, supported SDK.

## Automatic Project Loading

The language server automatically discovers and loads projects using the following strategy (evaluated in order):

### 1. VS Code Settings (`dotnet.defaultSolution`)

If a `.vscode/settings.json` file exists in the workspace folder, the server reads the `dotnet.defaultSolution` setting:

```jsonc
// .vscode/settings.json
{
  "dotnet.defaultSolution": "src/MyApp.sln"
}
```

- **Relative or absolute paths** to a `.sln` or `.slnx` file are supported.
- Set to `"disable"` to prevent the server from loading any solution or projects automatically:
  ```jsonc
  {
    "dotnet.defaultSolution": "disable"
  }
  ```

### 2. Single Solution File at the Root

If there is exactly **one** `.sln` or `.slnx` file at the root of the workspace folder, the server will automatically load that solution.

### 3. Individual Project Discovery

As a fallback, the server recursively discovers all `.csproj` files within the workspace folders and loads them individually.

## Troubleshooting

### Verify LSP configuration is found

- In Copilot CLI, running the `/lsp show` should show the server configured for C#:
```
  Plugin-configured servers:
    • csharp: (.cs) [from dotnet]
```

### Viewing LSP Logs

Copilot CLI writes LSP server logs to the `.copilot/logs/` directory in your home folder. To inspect the language server's output:

- **macOS / Linux:** `~/.copilot/logs/`
- **Windows:** `%USERPROFILE%\.copilot\logs\`

Look for log files related to `csharp` or `roslyn-language-server`. These contain the server's startup output, project loading progress, and any errors encountered. This is the first place to check when the language server isn't behaving as expected.

### The tool isn't being found

- Ensure `dotnet` is on your `PATH`.
- Check for a `nuget.config` (repo, user, or machine-level) that restricts package sources; `dnx` uses NuGet sources to resolve tools.
- Verify `nuget.org` (or an other feed that mirrors `roslyn-language-server`) is enabled, for example with `dotnet nuget list source`.
- Try installing the tool manually: `dotnet tool install -g roslyn-language-server --prerelease`.

### Project load issues

If the LSP logs show failures loading projects

- Verify that a compatible .NET SDK is installed by running `dotnet --version`.
- Check that your project builds successfully with `dotnet build` before using the language server.
- For large repositories with multiple solutions, configure `dotnet.defaultSolution` in `.vscode/settings.json` to specify which solution to load.

### Performance

- Loading a solution with many projects may take some time. The server reports progress to the client during loading.
- For large repositories, prefer loading a specific solution (via `dotnet.defaultSolution`) rather than relying on individual project discovery, which may load test projects and other projects you don't need.

## Related Links

- [dotnet/skills repository](https://github.com/dotnet/skills) — Plugin marketplace for .NET agent skills

## How It Works

The plugin is configured in the [dotnet/skills](https://github.com/dotnet/skills) repository via [`plugins/dotnet/lsp.json`](https://github.com/dotnet/skills/blob/main/plugins/dotnet/lsp.json):

When the agent opens a workspace containing `.cs` files, it will:

1. **Install and run** the [`roslyn-language-server`](https://www.nuget.org/packages/roslyn-language-server) .NET tool on-the-fly using `dotnet dnx` (which downloads and caches the tool automatically; `--yes` skips confirmation and `--prerelease` allows prerelease versions).
2. **Communicate over stdio** (`--stdio`) using the Language Server Protocol.
3. **Automatically discover and load projects** (`--autoLoadProjects`) so that the agent immediately has full semantic understanding of the codebase.

### Command-Line Options

| Option | Description |
|--------|-------------|
| `--stdio` | Use stdio for LSP communication (required for most agent integrations) |
| `--autoLoadProjects` | Automatically discover and load projects from workspace folders |
| `--logLevel <level>` | Minimum log verbosity (default: `Information`) |
| `--debug` | Launch the debugger on startup |
