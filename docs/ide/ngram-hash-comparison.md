# CRC Hash vs Frequency-Based Hash: Sparse N-gram Comparison

Corpus: 13986 C# files, 13946 with identifiers (length >= 3).
Both use the same sparse n-gram algorithm; only the bigram hash function differs.

## Per-document Bloom filter size (at 0.01% FPR)

| Percentile | CRC Bloom | Freq Bloom | Delta | Change |
|---|---|---|---|---|
| P10 | 358 B | 348 B | -10 B | -2.8% |
| P25 | 566 B | 556 B | -10 B | -1.8% |
| **Median** | 1.0 KB | 1.0 KB | -14 B | -1.3% |
| P75 | 1.9 KB | 1.9 KB | -17 B | -0.9% |
| P90 | 3.3 KB | 3.2 KB | -17 B | -0.5% |
| P95 | 4.5 KB | 4.4 KB | -36 B | -0.8% |

**Total across all docs:** 21.5 MB (CRC) vs 21.3 MB (Freq), delta -207.1 KB (-0.9%)

## Per-document distinct n-gram counts (BuildAllNgrams)

| Percentile | CRC distinct | Freq distinct | Delta | Change |
|---|---|---|---|---|
| P10 | 149 | 145 | -4 | -2.7% |
| P25 | 236 | 232 | -4 | -1.7% |
| **Median** | 444 | 438 | -6 | -1.4% |
| P75 | 824 | 817 | -7 | -0.8% |
| P90 | 1,391 | 1,384 | -7 | -0.5% |

## Index-time n-gram length distribution (BuildAllNgrams)

Total n-grams: 25,339,431 (CRC) vs 24,788,569 (Freq)

| Length | CRC count | CRC % | Freq count | Freq % | Shift |
|---|---|---|---|---|---|
| 3 | 14,931,803 | 58.9% | 14,931,803 | 60.2% | +1.3pp |
| 4 | 4,672,923 | 18.4% | 4,246,164 | 17.1% | -1.3pp |
| 5 | 2,173,929 | 8.6% | 2,084,441 | 8.4% | -0.2pp |
| 6 | 1,165,397 | 4.6% | 1,145,433 | 4.6% | +0.0pp |
| 7 | 659,785 | 2.6% | 643,785 | 2.6% | -+0.0pp |
| 8 | 451,719 | 1.8% | 465,947 | 1.9% | +0.1pp |
| 9 | 298,018 | 1.2% | 314,449 | 1.3% | +0.1pp |
| 10 | 225,048 | 0.9% | 225,876 | 0.9% | +0.0pp |
| 11 | 177,988 | 0.7% | 169,626 | 0.7% | -+0.0pp |
| 12 | 130,965 | 0.5% | 119,125 | 0.5% | -+0.0pp |
| 13 | 92,751 | 0.4% | 91,711 | 0.4% | +0.0pp |
| 14 | 73,360 | 0.3% | 67,083 | 0.3% | -+0.0pp |
| 15 | 56,759 | 0.2% | 55,168 | 0.2% | -+0.0pp |
| 16+ | 228,986 | 0.9% | 227,958 | 0.9% | +0.0pp |

## Query-time covering n-grams (BuildCoveringNgrams)

This is what matters for query performance: fewer, longer covering n-grams = fewer
Bloom filter lookups, each more selective.

Total covering n-grams: 4,783,006 (CRC) vs 5,343,885 (Freq), +11.7%

| Length | CRC count | CRC % | Freq count | Freq % | Shift |
|---|---|---|---|---|---|
| 3 | 1,826,703 | 38.2% | 2,604,190 | 48.7% | +10.5pp |
| 4 | 1,004,348 | 21.0% | 808,485 | 15.1% | -5.9pp |
| 5 | 532,761 | 11.1% | 546,804 | 10.2% | -0.9pp |
| 6 | 364,016 | 7.6% | 380,578 | 7.1% | -0.5pp |
| 7 | 240,592 | 5.0% | 230,389 | 4.3% | -0.7pp |
| 8 | 181,618 | 3.8% | 200,700 | 3.8% | -+0.0pp |
| 9 | 147,604 | 3.1% | 136,168 | 2.5% | -0.5pp |
| 10 | 125,420 | 2.6% | 117,557 | 2.2% | -0.4pp |
| 11 | 113,354 | 2.4% | 96,552 | 1.8% | -0.6pp |
| 12 | 93,235 | 1.9% | 84,991 | 1.6% | -0.4pp |
| 13 | 79,995 | 1.7% | 70,388 | 1.3% | -0.4pp |
| 14 | 73,360 | 1.5% | 67,083 | 1.3% | -0.3pp |
| 16+ | 0 | 0.0% | 0 | 0.0% | +0.0pp |

**Mean covering n-gram length:** 5.12 chars (CRC) vs 4.79 chars (Freq)

## Sample identifiers: covering n-grams comparison

| Identifier | CRC covering set | Freq covering set |
|---|---|---|
| `getvalue` | `getv`, `tvalue` (2) | `getv`, `tvalue` (2) |
| `tostring` | `ing`, `rin`, `stri`, `ost`, `tos` (5) | `tos`, `ostring` (2) |
| `readline` | `readl`, `ine`, `dlin` (3) | `rea`, `ead`, `adl`, `dline` (4) |
| `stringbuilder` | `stringbuilder` (1) | `str`, `tri`, `ring`, `ngb`, `der`, `lde`, `uild`, `gbui` (8) |
| `cancellationtoken` | `can`, `ancell`, `lla`, `latio`, `iont`, `ken`, `oke`, `tok`, `nto` (9) | `cancel`, `ell`, `llationtok`, `ken`, `oke` (5) |
| `iasyncenumerable` | `ias`, `asy`, `rable`, `syncenumera` (4) | `iasyn`, `ble`, `abl`, `umerab`, `num`, `yncenu` (6) |
| `xmldocumentwriter` | `xmldoc`, `ocume`, `ment`, `ntwriter` (4) | `xml`, `mldocumentwr`, `ter`, `ite`, `rit`, `wri` (6) |

## Bigram weight distribution

The frequency hash assigns weight 0 to the most common bigram and weight 1368 to the rarest.
Higher weight = rarer bigram = more likely to be an n-gram boundary (monotonic stack peak).

**Top 20 most common bigrams** (lowest weight = least likely boundary):

| Rank | Bigram | Weight | Interpretation |
|---|---|---|---|
| 1 | `er` | 0 | Very common in identifiers |
| 2 | `te` | 1 | Very common in identifiers |
| 3 | `on` | 2 | Very common in identifiers |
| 4 | `in` | 3 | Very common in identifiers |
| 5 | `es` | 4 | Very common in identifiers |
| 6 | `re` | 5 | Very common in identifiers |
| 7 | `st` | 6 | Very common in identifiers |
| 8 | `ti` | 7 | Very common in identifiers |
| 9 | `en` | 8 | Very common in identifiers |
| 10 | `at` | 9 | Very common in identifiers |
| 11 | `me` | 10 | Very common in identifiers |
| 12 | `nt` | 11 | Very common in identifiers |
| 13 | `io` | 12 | Very common in identifiers |
| 14 | `et` | 13 | Very common in identifiers |
| 15 | `ar` | 14 | Very common in identifiers |
| 16 | `co` | 15 | Very common in identifiers |
| 17 | `ss` | 16 | Very common in identifiers |
| 18 | `as` | 17 | Very common in identifiers |
| 19 | `le` | 18 | Very common in identifiers |
| 20 | `it` | 19 | Very common in identifiers |

**Top 20 rarest bigrams** (highest weight = most likely boundary):

| Rank | Bigram | Weight | Interpretation |
|---|---|---|---|
| 1 | `9z` | 1368 | Extremely rare in identifiers |
| 2 | `9y` | 1367 | Extremely rare in identifiers |
| 3 | `9x` | 1366 | Extremely rare in identifiers |
| 4 | `9q` | 1365 | Extremely rare in identifiers |
| 5 | `9m` | 1364 | Extremely rare in identifiers |
| 6 | `9l` | 1363 | Extremely rare in identifiers |
| 7 | `9k` | 1362 | Extremely rare in identifiers |
| 8 | `9j` | 1361 | Extremely rare in identifiers |
| 9 | `9i` | 1360 | Extremely rare in identifiers |
| 10 | `9h` | 1359 | Extremely rare in identifiers |
| 11 | `9g` | 1358 | Extremely rare in identifiers |
| 12 | `8y` | 1357 | Extremely rare in identifiers |
| 13 | `8x` | 1356 | Extremely rare in identifiers |
| 14 | `8q` | 1355 | Extremely rare in identifiers |
| 15 | `8k` | 1354 | Extremely rare in identifiers |
| 16 | `7z` | 1353 | Extremely rare in identifiers |
| 17 | `7y` | 1352 | Extremely rare in identifiers |
| 18 | `7x` | 1351 | Extremely rare in identifiers |
| 19 | `7v` | 1350 | Extremely rare in identifiers |
| 20 | `7u` | 1349 | Extremely rare in identifiers |

## N-gram boundary analysis (where do n-grams start?)

Sampled 5,000 identifiers. A "boundary" is the bigram immediately before an n-gram start position.

**CRC hash — most frequent boundaries:**

| Bigram before break | Count | Freq weight |
|---|---|---|
| `er` | 2,753 | 0 |
| `ti` | 2,515 | 7 |
| `re` | 2,492 | 5 |
| `en` | 1,952 | 8 |
| `te` | 1,849 | 1 |
| `es` | 1,744 | 4 |
| `se` | 1,656 | 37 |
| `on` | 1,615 | 2 |
| `in` | 1,581 | 3 |
| `st` | 1,573 | 6 |
| `at` | 1,490 | 9 |
| `ec` | 1,410 | 29 |
| `et` | 1,401 | 13 |
| `ge` | 1,375 | 46 |
| `le` | 1,262 | 18 |

**Frequency hash — most frequent boundaries:**

| Bigram before break | Count | Freq weight |
|---|---|---|
| `er` | 2,586 | 0 |
| `ti` | 2,342 | 7 |
| `re` | 2,258 | 5 |
| `es` | 1,702 | 4 |
| `on` | 1,655 | 2 |
| `in` | 1,613 | 3 |
| `te` | 1,573 | 1 |
| `st` | 1,507 | 6 |
| `et` | 1,496 | 13 |
| `at` | 1,485 | 9 |
| `en` | 1,444 | 8 |
| `le` | 1,229 | 18 |
| `ed` | 1,212 | 24 |
| `ex` | 1,195 | 47 |
| `ng` | 1,188 | 42 |

## Detailed identifier breakdown

Shows where each hash function places n-gram boundaries. `|` marks a boundary.

- **`stringbuilder`**
  - CRC boundaries:  `stringbuilder` → 1 covering n-grams (all: 21, distinct: 21)
  - Freq boundaries: `s|t|ri|n|gb|ui|l|der` → 8 covering n-grams (all: 14, distinct: 14)
- **`tostring`**
  - CRC boundaries:  `t|o|st|r|ing` → 5 covering n-grams (all: 7, distinct: 7)
  - Freq boundaries: `t|ostring` → 2 covering n-grams (all: 10, distinct: 10)
- **`cancellationtoken`**
  - CRC boundaries:  `c|ance|l|lat|io|n|t|o|ken` → 9 covering n-grams (all: 21, distinct: 21)
  - Freq boundaries: `canc|e|llationt|o|ken` → 5 covering n-grams (all: 25, distinct: 25)
- **`getdefaultvalue`**
  - CRC boundaries:  `ge|tdefaul|tvalue` → 3 covering n-grams (all: 23, distinct: 23)
  - Freq boundaries: `ge|tdef|aul|tvalue` → 4 covering n-grams (all: 22, distinct: 22)
- **`xmldocumentwriter`**
  - CRC boundaries:  `xmld|ocu|me|ntwriter` → 4 covering n-grams (all: 26, distinct: 26)
  - Freq boundaries: `x|mldocument|w|r|i|ter` → 6 covering n-grams (all: 24, distinct: 24)
- **`iasyncenumerable`**
  - CRC boundaries:  `i|a|syncenume|rable` → 4 covering n-grams (all: 24, distinct: 24)
  - Freq boundaries: `ias|ynce|n|umer|a|ble` → 6 covering n-grams (all: 22, distinct: 22)
- **`trygetvalue`**
  - CRC boundaries:  `t|ry|ge|tvalue` → 4 covering n-grams (all: 14, distinct: 14)
  - Freq boundaries: `t|r|yge|tvalue` → 4 covering n-grams (all: 14, distinct: 14)
- **`addrange`**
  - CRC boundaries:  `a|d|d|range` → 4 covering n-grams (all: 8, distinct: 8)
  - Freq boundaries: `a|d|drange` → 3 covering n-grams (all: 9, distinct: 9)
- **`argumentnullexception`**
  - CRC boundaries:  `a|r|gu|me|ntnulle|xc|ept|ion` → 8 covering n-grams (all: 30, distinct: 30)
  - Freq boundaries: `a|r|gumen|tnulle|xce|pt|ion` → 7 covering n-grams (all: 31, distinct: 31)
- **`diagnosticanalyzer`**
  - CRC boundaries:  `d|ia|gn|osticanalyzer` → 4 covering n-grams (all: 28, distinct: 28)
  - Freq boundaries: `d|i|a|gnosticanal|y|zer` → 6 covering n-grams (all: 26, distinct: 26)

## Selectivity analysis

For a random search query, longer covering n-grams eliminate more false-positive documents.
Below we compare the fraction of covering n-grams that are 'selective' (length >= 5).

| Metric | CRC | Freq | Delta |
|---|---|---|---|
| Covering n-grams with length >= 5 | 1,951,955 (40.8%) | 1,931,210 (36.1%) | -4.7pp |
| Covering n-grams with length >= 7 | 1,055,178 (22.1%) | 1,003,828 (18.8%) | |
| Mean covering length | 5.12 | 4.79 | -0.33 |

## Summary

The frequency-based hash function, trained on Roslyn's own identifier corpus:

- **Bloom filter storage**: -0.9% (-207.1 KB)
- **Distinct n-grams**: Slightly fewer (-0.9%)
- **Covering n-gram count**: +11.7% (more covering n-grams means more Bloom filter probes)
- **Mean covering length**: 5.12 → 4.79 (-0.33 chars)
- **Selective (>= 5 char) covering share**: 40.8% → 36.1%
