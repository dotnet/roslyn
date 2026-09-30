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
| `--testfilter` | xUnit filter expression passed to `dotnet test --filter` |
| `--timeout` | Global local-run deadline in minutes (default: 90) |
| `--helix` | Submit test work items to Helix instead of running locally |
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

Helix submission returns after the jobs are submitted; the pipeline's **Monitor
Helix Jobs** job monitors completion and retries. Test-run names distinguish
configuration, runtime, architecture, and the test kinds selected through
`--testKind` or `--env`.

Historical timing data for Helix partitioning can be selected with `--accessToken`,
`--projectUri`, `--pipelineDefinitionId`, and `--targetBranchName`. When omitted,
these use the corresponding Azure Pipelines environment variables.

## Local timeout diagnostics

Each local work item currently runs one **whole assembly**, not a method-sized
partition. The timeout policy is:

- VSTest blame aborts after **25 minutes without test progress**, collecting a full
  hang dump. This fixed inactivity timeout is not an assembly-duration limit.
- RunTests limits the **whole run to 90 minutes**, even if discovery,
  the test host, or VSTest's own dump collector stops responding. There is no
  separate assembly-duration limit.

The global deadline stops scheduling new work and writes a synthetic failed xUnit
result for every active work item, then attempts full dumps before killing its
owned process tree. Dump APIs execute in helper subprocesses of the same RunTests
binary: Windows uses `MiniDumpWriteDump` for both Framework and Core; Linux uses
`DiagnosticsClient`. Collection has no timeout: helpers are awaited until they
finish or the processes are externally terminated. A blocked helper therefore
waits for external termination, such as the CI job timeout. Completed collection
is followed by awaiting process exit and output draining. Reported dump
failures do not prevent attempts on the remaining candidates or failure reporting.

At cancellation, RunTests takes a parent-process snapshot and traverses each
launcher's descendants, then dumps that process list. It does not track processes
during normal execution or select machine-wide testhost-name matches. Children
that have already been reparented before the snapshot are not included.

Diagnostics live beneath `TestResults/<configuration>/WorkItem_<index>_<arch>/`
in an invocation-specific directory (or beneath `--out`). Synthetic failure XML
is next to the normal xUnit results. Existing CI test-result artifact publication
includes these dumps and sequence files. `--collectdumps` additionally enables
Windows WER crash collection when running as administrator; timeout dump attempts
do not require that option and do not require changing the registry.

The local inactivity allowance also accommodates VSIX/hive setup for integration
tests. On a failed test run, `eng/test-vsi.ps1` captures `TestFailure.png` in the
artifact log directory before its cleanup. RunTests itself does not take screenshots.
Helix retains its existing 15-minute VSTest timeout and infrastructure
deadline; `--timeout` is rejected with `--helix`.

For a global-deadline probe, run multiple hanging assemblies with `--timeout 1`.
The fixed inactivity timeout is longer, so the global deadline initiates dump
collection. The `--timeout` value must be positive.

## Exit Codes

- `0` — All tests passed (or `--help` was shown)
- `1` — Test failures, timeout, or invalid arguments
