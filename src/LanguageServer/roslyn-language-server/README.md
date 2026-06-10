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
- `--daemon-mode` - Allow connecting to (or starting) a shared, multi-client language server daemon instead of hosting a server in-process.
- `--autoLoadProjects` - Automatically discover and load projects based on workspace folders (default: false)
- `--logLevel <level>` - Set the minimum log verbosity: Trace, Debug, Information, Warning, Error, or None (default: Information)
- `--extensionLogDirectory <path>` - Directory for log files
- `--extension <path>` - Load extension assemblies (can be specified multiple times)
- `--debug` - Launch the debugger on startup (default: false)
- `--telemetryLevel <level>` - Set telemetry level: all, crash, error, or off (default: off)
- And other specialized options for advanced scenarios

### Daemon mode

When daemon mode is enabled, the thin client discovers a running language server daemon (scoped to the current user and tool version) and connects to it, starting one if necessary. A single daemon can serve multiple clients, each with its own isolated language server instance. When the last client disconnects, the daemon stays alive for a configurable keepalive period (see `--daemonKeepAlive` / `ROSLYN_LANGUAGE_SERVER_DAEMON_KEEPALIVE`) before exiting.

Because the daemon is shared and outlives any single client, it must not be torn down when an editor kills the launching client's process tree. The thin client therefore does not launch the daemon directly. Instead it launches a short-lived *bootstrap* process, which re-launches the real daemon and then exits, orphaning the daemon so it is no longer a descendant of the thin client. This is what keeps the daemon alive when the launching client's tree is torn down, on every platform — process-tree teardowns follow parent/child links, which neither Windows job-object breakaway nor Unix `setsid` change. On Unix the daemon additionally moves itself into a new session (`setsid`) so signals aimed at the launching client's session/process group (such as terminal-close `SIGHUP`) don't reach it. Its lifetime is then governed by the keepalive logic above rather than by which client happened to launch it.

When daemon mode is disabled (the default), the thin client launches the language server as a dedicated child process on the same transport the editor requested. With a named pipe, the server connects to the editor's pipe directly (the thin client stays out of the message path); with stdio, the thin client relays messages between its own stdio and the server. In both cases the server's standard output and error are forwarded to the thin client's, so server diagnostics reach the host even when LSP isn't available.

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
