# Compiler Output Cache Experiment

> [!WARNING]
> This feature is **experimental**. It is intended for evaluation and dogfooding only, is not a supported compatibility surface, may have bugs or unintended side-effects, and may be changed or removed at any time without notice.

## Overview

Roslyn's compiler server already amortizes several compilation costs by keeping compiler state alive across requests. This experiment extends that idea by allowing the server to persist the outputs of successful compilations to disk and restore them on later requests when the compilation inputs are determined to be equivalent.

The current implementation is intentionally scoped as an experiment:

- it is only enabled for the `.NET 8+` compiler-server path
- it is primarily designed for repeated local or CI builds that produce identical outputs
- it is expected to evolve as we learn more about correctness, diagnostics, and operational behavior

## Goals

The experiment has four primary goals:

1. Avoid repeating identical compilations when the deterministic inputs have not changed.
2. Reuse successful compiler outputs across compiler-server lifetimes instead of limiting reuse to a single warm process.
3. Preserve correctness by falling back to a regular compilation on any cache miss or cache-related failure.
4. Produce enough logging to make cache hits, misses, and key mismatches diagnosable while the experiment is being evaluated.

## Non-goals

The current design does **not** attempt to provide:

- a stable on-disk format
- a long-term compatibility promise across Roslyn versions
- a user-facing product feature with committed command-line switches or SDK behavior
- a guarantee that every deterministic-looking build will be served from the cache

## Enablement

The experiment is only compiled into the relevant server-side code on `.NET 8+`.

At runtime, the cache is enabled when the compiler server observes the `ROSLYN_CACHE_PATH` environment variable. If that variable is not set, the server behaves exactly as it did before and no compilation outputs are cached.

There are currently two ways the variable can be provided:

1. Directly set `ROSLYN_CACHE_PATH` in the environment before invoking the compiler or build.
2. Use the MSBuild feature flag `use-global-cache`, which causes `Microsoft.Build.Tasks.CodeAnalysis` to set `ROSLYN_CACHE_PATH` when launching the compiler server.

The feature flag can be used in either of these forms:

- `use-global-cache`
- `use-global-cache=/absolute/path/to/cache`

When the feature flag is present without an explicit path, the current implementation uses:

```text
<temp>/roslyn-cache
```

If `ROSLYN_CACHE_PATH` is already set in the environment, that value wins and the feature flag does not override it.

## Cache key

The cache key is based on Roslyn's deterministic compilation key. For a given compilation request, the server computes a deterministic key from inputs such as:

- the compilation
- analyzers
- source generators
- additional files
- path mapping
- emit options
- manifest resources

That deterministic key is then hashed with SHA-256 and used as the directory key for the cache entry.

The cache is additionally grouped by output file name, so the effective lookup shape is:

```text
<cache root>/<output file name>/<sha-256-of-deterministic-key>/
```

## Cache format

Each cache entry is a directory containing the outputs of a successful compilation plus the full deterministic key used to produce them.

The current layout is:

```text
$ROSLYN_CACHE_PATH/
  <dll name>/
    <sha-256>/
      assembly
      pdb
      refassembly
      xmldoc
      <dll name>.key
```

Where:

- `assembly` is the primary emitted assembly and is required for a valid entry
- `pdb` is the emitted PDB, when one exists
- `refassembly` is the emitted reference assembly, when one exists
- `xmldoc` is the emitted XML documentation file, when one exists
- `<dll name>.key` contains the full deterministic key text

Only outputs that actually exist for a successful compilation are stored. Optional outputs are omitted when the compilation did not produce them.

## Lookup and population flow

For each eligible compilation request, the compiler server:

1. Computes the deterministic key.
2. Hashes that key.
3. Looks for an existing entry under the cache root.
4. If a valid cached assembly is present, copies the cached outputs to the requested output locations and reports success.
5. Otherwise performs a normal compilation.
6. After a successful compilation, attempts to publish the outputs into the cache for future requests.

Any failure while reading from or writing to the cache is treated as a cache miss or skipped store, not as a build failure. The request falls back to normal compilation behavior.

## Concurrency and publication

Cache entries are written using a staging directory and then published with a directory move. This is intended to ensure readers only observe fully populated entries.

Per-entry synchronization is used so that multiple writers racing on the same key do not all try to publish the same result concurrently. If another process is already populating an entry, the current request simply continues with normal compilation behavior.

## Diagnostics

While the experiment is active, the compiler server logs notable cache events, including:

- cache enabled
- cache hit
- cache miss
- cache store success
- cache store skipped because another writer won the race
- cache store or restore failures

On a miss, Roslyn may also log diffs against recent prior key files for the same output name. This is intended to help explain why two apparently similar builds did not reuse the same cached result.

## Current limitations

The experiment currently has several important limitations:

- `PathMap` stability is effectively required for useful cache reuse. Differences in path mapping participate in the deterministic key, so builds that do not normalize machine-specific source paths are much less likely to hit the same cache entry across machines or worktrees.
- Normal compiler behavior is not fully preserved on cache hits today. In particular, a restored cached result does not currently replay warnings and other compiler diagnostics that would normally be produced during a regular compilation.
- Because the cache key is based on the current deterministic-key computation, the set of inputs that control cache reuse should be treated as an implementation detail of the experiment rather than a guaranteed contract.
- The on-disk cache format is intentionally provisional and may change without compatibility support.

## Risks and open questions

Areas still under evaluation include:

- correctness of the deterministic key as a cache identity for all relevant build scenarios
- whether the current treatment of `PathMap` and other deterministic inputs is sufficient for the scenarios where this experiment is expected to be useful
- disk growth and cache cleanup policy
- behavior across SDK, compiler, or machine-environment changes
- whether the current enablement mechanism should remain environment-variable based
- what, if any, parts of the on-disk format should become durable

Because these questions are still open, consumers should treat this document as a description of the current experiment, not a product contract.
