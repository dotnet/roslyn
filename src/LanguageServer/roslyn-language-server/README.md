# roslyn-language-server

A [Language Server Protocol (LSP)](https://microsoft.github.io/language-server-protocol/) implementation for C# and Razor powered by Roslyn.

## Overview

The `roslyn-language-server` is a .NET tool that provides rich language features for C# and Razor through the Language Server Protocol. It powers editor integrations including the [C# extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) and C# Dev Kit.

This tool is a lightweight entry point that relays LSP traffic to the Roslyn language server. It bundles the `Microsoft.CodeAnalysis.LanguageServer` executable and launches it on demand.

This tool implements the LSP specification, enabling features such as:

- IntelliSense (code completion)
- Go to definition
- Find all references
- Code fixes and refactorings
- Diagnostics and errors
- Hover information
- Document formatting
- And more

## Installation

Install the language server as a .NET tool:

```bash
dotnet tool install --global roslyn-language-server --prerelease
```

## Usage

The language server is designed to be launched by editor clients and typically should not be run directly by end users. It communicates via standard input/output or named pipes.

### Command-line Options

All options are optional. One of `--stdio` or `--pipe` should typically be specified for communication. The thin client forwards all unrecognized options through to the underlying language server.

> **Note:** Command-line options are subject to change in future versions.

- `--stdio` - Use standard I/O for communication with the client (default: false)
- `--pipe <name>` - Use a named pipe for communication
- `--daemon-mode` - Allow connecting to (or starting) a shared, multi-client language server daemon instead of launching a dedicated language-server child process for that client.
- `--autoLoadProjects [maximum]` - Automatically discover and load projects based on workspace folders. See [Automatic project loading](#automatic-project-loading).
- `--logLevel <level>` - Set the minimum log verbosity: Trace, Debug, Information, Warning, Error, or None (default: Information)
- `--extensionLogDirectory <path>` - Directory for log files
- `--extension <path>` - Load extension assemblies (can be specified multiple times)
- `--debug` - Launch the debugger on startup (default: false)
- `--telemetryLevel <level>` - Set telemetry level: all, crash, error, or off (default: off)
- And other specialized options for advanced scenarios

### Daemon mode

By default, each editor session starts its own language server process, which exits when the editor disconnects.

With `--daemon-mode`, editor sessions instead share a single background language server process (one per user and tool version). The first session starts it, and later sessions connect to it. Each session still gets its own isolated workspace. This reduces overall memory usage when you run several editor sessions at once - each session can share common resources with each other. 

Because the process is shared, closing the editor or terminal that started it does not shut it down.  When the last session disconnects, the daemon keeps running for a short period so that later sessions can reuse it, and then it exits. You can set this period with `--daemonKeepAlive` or the `ROSLYN_LANGUAGE_SERVER_DAEMON_KEEPALIVE` environment variable.

### Automatic project loading

The server can find and load your solution or projects on its own when an editor connects, so language features work without the editor having to open a solution explicitly. It is off by default.

#### Enabling it

On the command line, pass `--autoLoadProjects`:

```bash
roslyn-language-server --stdio --autoLoadProjects [maximum]
```

`maximum` is optional and must be greater than zero. It limits how many projects are loaded when the server falls back to discovering individual projects (default: 500).

Alternatively, the editor can set it per session in the `initializationOptions` of the LSP `initialize` request:

```jsonc
{
  "initializationOptions": { "autoLoadProjects": 500 }
}
```

- `0` disables automatic loading for the session, even if `--autoLoadProjects` was passed on the command line.
- If the option is missing or `null`, the command-line setting is used.

This value only applies to the session that sent it, so in [daemon mode](#daemon-mode) each editor session can choose its own setting.

#### How the server chooses what to load

The server looks at the workspace folders sent in the `initialize` request and uses the first rule that applies:

1. **`dotnet.defaultSolution` setting.** If a workspace folder contains a `.vscode/settings.json` with a `dotnet.defaultSolution` setting, that solution is loaded. The path can be absolute or relative to the workspace folder. If the file doesn't exist, the setting is ignored.

   ```jsonc
   // .vscode/settings.json
   {
     "dotnet.defaultSolution": "src/MyApp.sln"
   }
   ```

   Set it to `"disable"` to turn off automatic loading. This only takes effect when a single workspace folder is open.

2. **A single solution at the root.** If one workspace folder is open and its root contains exactly one `.sln` or `.slnx` file, that solution is loaded.

3. **Project discovery.** Otherwise, every `.csproj` file in the workspace folders (including subdirectories) is loaded. If there are more than the maximum, projects under a `test` or `tests` directory are dropped first, and then the remaining list is cut down to the maximum.

Loading happens in the background. If the editor supports it, the server reports progress while loading.

For large repositories, set `dotnet.defaultSolution` so that only the projects you need are loaded.

### Example

```bash
roslyn-language-server --stdio --autoLoadProjects
```

## Requirements

- .NET 10.0 or later runtime

## More Information

- [Roslyn GitHub Repository](https://github.com/dotnet/roslyn)
- [Language Server Protocol Specification](https://microsoft.github.io/language-server-protocol/)

## License

This tool is part of the .NET Compiler Platform ("Roslyn") and is licensed under the [MIT license](https://github.com/dotnet/roslyn/blob/main/License.txt).
