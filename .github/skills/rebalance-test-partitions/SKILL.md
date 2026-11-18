---
name: rebalance-test-partitions
description: "Regenerate eng/config/test-partitions.json, which RunTests uses to split large test assemblies into several processes and to order work items longest first in local (non-Helix) runs. Use when: rebalancing test partitions, the periodic test partition refresh, local or single-machine test runs have a long tail, an assembly should start or stop being partitioned, or RunTests partitions are out of date."
---

# Rebalance test partitions

`eng/config/test-partitions.json` drives local RunTests scheduling (see the **Local partitioning**
section of `src/Tools/RunTests/README.md`):

- `estimatedMinutes` orders work items longest first.
- `partitions` split a .NET Core assembly into one process per prefix group plus a remainder process.
- `doNotPartitionReason` keeps an assembly in one process. The script preserves these entries.

Test durations drift as tests are added, so the file is regenerated periodically (every two weeks)
from the **Test Windows CoreClr Debug Single Machine** job of the Roslyn CI pipeline
(`dnceng-public/public`, definition 95).

## Steps

1. Sign in to Azure DevOps: `az login`. Alternatively, set `AZDO_TOKEN` to a token with build and
   test read access to `dnceng-public`.
2. Start from an up to date `main` and run:

   ```
   dotnet run --file eng/rebalance-test-partitions.cs -- --summary artifacts/tmp/rebalance-summary.md
   ```

   By default the script uses the latest successful `main` build. Pass `--build <id>` to choose a
   build, or `--xml artifacts/TestResults/Debug` to use local xUnit results. `--threshold` (default
   20 minutes) sets which assemblies are partitioned, and `--target` (default 12 minutes) sets the
   partition size. Use `--dry-run` to see the summary without writing the file.
3. Review the summary and `git diff eng/config/test-partitions.json`:
   - New assemblies over the threshold should be split unless their tests mostly wait on child
     processes or I/O; splitting those adds load without reducing the wall time. For those assemblies,
     add a `doNotPartitionReason` and rerun the script.
   - An assembly reported as "could not be split" has a single class or namespace over the target.
     That's acceptable, but mention it in the PR.
   - Estimate changes of less than about a minute per assembly are noise. Skip the PR when the
     partitions did not otherwise change.
4. Optionally validate locally. Build, then compare a run with `--partition` against one without it. Pin both
   runs to 4 CPUs (`start /affinity F ...` on Windows) to emulate the CI agents:

   ```
   dotnet exec artifacts/bin/RunTests/Debug/net10.0/RunTests.dll --testConfiguration Debug --testFramework:core --ci --partition
   ```

5. Open a PR with only the regenerated `eng/config/test-partitions.json` and paste the summary into
   the description.

## Notes

- Partition filters use vstest `FullyQualifiedName~<prefix>`, a case-insensitive contains check. The
  script verifies that every known test class matches at most one explicit partition. The remainder
  partition is the complement of the explicit partitions, so every test runs exactly once even when
  classes are added or renamed.
- If the script can't find the test run or job log, the CI job has probably been renamed. Update
  `RunName` and `JobName` at the top of `eng/rebalance-test-partitions.cs`.
