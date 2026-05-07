# Sparse N-gram Storage Analysis

Corpus: **13984** C# files under `src/`, **13944** containing identifiers of length >= 3.

This analysis compares the Bloom filter storage cost of **fixed trigrams** (the previous
approach) vs **sparse n-grams** (this PR) on a per-document basis. Each C# source file is
treated as one NavigateTo document. All identifier tokens are lowercased and deduplicated
(matching NavigateTo's indexing behavior). The Bloom filter size is computed using Roslyn's
exact `BloomFilter.ComputeM` formula with the actual false positive rate of **0.0001**.

## Per-document Bloom filter size

| Percentile | Trigram Bloom | Sparse Bloom | Delta | Ratio |
|---|---|---|---|---|
| P10 | 211 B | 358 B | +147 B | 1.65x |
| P25 | 329 B | 566 B | +237 B | 1.72x |
| **Median** | 588 B | 1.0 KB | +476 B | 1.82x |
| P75 | 1021 B | 1.9 KB | +956 B | 1.95x |
| P90 | 1.6 KB | 3.3 KB | +1.7 KB | 2.11x |
| P95 | 2.0 KB | 4.5 KB | +2.5 KB | 2.27x |
| P99 | 3.4 KB | 9.5 KB | +6.1 KB | 2.74x |

### Totals across entire codebase

| Metric | Trigrams | Sparse | Delta |
|---|---|---|---|
| Mean Bloom/doc | 787 B | 1.6 KB | +827 B |
| Total (all docs) | 10.5 MB | 21.5 MB | +11.0 MB |

For a codebase the size of Roslyn (13984 C# files), the sparse n-gram Bloom filters
use **11.0 MB more** total storage than fixed trigrams
(10.5 MB -> 21.5 MB).

## Per-document distinct n-gram counts

The number of distinct strings inserted into each document's Bloom filter:

| Percentile | Trigrams | Sparse | Ratio |
|---|---|---|---|
| P10 | 88 | 149 | 1.69x |
| P25 | 137 | 236 | 1.72x |
| **Median** | 245 | 444 | 1.81x |
| P75 | 426 | 825 | 1.94x |
| P90 | 667 | 1,391 | 2.09x |

## Sparse n-gram length distribution

Total sparse n-grams emitted across all documents: **25,338,168**
Total fixed trigrams emitted: **14,931,022**

| Length | Count | % of total | Cumulative % |
|---|---|---|---|
| 3 | 14,931,022 | 58.9% | 58.9% |
| 4 | 4,672,689 | 18.4% | 77.4% |
| 5 | 2,173,835 | 8.6% | 85.9% |
| 6 | 1,165,336 | 4.6% | 90.5% |
| 7 | 659,748 | 2.6% | 93.2% |
| 8 | 451,699 | 1.8% | 94.9% |
| 9 | 298,006 | 1.2% | 96.1% |
| 10 | 225,041 | 0.9% | 97.0% |
| 11 | 177,984 | 0.7% | 97.7% |
| 12 | 130,958 | 0.5% | 98.2% |
| 13 | 92,754 | 0.4% | 98.6% |
| 14 | 73,359 | 0.3% | 98.9% |
| 15 | 56,758 | 0.2% | 99.1% |
| 16 | 41,792 | 0.2% | 99.3% |
| 17 | 32,723 | 0.1% | 99.4% |
| 18 | 26,844 | 0.1% | 99.5% |
| 19 | 21,742 | 0.1% | 99.6% |
| 20 | 17,619 | 0.1% | 99.7% |
| 21 | 14,332 | 0.1% | 99.7% |
| 22 | 11,584 | 0.0% | 99.8% |
| 23+ | 62,343 | 0.2% | 100.0% |

The majority (59%) are still trigrams — the same as the previous approach. The additional
41% are length 4+, which are exponentially more selective in the Bloom filter. A length-5
n-gram over a 37-character alphabet has ~37x fewer collisions than a trigram.

## Why the extra storage is worth it

The sparse n-gram approach trades a modest increase in Bloom filter size (~1.8x per document)
for dramatically higher selectivity at query time. When checking whether a document could
contain a search term, the Bloom filter lookup uses `BuildCoveringNgrams` which produces
the **fewest, longest** n-grams that cover the query. These longer n-grams have far fewer
false positives, meaning fewer documents need to be loaded and scanned.

For example, searching for "readline" with trigrams checks {"rea", "ead", "adl", "dli",
"lin", "ine"} — 6 lookups, each matching many documents. With sparse n-grams, the covering
set might be {"readl", "dline"} — 2 lookups, each far more selective.
