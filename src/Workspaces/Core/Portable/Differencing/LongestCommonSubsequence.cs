// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Differencing
{
    /// <summary>
    /// Calculates Longest Common Subsequence.
    /// </summary>
    internal abstract class LongestCommonSubsequence<TSequence>
    {
        private const int DeleteCost = 1;
        private const int InsertCost = 1;
        private const int UpdateCost = 2;

        protected abstract bool ItemsEqual(TSequence oldSequence, int oldIndex, TSequence newSequence, int newIndex);

        protected IEnumerable<KeyValuePair<int, int>> GetMatchingPairs(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            int[,] d = ComputeCostMatrix(oldSequence, oldLength, newSequence, newLength);
            int i = oldLength;
            int j = newLength;

            while (i != 0 && j != 0)
            {
                if (d[i, j] == d[i - 1, j] + DeleteCost)
                {
                    i--;
                }
                else if (d[i, j] == d[i, j - 1] + InsertCost)
                {
                    j--;
                }
                else
                {
                    i--;
                    j--;
                    yield return new KeyValuePair<int, int>(i, j);
                }
            }
        }

        protected IEnumerable<SequenceEdit> GetEdits(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            int[,] d = ComputeCostMatrix(oldSequence, oldLength, newSequence, newLength);
            int i = oldLength;
            int j = newLength;

            while (i != 0 && j != 0)
            {
                if (d[i, j] == d[i - 1, j] + DeleteCost)
                {
                    i--;
                    yield return new SequenceEdit(i, -1);
                }
                else if (d[i, j] == d[i, j - 1] + InsertCost)
                {
                    j--;
                    yield return new SequenceEdit(-1, j);
                }
                else
                {
                    i--;
                    j--;
                    yield return new SequenceEdit(i, j);
                }
            }

            while (i > 0)
            {
                i--;
                yield return new SequenceEdit(i, -1);
            }

            while (j > 0)
            {
                j--;
                yield return new SequenceEdit(-1, j);
            }
        }

        /// <summary>
        /// Returns a distance [0..1] of the specified sequences.
        /// The smaller distance the more of their elements match.
        /// </summary>
        /// <summary>
        /// Returns a distance [0..1] of the specified sequences.
        /// The smaller distance the more of their elements match.
        /// </summary>
        protected double ComputeDistance(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            Debug.Assert(oldLength >= 0 && newLength >= 0);

            if (oldLength == 0 || newLength == 0)
            {
                return (oldLength == newLength) ? 0.0 : 1.0;
            }

            int lcsLength = 0;
            foreach (var pair in GetMatchingPairs(oldSequence, oldLength, newSequence, newLength))
            {
                lcsLength++;
            }

            int max = Math.Max(oldLength, newLength);
            Debug.Assert(lcsLength <= max);
            return 1.0 - (double)lcsLength / (double)max;
        }

        /// <summary>
        /// Calculates costs of all paths in an edit graph starting from vertex (0,0) and ending in vertex (lengthA, lengthB). 
        /// </summary>
        /// <remarks>
        /// The edit graph for A and B has a vertex at each point in the grid (i,j), i in [0, lengthA] and j in [0, lengthB].
        /// 
        /// The vertices of the edit graph are connected by horizontal, vertical, and diagonal directed edges to form a directed acyclic graph.
        /// Horizontal edges connect each vertex to its right neighbor. 
        /// Vertical edges connect each vertex to the neighbor below it.
        /// Diagonal edges connect vertex (i,j) to vertex (i-1,j-1) if <see cref="ItemsEqual"/>(sequenceA[i-1],sequenceB[j-1]) is true.
        /// 
        /// Editing starts with S = []. 
        /// Move along horizontal edge (i-1,j)-(i,j) represents the fact that sequenceA[i-1] is not added to S.
        /// Move along vertical edge (i,j-1)-(i,j) represents an insert of sequenceB[j-1] to S.
        /// Move along diagonal edge (i-1,j-1)-(i,j) represents an addition of sequenceB[j-1] to S via an acceptable 
        /// change of sequenceA[i-1] to sequenceB[j-1].
        /// 
        /// In every vertex the cheapest outgoing edge is selected. 
        /// The number of diagonal edges on the path from (0,0) to (lengthA, lengthB) is the length of the longest common subsequence.
        /// </remarks>
        private int[,] ComputeCostMatrix(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            var la = oldLength + 1;
            var lb = newLength + 1;

            // TODO: Optimization possible: O(ND) time, O(N) space
            // EUGENE W. MYERS: An O(ND) Difference Algorithm and Its Variations
            var d = new int[la, lb];

            d[0, 0] = 0;
            for (int i = 1; i <= oldLength; i++)
            {
                d[i, 0] = d[i - 1, 0] + DeleteCost;
            }

            for (int j = 1; j <= newLength; j++)
            {
                d[0, j] = d[0, j - 1] + InsertCost;
            }

            for (int i = 1; i <= oldLength; i++)
            {
                for (int j = 1; j <= newLength; j++)
                {
                    int m1 = d[i - 1, j - 1] + (ItemsEqual(oldSequence, i - 1, newSequence, j - 1) ? 0 : UpdateCost);
                    int m2 = d[i - 1, j] + DeleteCost;
                    int m3 = d[i, j - 1] + InsertCost;
                    d[i, j] = Math.Min(Math.Min(m1, m2), m3);
                }
            }

            return d;
        }
    }
}
