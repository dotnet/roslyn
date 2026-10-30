# Prepare Tests

PrepareTests discovers tests and minimizes already-built output for upload and
download between CI build and test machines. It does not build the test projects.

## Usage

After building the test projects, PrepareTests, and the appropriate runners:

```powershell
dotnet exec .\artifacts\bin\PrepareTests\Debug\net10.0\PrepareTests.dll --source . --destination .\artifacts\testPayload
```

Use `--unix` when preparing a Unix payload, and `--dotnetPath <path>` to select
the dotnet executable used for test discovery. The source is the repository root,
not just its `artifacts/bin` directory. Use a fresh destination directory.

The resulting payload contains:

- Unit and integration test output, including discovered `testlist.json` files.
- Built RunTests and RunHelix output and dependencies. RunHelix is optional for
  local-only builds, including Visual Studio integration test builds.
- Engineering scripts, VS setup artifacts when present, `global.json`, and
  `NuGet.config`, so downstream jobs can run without a source checkout.
- A `.duplicate` directory of shared binaries and per-output-directory
  `rehydrate.cmd` or `rehydrate.sh` scripts.

## Local execution and Helix submission

[RunTests](../RunTests/README.md) executes whole assemblies locally using VSTest;
it does not need `testlist.json`. Use `rehydrate-all.cmd` or `rehydrate-all.sh`
to restore a downloaded payload before local execution.

[RunHelix](../RunHelix/README.md) uses `testlist.json` and historical timings
(or a test-count fallback) to partition tests into remote work items. It creates
the Helix project and work-item payloads, then submits them. Helix workers invoke
VSTest directly rather than either runner.

The Windows and Unix Helix submission templates rehydrate RunHelix before
invoking it. Set `HELIX_CORRELATION_PAYLOAD` to the downloaded payload's
`.duplicate` directory when running an individual rehydration script.
Single-machine and Visual Studio integration jobs continue to use RunTests.

Helix separates shared **correlation payloads**, reused across work items on a
machine, from **work-item payloads**, downloaded for individual work items.
Keeping duplicate binaries in the shared payload reduces repeated transfers.

## Implementation

1. Run TestDiscoveryWorker over test assemblies to create `testlist.json`.
2. Walk selected test, runner, and supporting output directories.
3. Read DLL module version IDs (MVIDs) and group copies with the same identity.
4. Hard-link one copy of each duplicate DLL into `.duplicate`, named by its MVID.
   Hard-link unique DLLs and other files at their original relative paths.
5. Generate per-directory rehydration scripts that restore duplicate DLLs by
   hard-linking from `HELIX_CORRELATION_PAYLOAD`. Unix scripts also restore
   executable permissions where needed.
6. Generate a `rehydrate-all` script for single-machine execution.

RunTests and RunHelix participate in the same deduplication and rehydration
process as the test outputs. Omitting an unbuilt RunHelix directory does not
prevent creation of a local-only payload.
