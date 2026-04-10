# ref assemblies analysis

Reducing churn matters because unnecessary differences in ref assemblies can cause avoidable cache misses.
If we can identify categories of changes that do not represent meaningful public contract changes, we can improve caching and make builds more stable and efficient.

## What the tool does

The analyzer compares `before.dll` and `after.dll` ref assembly pairs and classifies each pair as:

- `valid-pair` when there are differences between before and after
- `same-mvid` when no tracked differences were observed and the compared ref assemblies share the same MVID
- `bad-dll` when a before/after pair cannot be analyzed because one of the DLLs is unreadable or has invalid metadata
- `partial-pair` when only one side of a before/after pair is present, so no comparison can be performed

For valid pairs, each individual diff entry is categorized into one of the current buckets:

| Bucket | Meaning |
| --- | --- |
| `other-public` | Public API surface changes not covered by a more specific category. |
| `assembly-identity` | Assembly identity changes such as version, name, culture, or public key token differences. |
| `assembly-file-version` | Version-only `AssemblyFileVersionAttribute` changes whose before/after entries otherwise match. |
| `assembly-informational-version` | Version-only `AssemblyInformationalVersionAttribute` changes whose before/after entries otherwise match. |
| `assembly-metadata` | Version-only `AssemblyMetadataAttribute` changes whose before/after entries otherwise match. |
| `references` | Assembly reference changes from the `AssemblyRef` table. |
| `state-machine` | Compiler-generated async/iterator machinery such as `<Method>d__N` types, state fields, builder fields, current fields, and parameter/`this` proxy fields. |
| `public-async-state-machine-attribute` | Visible method entries whose before/after API signatures become identical after normalizing `AsyncStateMachineAttribute`. |
| `awaiter-field` | Compiler-generated awaiter slots such as `<>u__N` fields inside async/iterator state machines. |
| `iterator-finally` | Compiler-generated iterator cleanup helpers such as `<>m__Finally` methods. |
| `display-class` | Closure/display-class artifacts such as `<>c`, `<>c__DisplayClass*`, and `<>8__locals*`. |
| `lambda-method` | Synthesized lambda bodies such as `<Method>b__N` methods. |
| `local-function` | Synthesized local-function implementations, typically with Roslyn-generated `<Method>g__...` names. |
| `lambda-or-dynamic-cache` | Synthesized delegate or dynamic call-site caches such as `<>9`, `<>9__N`, `<>o__`, or `<>p__` fields/types. |
| `hoisted-local` | Hoisted user or synthesized locals that were lifted into generated frames or state machines. |
| `backing-field` | Compiler-generated backing fields such as auto-property or anonymous-type backing fields. |
| `anonymous-type-or-delegate` | Synthesized anonymous type or anonymous delegate artifacts. |
| `inline-array-or-readonly-list` | Synthesized helper types for inline arrays or compiler-generated read-only list wrappers. |
| `private-implementation-details` | Compiler-generated implementation storage under `<PrivateImplementationDetails>`. |
| `compiler-generated-other` | Other compiler-generated artifacts that match generated-name conventions but do not fit a more specific bucket. |
| `user-authored-ivt` | Non-public user-authored members in assemblies that have `InternalsVisibleToAttribute`. |
| `user-authored-other` | Non-public user-authored members in assemblies without `InternalsVisibleToAttribute`. |
| `other-metadata` | Metadata changes outside the modeled member/type signatures, such as module, resource, forwarder, file, or other assembly-level metadata. |
| `unrecognized-difference` | Differences outside the recognized buckets, such as MVID-only changes or raw content differences that remain after tracked comparisons. |

These buckets show which kinds of compiler artifacts are contributing to churn. In practice, `other-public`, `assembly-identity`, and `unrecognized-difference` usually deserve manual review first, while the compiler-generated and non-public buckets help quantify potentially avoidable churn.

## Why this exists

The goal is not just to diff assemblies. The goal is to understand whether some categories of compiler-generated metadata could be excluded from ref assemblies without losing important public contract information.

If certain categories can be safely ignored or removed, then:

- fewer ref assemblies would appear to change
- incremental and remote caching could improve
- builds could become cheaper and more predictable

## Repository layout

- `Program.cs` contains the file-based analysis tool
- `dummy-assemblies\` contains small hand-authored test pairs (for unit testing)
- dataset folders contain larger real-world inputs and generated analysis outputs

## Running the analyzer

From `src\Tools\RefAssembliesAnalysis\`:

```powershell
dotnet run .\Program.cs -- ".\dummy-assemblies"
dotnet run .\Program.cs -- "<path-to-dataset-root>"
```

The analyzer writes reports into an `output\` folder under the input directory, including:

- `pair-results.json`
- `summary.json`
- `summary.txt`
- `before\{category}\...`
- `after\{category}\...`

The `before\` and `after\` directories each contain one subfolder per diff bucket (for example `other-public`, `assembly-identity`, `state-machine`, or `user-authored-other`) for visual diff. Folder-compare `before\other-public\` vs `after\other-public\` to review a specific bucket. Only pairs with items in that bucket are included. The first path segment of each `PairId` is preserved as a subdirectory, and deeper segments are flattened into the filename, so all files within a subdirectory are immediately visible in a folder diff tool.

Member/type signatures in these diff files show raw metadata visibility. Category bucketing still distinguishes visible API from non-visible metadata using the analyzer's surface checks, so a member may display `public` or `internal` metadata while still being categorized as non-public if its containing type is not externally visible.
