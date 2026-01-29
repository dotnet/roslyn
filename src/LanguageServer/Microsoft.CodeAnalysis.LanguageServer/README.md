# roslyn-language-server

A [Language Server Protocol (LSP)](https://microsoft.github.io/language-server-protocol/) implementation for C# and Visual Basic powered by Roslyn.

## Overview

The `roslyn-language-server` is a .NET global tool that provides rich language features for C# and Visual Basic through the Language Server Protocol. It powers editor integrations including the [C# extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) and C# Dev Kit.

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

Install the language server as a .NET global tool:

```bash
dotnet tool install --global roslyn-language-server
```

## Usage

The language server is designed to be launched by editor clients and typically should not be run directly by end users. It communicates via standard input/output or named pipes.

### Command-line Options

- `--stdio` - Use standard I/O for communication with the client
- `--pipe <name>` - Use a named pipe for communication
- `--logLevel <level>` - Set the minimum log verbosity
- `--extensionLogDirectory <path>` - Directory for log files
- `--extension <path>` - Load extension assemblies (can be specified multiple times)
- `--debug` - Launch the debugger on startup
- `--telemetryLevel <level>` - Set telemetry level (off, crash, error, or all)
- `--autoLoadProjects` - Automatically discover and load projects
- And other specialized options for advanced scenarios

### Example

```bash
roslyn-language-server --stdio
```

## Requirements

- .NET 10.0 or later runtime

## Editor Integration

This language server is primarily used through editor extensions:

- **Visual Studio Code**: Integrated via the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- **Other LSP-compatible editors**: Can be configured to use this server for C# and Visual Basic support

## More Information

- [Roslyn GitHub Repository](https://github.com/dotnet/roslyn)
- [Language Server Protocol Specification](https://microsoft.github.io/language-server-protocol/)
- [Common Language Server Protocol Framework (CLaSP)](https://github.com/dotnet/roslyn/tree/main/src/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework)

## License

This tool is part of the .NET Compiler Platform ("Roslyn") and is licensed under the [MIT license](https://github.com/dotnet/roslyn/blob/main/License.txt).
