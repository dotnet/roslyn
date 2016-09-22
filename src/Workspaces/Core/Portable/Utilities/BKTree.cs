﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Utilities;
using System;

namespace Roslyn.Utilities
{
    /// <summary>
    /// NOTE: Only use if you truly need a BK-tree.  If you just want to compare words, use
    /// the <see cref="SpellChecker"/> type instead.
    ///
    /// An implementation of a Burkhard-Keller tree.  Introduced in:
    /// 
    /// 'Some approaches to best-match file searching.'
    /// Communications of the ACM CACM
    /// Volume 16 Issue 4, April 1973 
    /// Pages 230-236 
    /// http://dl.acm.org/citation.cfm?doid=362003.362025
    /// </summary>
    internal partial class BKTree
    {
        public static readonly BKTree Empty = new BKTree(
            Array.Empty<char>(),
            ImmutableArray<Node>.Empty,
            ImmutableArray<Edge>.Empty);

        // We have three completely flat arrays of structs.  These arrays fully represent the 
        // BK tree.  The structure is as follows:
        //
        // The root node is in _nodes[0].
        //
        // It lists the count of edges it has.  These edges are in _edges in the range 
        // [0*, childCount).  Each edge has the index of the child node it points to, and the
        // edit distance between the parent and the child.
        //
        // * of course '0' is only for the root case.  
        //
        // All nodes state where in _edges their child edges range starts, so the children 
        // for any node are in the range[node.FirstEdgeIndex, node.FirstEdgeIndex + node.EdgeCount).
        //
        // Each node also has an associated string.  These strings are concatenated and stored
        // in _concatenatedLowerCaseWords.  Each node has a TextSpan that indicates which portion
        // of the character array is their string.  Note: i'd like to use an immutable array
        // for the characters as well.  However, we need to create slices, and they need to 
        // work on top of an ArraySlice (which needs a char[]).  The edit distance code also
        // wants to work on top of raw char[]s (both for speed, and so it can pool arrays
        // to prevent lots of garbage).  Because of that we just keep this as a char[].
        private readonly char[] _concatenatedLowerCaseWords;
        private readonly ImmutableArray<Node> _nodes;
        private readonly ImmutableArray<Edge> _edges;

        private BKTree(char[] concatenatedLowerCaseWords, ImmutableArray<Node> nodes, ImmutableArray<Edge> edges)
        {
            _concatenatedLowerCaseWords = concatenatedLowerCaseWords;
            _nodes = nodes;
            _edges = edges;
        }

        public static BKTree Create(params string[] values)
        {
            return Create(values.Select(v => new StringSlice(v)));
        }

        public static BKTree Create(IEnumerable<StringSlice> values)
        {
            return new Builder(values).Create();
        }

        public IList<string> Find(string value, int? threshold = null)
        {
            if (_nodes.Length == 0)
            {
                return SpecializedCollections.EmptyList<string>();
            }

            var lowerCaseCharacters = ArrayPool<char>.GetArray(value.Length);
            try
            {
                for (var i = 0; i < value.Length; i++)
                {
                    lowerCaseCharacters[i] = CaseInsensitiveComparison.ToLower(value[i]);
                }

                threshold = threshold ?? WordSimilarityChecker.GetThreshold(value);
                var result = new List<string>();
                Lookup(_nodes[0], lowerCaseCharacters, value.Length, threshold.Value, result, recursionCount: 0);
                return result;
            }
            finally
            {
                ArrayPool<char>.ReleaseArray(lowerCaseCharacters);
            }
        }

        private void Lookup(
            Node currentNode,
            char[] queryCharacters,
            int queryLength,
            int threshold,
            List<string> result, 
            int recursionCount)
        {
            // Don't bother recursing too deeply in the case of pathological trees.
            // This really only happens when the actual code is strange (like
            // 10,000 symbols all a single letter long).  In htat case, searching
            // down this path will be fairly fruitless anyways.
            //
            // Note: this won't affect good searches against good data even if this
            // pathological chain exists.  That's because the good items will still
            // cluster near the root node in the tree, and won't be off the end of
            // this long chain.
            if (recursionCount > 256)
            {
                return;
            }

            // We always want to compute the real edit distance (ignoring any thresholds).  This is
            // because we need that edit distance to appropriately determine which edges to walk 
            // in the tree.
            var characterSpan = currentNode.WordSpan;
            var editDistance = EditDistance.GetEditDistance(
                new ArraySlice<char>(_concatenatedLowerCaseWords, characterSpan),
                new ArraySlice<char>(queryCharacters, 0, queryLength));

            if (editDistance <= threshold)
            {
                // Found a match.
                result.Add(new string(_concatenatedLowerCaseWords, characterSpan.Start, characterSpan.Length));
            }

            var min = editDistance - threshold;
            var max = editDistance + threshold;

            var startInclusive = currentNode.FirstEdgeIndex;
            var endExclusive = startInclusive + currentNode.EdgeCount;
            for (var i = startInclusive; i < endExclusive; i++)
            {
                var childEditDistance = _edges[i].EditDistance;
                if (min <= childEditDistance && childEditDistance <= max)
                {
                    Lookup(_nodes[_edges[i].ChildNodeIndex],
                        queryCharacters, queryLength, threshold, result,
                        recursionCount + 1);
                }
            }
        }

#if false
        // Used for diagnostic purposes.
        internal void DumpStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Nodes length: " + _nodes.Length);
            var childCountHistogram = new Dictionary<int, int>();

            foreach (var node in _nodes)
            {
                var childCount = node.EdgeCount;
                int existing;
                childCountHistogram.TryGetValue(childCount, out existing);

                childCountHistogram[childCount] = existing + 1;
            }

            sb.AppendLine();
            sb.AppendLine("Child counts:");
            foreach (var kvp in childCountHistogram.OrderBy(kvp => kvp.Key))
            {
                sb.AppendLine(kvp.Key + "\t" + kvp.Value);
            }

            // An item is dense if, starting from 1, at least 80% of it's array would be full.
            var densities = new int[11];
            var empyCount = 0;

            foreach (var node in _nodes)
            {
                if (node.EdgeCount == 0)
                {
                    empyCount++;
                    continue;
                }

                var maxEditDistance = -1;
                var startInclusive = node.FirstEdgeIndex;
                var endExclusive = startInclusive + node.EdgeCount;
                for (var i = startInclusive; i < endExclusive; i++)
                {
                    maxEditDistance = Max(maxEditDistance, _edges[i].EditDistance);
                }

                var editDistanceCount = node.EdgeCount;

                var bucket = 10 * editDistanceCount / maxEditDistance;
                densities[bucket]++;
            }

            var nonEmptyCount = _nodes.Length - empyCount;
            sb.AppendLine();
            sb.AppendLine("NoChildren: " + empyCount);
            sb.AppendLine("AnyChildren: " + nonEmptyCount);
            sb.AppendLine("Densities:");
            for (var i = 0; i < densities.Length; i++)
            {
                sb.AppendLine("<=" + i + "0% = " + densities[i] + ", " + ((float)densities[i] / nonEmptyCount));
            }

            var result = sb.ToString();
        }
#endif
    }
}