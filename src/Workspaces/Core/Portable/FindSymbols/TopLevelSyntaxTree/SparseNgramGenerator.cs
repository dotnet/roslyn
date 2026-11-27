// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Generates sparse variable-length n-grams from a string, using the hash-based monotonic-stack
/// algorithm from GitHub's Blackbird code search engine.
/// <para/>
/// Ported from the C++ reference implementation at https://github.com/danlark1/sparse_ngrams
/// (Boost License 1.0). See also: https://github.blog/2023-02-06-the-technology-behind-githubs-new-code-search/
/// <para/>
/// <b>Why not fixed-length trigrams?</b> Traditional code search indices (Russ Cox's codesearch,
/// Zoekt/Sourcegraph) use fixed trigrams. A 3-character trigram over a 38-character alphabet
/// (a-z, 0-9, underscore, "other") has only ~54K possible values, so any given trigram matches a
/// large fraction of documents. Searching for "ReadLine" produces trigrams {"Rea","ead","adL","dLi",
/// "Lin","ine"} — each individually matches many documents, and only the AND of all six narrows it
/// down. Longer n-grams are exponentially more selective, but naively generating all overlapping
/// n-grams of all lengths would be O(n²).
/// <para/>
/// <b>The sparse n-gram idea:</b> instead of extracting every possible substring, use bigram hashes
/// to deterministically select a <em>sparse subset</em> of variable-length n-grams whose boundaries
/// fall at "hash valleys" (local minima in the bigram hash sequence). The monotonic stack naturally
/// finds these valleys: when a new bigram has a higher hash than the stack top, the top is a valley
/// and the span from that valley to the current position becomes an n-gram. This produces longer
/// n-grams through regions where hashes are monotonically increasing, and shorter ones at valleys —
/// giving much higher selectivity than fixed trigrams, with bounded output size (at most 2n−2 for
/// indexing, n−2 for querying).
/// <para/>
/// <b>Running example:</b> consider indexing the identifier "GetValue". The bigram positions are:
/// <code>
///   pos: 0:"Ge" 1:"et" 2:"tV" 3:"Va" 4:"al" 5:"lu" 6:"ue"
/// </code>
/// Suppose the hash values create valleys at positions 1 and 5. The algorithm emits n-grams that
/// span from each valley to the next boundary: e.g. "etVa" (pos 1–4), "lue" (pos 5–7), plus
/// n-grams connecting across valleys like "GetV" (pos 0–3). The exact n-grams depend on the hash
/// function, but the key property is: they are <em>deterministic</em> for a given string, so the
/// same n-grams are produced at index time and query time.
/// <para/>
/// At query time, <see cref="BuildCoveringNgrams"/> extracts a <em>covering</em> subset — the
/// fewest, longest n-grams that together span the entire query string. If the query is "GetValue",
/// and the covering set is {"GetVal", "alue"}, then a document can only match if its Bloom filter
/// contains both. Because these are 6 and 4 characters long (not just 3), they have far fewer false
/// positives than any individual trigram would.
/// </summary>
internal static class SparseNgramGenerator
{
    /// <summary>
    /// The shortest n-gram the algorithm can produce is 3 characters (a trigram). This happens
    /// when two adjacent bigram positions form a valley — the span from one to the next covers
    /// exactly 3 characters. Strings shorter than this cannot produce any n-grams.
    /// </summary>
    public const int MinNgramLength = 3;

    // Hash constants from the reference implementation (https://github.com/danlark1/sparse_ngrams).
    // These are mixing constants chosen to spread bigram values across the uint range, ensuring
    // that n-gram boundaries are well-distributed and don't cluster on common character pairs.
    private const ulong Mul1 = 0xc6a4a7935bd1e995UL;
    private const ulong Mul2 = 0x228876a7198b743UL;

    /// <summary>
    /// Hashes the bigram at <paramref name="pos"/> (characters at pos and pos+1) into a uint.
    /// The hash determines where n-gram boundaries fall: valleys in the hash sequence become
    /// n-gram start/end points. A good hash function here is critical — it must spread common
    /// bigrams widely so that boundaries don't cluster on frequent character pairs like "th" or "er".
    /// </summary>
    private static uint HashBigram(ReadOnlySpan<char> text, int pos)
    {
        var a = text[pos] * Mul1 + text[pos + 1] * Mul2;
        return (uint)(a + (~a >> 47));
    }

    /// <summary>
    /// Produces the full set of sparse n-grams for indexing (at most 2n−2). These are stored in the
    /// document's Bloom filter at index time so that queries can later check for their presence.
    /// <para/>
    /// The algorithm maintains a stack of bigram positions ordered by increasing hash value (a
    /// "monotonic stack"). As we scan left to right, each new bigram either continues the ascending
    /// run (pushed onto the stack) or breaks it (pops entries with lower hashes). Each pop reveals
    /// a hash valley — a position where the hash was locally minimal — and emits an n-gram spanning
    /// from that valley to the current position. This is why the algorithm produces <em>variable-
    /// length</em> n-grams: a long ascending hash run produces a single long n-gram, while a
    /// sequence of valleys produces many short ones.
    /// <para/>
    /// Two kinds of n-grams are emitted per iteration:
    /// <list type="number">
    /// <item><b>Popped n-grams:</b> when the current hash exceeds the stack top, the top is popped
    /// and an n-gram from that position to the current bigram's end is emitted. This captures the
    /// span "guarded" by the valley.</item>
    /// <item><b>Extension n-gram:</b> after all pops, if the stack is non-empty, an n-gram from
    /// the new top to the current bigram's end is emitted. This extends the previous valley's n-gram
    /// to cover the current position, ensuring overlapping coverage of the entire string.</item>
    /// </list>
    /// Together, these two rules guarantee that every position in the string is covered by at least
    /// one indexed n-gram — the property that makes query-time checking sound.
    /// </summary>
    public static void BuildAllNgrams(ReadOnlySpan<char> text, ref TemporaryArray<(int start, int length)> results)
    {
        if (text.Length < MinNgramLength)
            return;

        using var _ = ArrayBuilder<(uint hash, int pos)>.GetInstance(out var stack);

        for (var i = 0; i + 2 <= text.Length; i++)
        {
            var hash = HashBigram(text, i);

            // Pop all stack entries with hashes ≤ the current hash. Each popped entry represents
            // a hash valley whose n-gram now terminates at the current bigram's end (position i+2).
            while (stack.Count > 0 && hash > stack[^1].hash)
            {
                var ngramStart = stack[^1].pos;
                var ngramLength = i + 2 - ngramStart;
                results.Add((ngramStart, ngramLength));

                // Collapse consecutive entries with the same hash value. When multiple bigrams hash
                // identically, they form a plateau — only the leftmost one matters as an n-gram
                // boundary, since the others would produce duplicate substrings of the same span.
                while (stack.Count > 1 && stack[^1].hash == stack[^2].hash)
                    stack.Pop();

                stack.Pop();
            }

            // Extend the current stack top's n-gram to include the current position. Without this,
            // the region between the last valley and the current position would not be covered by
            // any indexed n-gram, potentially causing false negatives at query time.
            if (stack.Count > 0)
            {
                var ngramStart = stack[^1].pos;
                var ngramLength = i + 2 - ngramStart;
                results.Add((ngramStart, ngramLength));
            }

            stack.Add((hash, i));
        }
    }

    /// <summary>
    /// Produces a minimal covering set of sparse n-grams for querying (at most n−2). At query time
    /// we need to check the document index for the presence of the query's n-grams. Checking fewer,
    /// longer n-grams is strictly better: each is more selective (fewer false positives) and we do
    /// fewer Bloom filter lookups.
    /// <para/>
    /// This method finds the <em>covering set</em>: the smallest number of n-grams from
    /// <see cref="BuildAllNgrams"/>'s output that together span the entire query string, preferring
    /// the longest possible n-grams. The soundness guarantee is: if a document contains a substring
    /// matching the query, then every covering n-gram of that query was emitted by
    /// <see cref="BuildAllNgrams"/> when that document was indexed, and therefore is in the
    /// document's Bloom filter.
    /// <para/>
    /// The algorithm uses a deque (double-ended queue) as a monotonic structure. The deque tracks
    /// hash valleys from front (oldest/leftmost) to back (newest/rightmost). N-grams are emitted
    /// in two situations:
    /// <list type="number">
    /// <item><b>Length cap:</b> when the span from the front of the deque to the current position
    /// would exceed <paramref name="maxNgramLength"/>, the front entry is evicted and an n-gram
    /// from the front to the second entry is emitted. This prevents impractically long n-grams that
    /// would rarely match and waste Bloom filter bits.</item>
    /// <item><b>Valley collapse:</b> when the current bigram hash exceeds the back of the deque
    /// (breaking the monotonic-decreasing invariant), entries are popped and n-grams emitted,
    /// covering the region from each popped valley to the current position.</item>
    /// </list>
    /// </summary>
    public static void BuildCoveringNgrams(
        ReadOnlySpan<char> text,
        ref TemporaryArray<(int start, int length)> results,
        int maxNgramLength = 16)
    {
        if (text.Length < MinNgramLength)
            return;

        using var _ = Deque<(uint hash, int pos)>.GetInstance(out var deque);

        for (var i = 0; i + 2 <= text.Length; i++)
        {
            var hash = HashBigram(text, i);

            // Evict the front if the span from the front valley to the current bigram's end would
            // exceed the maximum n-gram length. Capping length keeps n-grams practical — an
            // extremely long n-gram would be so specific it's unlikely to appear in the Bloom
            // filter, offering no benefit over a moderately long one.
            if (deque.Count > 1 &&
                i - deque.First.pos + 3 >= maxNgramLength)
            {
                var frontPos = deque.First.pos;
                var secondPos = deque[1].pos;
                results.Add((frontPos, secondPos + 2 - frontPos));
                deque.RemoveFirst();
            }

            // Pop entries from the back whose hashes are ≤ the current hash, maintaining the
            // monotonic invariant. When the front and back have the same hash (the entire deque
            // is a single plateau), the plateau collapses: we emit n-grams covering the full
            // span, then unwind the deque to emit bridging n-grams between consecutive entries.
            while (deque.Count > 0 && hash > deque.Last.hash)
            {
                if (deque.First.hash == deque.Last.hash)
                {
                    var lastPos = deque.Last.pos;
                    results.Add((lastPos, i + 2 - lastPos));

                    // Unwind the entire deque, emitting bridging n-grams that connect each
                    // consecutive pair. This ensures the covering set has no gaps.
                    while (deque.Count > 1)
                    {
                        var lastPosition = deque.Last.pos + 2;
                        deque.RemoveLast();
                        results.Add((deque.Last.pos, lastPosition - deque.Last.pos));
                    }
                }

                deque.RemoveLast();
            }

            deque.AddLast((hash, i));
        }

        // Drain any remaining entries after the last bigram. The deque still holds valleys that
        // were never popped (because no subsequent bigram had a higher hash). Unwinding these
        // emits the final covering n-grams that span to the end of the string.
        while (deque.Count > 1)
        {
            var lastPosition = deque.Last.pos + 2;
            deque.RemoveLast();
            results.Add((deque.Last.pos, lastPosition - deque.Last.pos));
        }
    }

    /// <summary>
    /// Checks whether all covering n-grams of <paramref name="text"/> are probably present in
    /// <paramref name="filter"/>. This is the query-time entry point: if <em>any</em> covering
    /// n-gram is missing from the filter, the document definitely does not contain the query
    /// substring, and we can skip the expensive full match. Returns <see langword="true"/> for
    /// strings shorter than <see cref="MinNgramLength"/> because no n-grams can be extracted
    /// and we must fall back to the full match.
    /// </summary>
    public static bool CoveringNgramsProbablyContained(
        ReadOnlySpan<char> text, Shared.Utilities.BloomFilter filter)
    {
        if (text.Length < MinNgramLength)
            return true;

        using var ngrams = TemporaryArray<(int start, int length)>.Empty;
        BuildCoveringNgrams(text, ref ngrams.AsRef());

        foreach (var (start, length) in ngrams)
        {
            if (!filter.ProbablyContains(text.Slice(start, length)))
                return false;
        }

        return true;
    }
}
