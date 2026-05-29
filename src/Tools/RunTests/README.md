# RunTests

A test runner tool for the Roslyn repository that executes **already-built** test assemblies from the `artifacts/bin` directory. It does not build anything — you must build test projects before running this tool.

The purpose of RunTests is to run large batches of test assemblies efficiently by saturating the machine with multiple concurrent `dotnet test` processes. For running a single test assembly, just use `dotnet test` directly.

## How It Works

1. Scans `artifacts/bin/` for project directories matching `--include` regex patterns (default: `.*UnitTests.*`)
2. Within each matching project, finds assemblies under `<Configuration>/<TargetFramework>/`
3. Filters by target framework based on `--testFramework` (core, desktop, or both)
4. Executes tests via multiple concurrent `dotnet test` processes, either locally or on Helix

## Quick Start

```bash
# Build first (RunTests does NOT build for you)
dotnet build
# Then run all unit tests for Debug configuration
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --testConfiguration Debug

# Run only compiler tests on .NET Core
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --testFramework core --testSet compiler

# Run specific test assemblies by regex
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --include "CSharp\.Emit"

# Filter to specific test methods
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --testfilter "FullyQualifiedName~MyTestClass"
```

## Options

Run `--help` for the full list of options:

```bash
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --help
```

Key options:

| Option | Description |
|--------|-------------|
| `--testConfiguration` | `Debug` or `Release` (default: Debug) |
| `--include` | Regex pattern to match test project names (repeatable) |
| `--exclude` | Regex pattern to exclude test project names (repeatable) |
| `--testFramework` | `core`, `desktop`, or `both` |
| `--testSet` | `compiler` to run only compiler test assemblies |
| `--testKind` | `ioperation`, `runtimeasync`, or `usedassemblies` |
| `--testfilter` | xUnit filter expression passed to `dotnet test --filter` |
| `--timeout` | Minutes before killing tests (default: 90) |
| `--helix` | Submit test work items to Helix instead of running locally |
| `--env:KEY=VALUE` | Set environment variable in test processes |

## Exit Codes

- `0` — All tests passed (or `--help` was shown)
- `1` — Test failures, timeout, or invalid arguments
