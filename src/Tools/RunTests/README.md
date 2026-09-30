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
| `--testInactivityTimeout` | Local VSTest inactivity timeout in seconds (default: 600) |
| `--workItemTimeout` | Optional local work-item process deadline in seconds (default: no separate deadline) |
| `--dumpTimeout` | Total dump-helper budget per timed-out work item in seconds (default: 120) |
| `--integration` | Allow VS integration setup: 25-minute inactivity timeout; an explicit inactivity timeout overrides this default |
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
partition. By default:

- VSTest blame aborts after **10 minutes without test progress**, collecting a full
  hang dump. This is an inactivity timeout, not an assembly-duration limit.
- RunTests limits the **whole run to 90 minutes**, even if discovery,
  the test host, or VSTest's own dump collector stops responding. There is no
  separate assembly-duration limit. Use `--workItemTimeout` to opt into a
  per-work-item deadline.

On the work-item deadline, RunTests first writes a synthetic failed xUnit result,
then attempts full dumps before killing the owned process tree. Dump APIs execute
in a helper mode of the same RunTests binary: Windows uses `MiniDumpWriteDump` for
both Framework and Core; Linux uses `DiagnosticsClient`. Collection has a separate
two-minute total budget per work item, followed by up to ten seconds for process
exit/output draining. Dump failures do not prevent termination or reporting the
failure. Other queued assemblies continue, but the overall run fails.

The global deadline stops scheduling new work and uses the same bounded cleanup
for every active work item, including when no work-item deadline was specified.
Process ancestry is sampled during execution; only the
launcher and its observed descendants are considered, never machine-wide
testhost-name matches. As with any sampled ancestry tracking, a child that starts
and becomes orphaned between samples can escape observation.

Diagnostics live beneath `TestResults/<configuration>/WorkItem_<index>_<arch>/`
in an invocation-specific directory (or beneath `--out`). Synthetic failure XML
is next to the normal xUnit results. Existing CI test-result artifact publication
includes these dumps and sequence files. `--collectdumps` additionally enables
Windows WER crash collection when running as administrator; timeout dump attempts
do not require that option and do not require changing the registry.

`eng/test-vsi.ps1` passes `--integration` to preserve additional VSIX/hive setup
time. Helix retains its existing 15-minute VSTest timeout and infrastructure
deadline; local timeout options are rejected with `--helix`.

For a short local watchdog probe, use e.g.
`--workItemTimeout 30 --testInactivityTimeout 120 --dumpTimeout 10`.
For an inactivity probe instead, make `--testInactivityTimeout` shorter than
`--workItemTimeout`. For a global-deadline probe, run multiple hanging assemblies
with `--timeout 1` and no work-item override; the default inactivity timeout is
longer, so the global deadline initiates dump collection. All supplied timeout
values must be positive.

## Exit Codes

- `0` — All tests passed (or `--help` was shown)
- `1` — Test failures, timeout, or invalid arguments
