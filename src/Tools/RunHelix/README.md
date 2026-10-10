# RunHelix

RunHelix submits **already-built and prepared** Roslyn tests to Helix. It does
not build test projects, run tests locally, wait for remote test completion, or
retry failed work items. Use [RunTests](../RunTests/README.md) for local execution.

## Preparation and invocation

1. Build the test projects and tooling, for example with `dotnet build Roslyn.slnx`.
   RunHelix is also included alongside RunTests in `Compilers.slnf` and `Ide.slnf`.
2. Run [PrepareTests](../PrepareTests/README.md) to discover tests and create a
   minimized payload with `testlist.json`, dependencies, and rehydration scripts.
3. On the submission machine, rehydrate RunHelix from that payload. The submission
   machine must use the same OS family as the target Helix queue.
4. Invoke RunHelix directly and specify the target queue.

For example, from the root of a downloaded Windows payload:

```powershell
$env:HELIX_CORRELATION_PAYLOAD = Join-Path $PWD '.duplicate'
& .\artifacts\bin\RunHelix\Debug\net10.0\rehydrate.cmd
dotnet exec .\artifacts\bin\RunHelix\Debug\net10.0\RunHelix.dll --ci --testConfiguration Debug --testFramework core --helixQueueName '<queue>'
```

On Unix, run the corresponding `rehydrate.sh` with Bash. The CI submission
templates install the SDK specified by the payload's `global.json`.

## Options

Run the built executable with `--help` for all supported options.

| Option | Description |
|--------|-------------|
| `--helixQueueName` | Required target Helix queue |
| `--helixApiAccessToken` | Authentication token for queues requiring authenticated access |
| `--testConfiguration` | `Debug` or `Release` (default: Debug) |
| `--testFramework` | `core` or `desktop` (repeatable, defaults to both) |
| `--include` / `--exclude` | Include or exclude test project names using regex patterns (repeatable) |
| `--testSet` | `compiler` adds compiler test patterns to any supplied includes |
| `--testKind` | `ioperation`, `runtimeasync`, or `usedassemblies`; runtime-async requires explicit Core-only selection |
| `--testPlatform` | Test process architecture: `x86`, `x64`, or `arm64` |
| `--artifactspath` | Override the artifacts directory |
| `--env:KEY=VALUE` | Set an environment variable in test processes |
| `--ci` | Enable CI-specific test behavior |

RunHelix accepts submission options only. Local-only options such as `--testfilter`,
`--sequential`, `--timeout`, `--collectdumps`, `--html`, `--out`, and `--logs` are
not accepted. RunHelix always uses the submission artifact locations below.

## Partitioning and history

RunHelix selects assemblies using the same discovery rules as RunTests, then
partitions the discovered tests into work items using historical timings when
available. It falls back to test counts when timings are unavailable. Helix
workers execute the generated VSTest response files directly.

The history lookup options are separate from the Helix API token:

| Option | Default environment variable |
|--------|------------------------------|
| `--accessToken` | `SYSTEM_ACCESSTOKEN` |
| `--projectUri` | `SYSTEM_COLLECTIONURI` |
| `--pipelineDefinitionId` | `SYSTEM_DEFINITIONID` |
| `--targetBranchName` | `SYSTEM_PULLREQUEST_TARGETBRANCH`, then `BUILD_SOURCEBRANCH` |

History lookup can fall back to `main` if the target branch has no successful
build. Test-run names distinguish configuration, runtime, architecture, and test
kinds selected through `--testKind` or `--env`.

## Artifacts and completion

Paths are relative to the selected artifacts directory:

- `helix.proj`: generated submission project.
- `payloads/`: generated work-item payloads, scripts, and response files.
- `log/<Configuration>/`: submission diagnostics, including `helix.binlog`,
  a copy of `helix.proj`, and work-item files.

Sequential submissions reuse these paths. The Windows CI template preserves
each submission's outputs before starting the next one.

A zero exit code means **submission succeeded**, not that remote tests passed
(`--help` also returns zero). Invalid arguments or submission failures return a
nonzero exit code. The pipeline's **Monitor Helix Jobs** job owns remote
completion monitoring and automatic retries.
