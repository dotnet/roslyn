# RunTests

A test runner tool for the Roslyn repository that executes **already-built** test assemblies from the `artifacts/bin` directory. It does not build anything — you must build test projects before running this tool.

The purpose of RunTests is to run large batches of test assemblies locally using multiple concurrent VSTest processes (`dotnet exec vstest.console.dll`). For running a single test assembly, just use `dotnet test` directly. To submit tests to Helix instead, use [RunHelix](../RunHelix/README.md).

## How It Works

1. Scans `artifacts/bin/` for project directories matching `--include` regex patterns (default: `.*UnitTests.*`)
2. Within each matching project, finds assemblies under `<Configuration>/<TargetFramework>/`
3. Filters by target framework based on `--testFramework` (core or desktop, repeatable)
4. Creates one work item per assembly and executes tests via concurrent VSTest processes (or one at a time with `--sequential`)

Local execution does not partition assemblies using historical test timings and
does not require prepared `testlist.json` files.

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

The build scripts also forward CI mode (`-ci` in PowerShell, `--ci` in Bash)
as `--ci`, so RunTests configures CI-specific test behavior.

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
| `--testSet` | `compiler` adds compiler test assembly patterns to any `--include` patterns |
| `--testKind` | `ioperation`, `runtimeasync`, or `usedassemblies`; `runtimeasync` requires `--testFramework:core` |
| `--testfilter` | VSTest filter expression passed as `/TestCaseFilter` |
| `--timeout` | Minutes before killing tests (default: 90) |
| `--sequential` | Execute one assembly at a time |
| `--out` | Test results directory (default: `artifacts/TestResults/<Configuration>`) |
| `--logs` | Diagnostic log directory (defaults to the test results directory) |
| `--html` / `--html-` | Enable/disable HTML reports and opening failed results (default: on locally, off with `--ci`) |
| `--ci` | Apply CI behavior, including disabling HTML reports by default |
| `--env:KEY=VALUE` | Set environment variable in test processes |

`--testSet:compiler` selects compiler test assemblies when used alone. With
`--include`, assemblies matching either the compiler patterns or any supplied
include pattern are selected; `--exclude` still removes matches from that selection.

`Test.cmd` and `test.sh` pass the repository's artifacts directory explicitly.
Pass `--artifactspath <path>` to override it when testing a different payload.

Runtime-async validation requires an explicit Core-only selection, for example
`./test.sh --testKind:runtimeasync --testFramework:core`. Omitting the framework
selects both Core and desktop by default and is rejected for runtime-async
validation; RunTests does not automatically change the selected frameworks.

`Test.cmd`, `test.sh`, build-script test options, single-machine pipeline jobs, and
Visual Studio integration tests always use RunTests. RunTests accepts local
execution options only; invoke RunHelix directly for remote submission.

## Exit Codes

- `0` — All tests passed (or `--help` was shown)
- `1` — Test failures, timeout, or invalid arguments
