# RunTests

A test runner tool for the Roslyn repository that executes **already-built** test assemblies from the `artifacts/bin` directory. It does not build anything — you must build test projects before running this tool.

The purpose of RunTests is to run large batches of test assemblies efficiently by saturating the machine with multiple concurrent `dotnet test` processes. For running a single test assembly, just use `dotnet test` directly.

## How It Works

1. Scans `artifacts/bin/` for project directories matching `--include` regex patterns (default: `.*UnitTests.*`)
2. Within each matching project, finds assemblies under `<Configuration>/<TargetFramework>/`
3. Filters by target framework based on `--testFramework` (core or desktop, repeatable)
4. Executes tests via multiple concurrent `dotnet test` processes, either locally or on Helix

## Quick Start

```bash
# Build first (RunTests does NOT build for you)
dotnet build Roslyn.slnx

# Then run all unit tests for Debug configuration
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --testConfiguration Debug

# Run only compiler tests on .NET Core
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --testFramework core --testSet compiler

# Run specific test assemblies by regex
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --include "CSharp\.Emit"

# Filter to specific test methods
dotnet run --project src/Tools/RunTests/RunTests.csproj -- --testfilter "FullyQualifiedName~MyTestClass"
```

## Build and test together

The build scripts expose a small set of test convenience options:

```powershell
.\Build.cmd -test
.\Build.cmd -configuration Release -testSet:compiler
.\Build.cmd -testFramework:core -testKind:ioperation
```

```bash
./build.sh --test
./build.sh --configuration Release --testSet:compiler
./build.sh --testFramework:core --testKind:ioperation
```

`test`, `testSet`, `testKind`, and `testFramework` each request a RunTests invocation
after successful build actions. The scripts forward the build configuration as
`--testConfiguration` and any supplied set, kind, and framework values unchanged.
RunTests owns test discovery, option validation, and execution. For other test
options, invoke RunTests directly.

For multiple frameworks, invoke the PowerShell script with an array:
`.\eng\build.ps1 -build -testFramework:core,desktop`. In Bash, repeat the option:
`./build.sh --testFramework:core --testFramework:desktop`.

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
| `--testFramework` | `core` or `desktop` (repeatable, defaults to both) |
| `--testSet` | `compiler` to run only compiler test assemblies |
| `--testKind` | `ioperation`, `runtimeasync`, or `usedassemblies` |
| `--testfilter` | xUnit filter expression passed to `dotnet test --filter` |
| `--timeout` | Minutes before killing tests (default: 90) |
| `--helix` | Submit test work items to Helix instead of running locally |
| `--env:KEY=VALUE` | Set environment variable in test processes |

Helix submission returns after the jobs are submitted; the pipeline's **Monitor
Helix Jobs** job monitors completion and retries. Test-run names distinguish
configuration, runtime, architecture, and the test kinds selected through
`--testKind` or `--env`.

Historical timing data for Helix partitioning can be selected with `--accessToken`,
`--projectUri`, `--pipelineDefinitionId`, and `--targetBranchName`. When omitted,
these use the corresponding Azure Pipelines environment variables.

## Exit Codes

- `0` — All tests passed (or `--help` was shown)
- `1` — Test failures, timeout, or invalid arguments
