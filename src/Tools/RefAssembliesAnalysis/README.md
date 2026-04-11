# ref assemblies

This repository contains a tool for analyzing **ref assembly before/after pairs**.

Its purpose is to help answer a specific question:

> Can we remove some categories of compiler-generated artifacts from ref assemblies to reduce ref assembly churn?

Reducing churn matters because unnecessary differences in ref assemblies can cause avoidable cache misses. If we can identify categories of changes that do not represent meaningful public contract changes, we can improve caching and make builds more stable and efficient.

## What the analyzer does

The analyzer compares `before.dll` and `after.dll` ref assembly pairs and classifies each pair as:

- `valid-pair` when there are differences between before and after
- `ignored-attribute-versioning-only` when the only tracked differences are the three version-only assembly attribute buckets, so the pair is excluded from churn counts
- `same-mvid` when no tracked differences were observed and the compared ref assemblies share the same MVID
- `bad-dll` when a before/after pair cannot be analyzed because one of the DLLs is unreadable or has invalid metadata
- `partial-pair` when only one side of a before/after pair is present, so no comparison can be performed

For valid pairs, each individual diff entry is categorized into buckets such as:

- `other-public` — public API surface changes not covered by a more specific category (catch-all)
- `state-machine`
- `async-state-machine-attribute`
- `iterator-state-machine-attribute`
- `test-method-attribute`
- `benchmark-attribute`
- `display-class`
- `lambda-method`
- `local-function`
- `awaiter-field`
- `lambda-or-dynamic-cache`
- `assembly-versioning`
- `user-authored-non-public` (likely IVT)

These buckets show which kinds of compiler artifacts are contributing to churn. The `other-public` catch-all contains diffs that need manual review. Only pairs whose categories are limited to `assembly-file-version`, `assembly-informational-version`, and/or `assembly-metadata` stay visible in `pair-results.json` while being excluded from churn summaries and visual diff folders.

## Why this exists

The goal is not just to diff assemblies. The goal is to understand whether some categories of compiler-generated metadata could be excluded from ref assemblies without losing important public contract information.

If certain categories can be safely ignored or removed, then:

- fewer ref assemblies would appear to change
- incremental and remote caching could improve
- builds could become cheaper and more predictable

## Repository layout

- `analyzer\` contains the analysis tool
- `dummy-assemblies\` contains small hand-authored test pairs (for unittesting)
- the dataset folders contain larger real-world inputs and generated analysis outputs

## Running the analyzer

From `analyzer\`:

```powershell
dotnet run Program.cs -- "..\dummy-assemblies"
dotnet run Program.cs -- "..\ref-assemblies"
```

My pairs of ref assemblies are stored in `Q:\repos\ref-assemblies\ref-assemblies`, but you can use a folder you like.

The analyzer writes reports into an `output\` folder under the input directory, including:

- `pair-results.json`
- `summary.json`
- `summary.txt`
- `before\{category}\...`
- `after\{category}\...`

The `before\` and `after\` directories each contain one subfolder per diff category (e.g. `other-public`, `assembly-versioning`, `user-authored-ivt`, etc.) for visual diff. Folder-compare `before\other-public\` vs `after\other-public\` to review a specific category. Only pairs with items in that category are included. The first path segment of each `PairId` is preserved as a subdirectory, and deeper segments are flattened into the filename, so all files within a subdirectory are immediately visible in a folder diff tool.

Member/type signatures in these diff files show raw metadata visibility. Category bucketing still distinguishes visible API from non-visible metadata using the analyzer's surface checks, so a member may display `public` or `internal` metadata while still being categorized as non-public if its containing type is not externally visible.
