## Summary

Stacked on top of [#82708](https://github.com/dotnet/roslyn/pull/82708) (sparse n-gram search indexing). Can be reviewed as a single commit.

Replaces the CRC-style multiplicative hash in `SparseNgramGenerator` with a **frequency-based bigram weight table** generated from Roslyn's own identifier corpus. This is the optimization described in [Cursor's blog post on fast regex search](https://cursor.com/blog/fast-regex-search) under "Sparse N-grams: Smarter Trigram Selection":

> Let's pick something very smart: a hash function that gives a high weight to every pair of characters that is actually *very rare*, and a low weight to every pair that is *very frequent*. [...] the covering mode results in *even fewer* n-grams to lookup, and fewer documents that can possibly match.

### Why try this

The sparse n-gram algorithm places n-gram boundaries at "hash valleys" — positions where the bigram hash is locally minimal. With a pseudo-random hash (CRC-style), boundaries are effectively random. With a frequency-based hash, boundaries cluster around **common** character pairs (like "er", "in", "st") because those get low weights, while **rare** pairs (like "gb" in "StringBuilder") get high weights and stay interior to long n-grams.

The hypothesis: longer, more selective covering n-grams at query time — fewer Bloom filter lookups, each with fewer false positives.

### Empirical comparison: CRC vs frequency-based hash on Roslyn's codebase

A new tool (`NgramHashComparison`) runs both hash functions on all 13,986 C# files in Roslyn's `src/` directory. Full report at [`docs/ide/ngram-hash-comparison.md`](../docs/ide/ngram-hash-comparison.md).

#### Bloom filter storage impact

| Percentile | CRC Bloom | Freq Bloom | Delta |
|---|---|---|---|
| **Median** | 1.0 KB | 1.0 KB | -14 B (-1.3%) |
| P90 | 3.3 KB | 3.2 KB | -17 B (-0.5%) |

**Total across Roslyn:** 21.5 MB (CRC) → 21.3 MB (Freq), **-207 KB (-0.9%)**

The frequency hash produces slightly *fewer* distinct n-grams, shrinking Bloom filters marginally.

#### Query-time covering n-grams

| Metric | CRC | Freq | Delta |
|---|---|---|---|
| Total covering n-grams | 4,783,006 | 5,343,885 | **+11.7%** |
| Mean covering length | 5.12 chars | 4.79 chars | **-0.33** |
| Covering length >= 5 | 40.8% | 36.1% | **-4.7pp** |
| Covering length >= 7 | 22.1% | 18.8% | **-3.3pp** |

The frequency hash produces **more** covering n-grams that are on average **shorter** — the opposite of what we'd hope for query selectivity.

#### Per-identifier results are mixed

| Identifier | CRC covering | Freq covering | Winner |
|---|---|---|---|
| `stringbuilder` | 1 | 8 | CRC (extreme) |
| `tostring` | 5 | 2 | **Freq** |
| `cancellationtoken` | 9 | 5 | **Freq** |
| `addrange` | 4 | 3 | **Freq** |
| `getdefaultvalue` | 3 | 4 | CRC |
| `trygetvalue` | 4 | 4 | Tie |
| `xmldocumentwriter` | 4 | 6 | CRC |
| `diagnosticanalyzer` | 4 | 6 | CRC |

Individual identifiers go either way. But across the full corpus, CRC wins on aggregate selectivity metrics.

### Analysis: why frequency-based hashing hurts for identifiers

#### The original insight (for code text) is correct

The Cursor/Blackbird approach was designed for **arbitrary source code text** — entire files including operators, braces, whitespace, comments, and string literals. In that context:

- The most common bigrams are syntactic noise: `  `, `//`, `()`, `{}`, `; `
- The rare bigrams are the distinctive letter combinations inside identifiers — the signal
- Giving noise low weight (= boundary) and signal high weight (= interior) means n-grams span through distinctive identifier text and break at uninteresting syntax

This works because the two populations (noise vs. signal) have well-separated frequencies, and the weight distribution is **bimodal** — syntax characters at one extreme, identifier characters at the other. The monotonic stack sees deep valleys at syntax and tall peaks at identifiers, producing long, selective n-grams.

#### Identifiers are a degenerate case

Our use case is fundamentally different: we've already extracted the identifiers. The input to the n-gram algorithm is pure identifier text like `cancellationtoken` and `stringbuilder`. All the syntactic noise has been stripped away.

In this world:

1. **The character distribution is narrow.** The effective alphabet is 26 letters (plus a handful of digits and underscores). Every bigram is a letter-letter pair. There is no bimodal separation between "noise" and "signal" because everything is signal.

2. **Common bigrams are densely packed.** `cancellationtoken` has common pairs at nearly every position: `an`(35), `nc`(51), `ce`(29), `el`(68), `ll`(74), `la`(20), `at`(9), `ti`(7), `io`(12), `on`(2), `nt`(11), `to`(41), `ok`(575), `ke`(148), `en`(8). Twelve of fifteen bigrams have weight below 75. There's barely any gap between one common pair and the next.

3. **The weight distribution is clustered.** Because ~80% of bigrams in identifier text map to the bottom 5–10% of the weight range, the hash values form a flat, noisy signal — lots of micro-fluctuations around similar values rather than decisive peaks and valleys.

#### How this interacts with the monotonic stack

The monotonic stack produces long n-grams when it sees **deep valleys followed by long ascending runs** — a valley with a very low hash stays on the stack for many iterations because subsequent hashes keep being higher. The longer a valley survives, the longer the n-gram it anchors.

Think of the hash sequence as terrain:

- **CRC = the Rocky Mountains.** Hash values are uniformly distributed over a vast range (~0 to 4 billion). When a valley forms, it's genuinely deep — the hash might be 50,000 while surrounding values are in the billions. That valley stays on the stack for many positions. Valleys are well-separated, peaks are tall, and the stack does decisive, infrequent work. Result: fewer, longer n-grams.

- **Frequency weights on identifiers = a gently rolling plain.** Most hash values cluster between 0 and 100. Every few positions, something slightly higher or lower appears. A value of 35 is pushed, then 51 pops it, then 29 pushes, then 68 pops both. The stack constantly churns through micro-valleys that never survive more than 2–3 positions. Result: many short n-grams.

This isn't about individual identifiers being lucky or unlucky. **A uniform distribution over a wide range systematically produces better monotonic-stack behavior than a clustered distribution over a narrow range.** The clustering is inherent to our input: identifiers are composed almost entirely of common English bigrams, so frequency weights for the vast majority of what we see are squeezed into a narrow low band.

#### The rare fragments help, but not enough

An important nuance: the frequency approach *does* produce selective rare fragments. For `stringbuilder`, the covering set includes `ngb` — a rare trigram that appears in very few documents. So why doesn't this help?

The covering set must span the **entire** query string. It can't skip the common parts. So alongside the selective `ngb`, the covering set also contains `str`, `tri`, `ring`, `der` — common trigrams genuinely present in many unrelated documents (anything with `string`, `attribute`, `render`, etc.).

When we AND all covering n-grams as a filter, the single rare fragment (`ngb`) does nearly all the filtering work. The other 7 are redundant — they pass the Bloom filter check for most documents anyway. We're doing 8 Bloom filter probes to achieve filtering that's at best comparable to what CRC achieves with fewer probes using longer n-grams that embed the rare content *together with its surrounding context*.

A 13-character n-gram like `stringbuilder` is strictly more selective than any 3-character substring of it. It contains the rare `ngb` transition *plus* all surrounding information. Length is the dominant factor in selectivity, and the frequency approach systematically produces shorter covering n-grams because boundaries are too densely placed.

### Conclusion

**Frequency-based hashing is not beneficial for identifier-focused sparse n-grams.** The CRC hash in the base PR is the better choice for our use case. The technique is well-suited for its original domain (arbitrary code text with a bimodal character distribution), but identifier text is a degenerate case where the weight distribution collapses into a narrow band, degrading monotonic-stack performance.

This branch is exploratory — the data, analysis, and tooling are preserved for future reference, but the recommendation is to keep the CRC hash.

### What changed

**New tool: `BigramFrequencyAnalyzer`** — Scans all C# files in a directory, extracts identifier tokens via Roslyn's syntax API, lowercases them, computes bigram frequencies, and generates a rank-based weight table as C# source. Run against Roslyn's own `src/` directory:

- 13,984 files, 260,678 unique identifiers, 5.8M bigrams
- 92.4% of possible bigrams (1,265 / 1,369) were observed
- Top: `er` (135K), `te` (130K), `on` (110K), `in` (106K)

**New tool: `NgramHashComparison`** — Runs both CRC and frequency hash functions side-by-side on the entire codebase and produces a comparative analysis report covering Bloom filter sizes, n-gram length distributions, covering set analysis, boundary placement patterns, and per-identifier breakdowns. Full output at [`docs/ide/ngram-hash-comparison.md`](../docs/ide/ngram-hash-comparison.md).

**New file: `SparseNgramGenerator.FrequencyWeights.cs`** — The generated weight table (37x37 = 1,369 entries for the identifier alphabet: a-z, 0-9, underscore) plus a `FrequencyHashBigram` method that looks up weights from the table, falling back to the CRC hash for characters outside the identifier alphabet.

**Updated: `SparseNgramGenerator.cs`** — Made `partial`; `HashBigram` now delegates to `FrequencyHashBigram`.

**Updated: `SparseNgramTests.cs`** — Tests now validate structural properties (bounds, subset relationships, determinism, minimum lengths) rather than exact n-gram values, since the specific n-grams produced are hash-function-dependent.

**Updated: `AbstractSyntaxIndex_Persistence.cs`** — Bumped serialization format checksum to invalidate cached data.

### Rollback

This is a pure hash function swap. The algorithm, API, and integration are unchanged. Reverting this single commit restores the CRC-style hash with no other impact.
