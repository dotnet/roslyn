## Summary

Stacked on top of [#82706](https://github.com/dotnet/roslyn/pull/82706) (regex search support for NavigateTo).

Replaces the existing fixed-length trigram Bloom filter with a sparse n-gram Bloom filter, ported from the hash-based algorithm used by GitHub's [Blackbird code search engine](https://github.blog/2023-02-06-the-technology-behind-githubs-new-code-search/). The C# implementation is a faithful port of the C++ reference at [danlark1/sparse_ngrams](https://github.com/danlark1/sparse_ngrams).

### Why

Fixed-length trigrams have high collision rates: a 3-character trigram over a ~38-character alphabet has only ~54K possible values, so any given trigram matches many documents. Longer n-grams are exponentially more selective (a 5-gram has ~38x fewer collisions than a 4-gram), but generating *all* overlapping n-grams of all lengths would be O(n*k).

The sparse n-gram algorithm solves this by using bigram hashes and a monotonic stack to select a *smart subset* of variable-length n-grams — at most 2n-2 for indexing and at most n-2 for querying. The lengths emerge naturally from hash boundaries, producing longer n-grams where the text is hash-monotonic and shorter ones at hash valleys.

### What changed

**New file: `SparseNgramGenerator.cs`** — Ports the two core methods from the reference implementation:

- **`BuildAllNgrams`** (index time): monotonic-stack algorithm that produces at most 2n-2 variable-length n-grams. For "chester" this produces `{"che", "hes", "ches", "est", "chest", "ste", "ter", "ster"}` — 8 n-grams with lengths ranging from 3 to 5.
- **`BuildCoveringNgrams`** (query time): produces a minimal covering subset, at most n-2 n-grams. For "chester" this produces just `{"chest", "ster"}` — 2 n-grams that cover the entire string.
- **`CoveringNgramsProbablyContained`**: query-time convenience method that builds covering n-grams and checks them against the Bloom filter.

**Updated: `NavigateToSearchIndex.NavigateToSearchInfo.cs`** — Hooks the sparse n-gram generator into the existing indexing and query paths:

- Index time: `AddTrigramData` now calls `BuildAllNgrams` instead of generating fixed-length trigrams
- Query time: `TrigramCheckPasses` now calls `CoveringNgramsProbablyContained` instead of checking fixed-length n-grams at each position

**Updated: `NavigateToRegexPreFilterBenchmarks.cs`** — Added benchmarks for `TrigramCheckPasses` to measure sparse n-gram query performance.

### Test coverage

**`SparseNgramTests.cs`** — 26 tests covering:
- `BuildAllNgrams`: validated against the reference implementation's exact expected output for `"hello world"`, `"chester "`, `"chester"`, `"hell"`, `"hel"`, and `"for(int i=42"`. Also verifies the 2n-2 bound and minimum n-gram length.
- `BuildCoveringNgrams`: validated against reference output for the same inputs. Verifies the n-2 bound and that covering n-grams are a subset of all n-grams.
- `CoveringNgramsProbablyContained`: Bloom filter integration tests (match, substring, mismatch, case sensitivity, partial overlap).

### Rollback

This change is purely an indexing/filtering optimization. It affects which n-grams are stored and checked, but not the matching logic. If the sparse n-gram approach causes issues, reverting this PR restores the previous fixed-length behavior with no impact on the regex search feature from #82706.
