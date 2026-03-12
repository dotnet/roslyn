// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Generates sparse variable-length n-grams from a string, using the hash-based monotonic-stack
/// algorithm from GitHub's Blackbird code search engine.
/// <para/>
/// Ported from the C++ reference implementation at https://github.com/danlark1/sparse_ngrams
/// (Boost License 1.0). See also: https://github.blog/2023-02-06-the-technology-behind-githubs-new-code-search/
/// <para/>
/// The key insight: fixed-length trigrams (as used by Russ Cox's codesearch and Zoekt/Sourcegraph)
/// have high collision rates. A 3-character trigram over a 38-character alphabet has only ~54K
/// possible values, so any given trigram matches many documents. Longer n-grams are exponentially
/// more selective, but generating <b>all</b> overlapping n-grams of all lengths would be O(n*k).
/// <para/>
/// The sparse n-gram algorithm instead uses bigram hashes to select a <b>specific subset</b> of
/// variable-length n-grams — at most 2n-2 for indexing and at most n-2 for querying. The lengths
/// emerge naturally from the hash structure (monotonic stack boundaries), producing longer n-grams
/// where the text is "hash-monotonic" and shorter ones at hash valleys. This gives much higher
/// selectivity than trigrams alone, with bounded output size.
/// </summary>
internal static class SparseNgramGenerator
{
    /// <summary>
    /// Minimum n-gram length produced by the algorithm (a single bigram window = 2 chars,
    /// but the algorithm produces substrings from stack-start to current-position+2, so
    /// minimum is 3 when a stack element is immediately adjacent to the current bigram).
    /// In practice, the shortest n-gram is a trigram.
    /// </summary>
    public const int MinNgramLength = 3;

    // Hash constants from the reference implementation.
    private const ulong Mul1 = 0xc6a4a7935bd1e995UL;
    private const ulong Mul2 = 0x228876a7198b743UL;

    private static uint HashBigram(ReadOnlySpan<char> text, int pos)
    {
        var a = (ulong)text[pos] * Mul1 + (ulong)text[pos + 1] * Mul2;
        return (uint)(a + (~a >> 47));
    }

    /// <summary>
    /// Produces the full set of sparse n-grams for indexing (at most 2n-2). These should be
    /// stored in the document's Bloom filter / index at index time.
    /// <para/>
    /// The algorithm walks each bigram position, maintains a monotonic stack ordered by hash,
    /// and emits n-grams spanning from popped/top stack positions to the current bigram end.
    /// This produces variable-length n-grams whose boundaries are determined by hash valleys.
    /// </summary>
    public static void BuildAllNgrams(ReadOnlySpan<char> text, ref TemporaryArray<(int start, int length)> results)
    {
        if (text.Length < MinNgramLength)
            return;

        using var _ = ArrayBuilder<(uint hash, int pos)>.GetInstance(out var stack);

        for (var i = 0; i + 2 <= text.Length; i++)
        {
            var hash = HashBigram(text, i);

            while (stack.Count > 0 && hash > stack[^1].hash)
            {
                var ngramStart = stack[^1].pos;
                var ngramLength = i + 2 - ngramStart;
                results.Add((ngramStart, ngramLength));

                // Glue same hashes to the left.
                while (stack.Count > 1 && stack[^1].hash == stack[^2].hash)
                    stack.RemoveAt(stack.Count - 1);

                stack.RemoveAt(stack.Count - 1);
            }

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
    /// Produces a minimal covering set of sparse n-grams for querying (at most n-2). These are
    /// checked against the document's Bloom filter at query time. Covers the string with the
    /// fewest n-grams from the <see cref="BuildAllNgrams"/> set, preferring longer ones.
    /// </summary>
    public static void BuildCoveringNgrams(
        ReadOnlySpan<char> text,
        ref TemporaryArray<(int start, int length)> results,
        int maxNgramLength = 16)
    {
        if (text.Length < MinNgramLength)
            return;

        // Uses a deque (LinkedList here) to track the monotonic structure.
        var deque = new LinkedList<(uint hash, int pos)>();

        for (var i = 0; i + 2 <= text.Length; i++)
        {
            var hash = HashBigram(text, i);

            if (deque.Count > 1 &&
                i - deque.First!.Value.pos + 3 >= maxNgramLength)
            {
                var frontPos = deque.First.Value.pos;
                var secondPos = deque.First.Next!.Value.pos;
                results.Add((frontPos, secondPos + 2 - frontPos));
                deque.RemoveFirst();
            }

            while (deque.Count > 0 && hash > deque.Last!.Value.hash)
            {
                if (deque.First!.Value.hash == deque.Last.Value.hash)
                {
                    var lastPos = deque.Last.Value.pos;
                    results.Add((lastPos, i + 2 - lastPos));

                    while (deque.Count > 1)
                    {
                        var lastPosition = deque.Last.Value.pos + 2;
                        deque.RemoveLast();
                        results.Add((deque.Last!.Value.pos, lastPosition - deque.Last.Value.pos));
                    }
                }

                deque.RemoveLast();
            }

            deque.AddLast((hash, i));
        }

        while (deque.Count > 1)
        {
            var lastPosition = deque.Last!.Value.pos + 2;
            deque.RemoveLast();
            results.Add((deque.Last!.Value.pos, lastPosition - deque.Last.Value.pos));
        }
    }

    /// <summary>
    /// Checks whether all covering n-grams of <paramref name="text"/> are probably present in
    /// <paramref name="filter"/>. Uses <see cref="BuildCoveringNgrams"/> to get the minimal
    /// set of n-grams to check.
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
