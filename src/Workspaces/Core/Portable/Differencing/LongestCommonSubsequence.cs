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
        // VArray struct enables array indexing in range [-d...d].
        private readonly struct VArray
        {
            private readonly int[] _array;

            public VArray(int[] array)
            {
                _array = array;
            }

            public void CopyFrom(VArray otherVArray)
            {
                var copyDelta = Offset - otherVArray.Offset;
                if (copyDelta >= 0)
                {
                    Array.Copy(otherVArray._array, 0, _array, copyDelta, otherVArray._array.Length);
                }
                else
                {
                    Array.Copy(otherVArray._array, -copyDelta, _array, 0, _array.Length);
                }
            }

            public int this[int index]
            {
                get => _array[index + Offset];
                set => _array[index + Offset] = value;
            }

            private int Offset => _array.Length / 2;
            public int[] UnderlyingArray => _array;
        }

        protected abstract bool ItemsEqual(TSequence oldSequence, int oldIndex, TSequence newSequence, int newIndex);

        // TODO: Consolidate return types between GetMatchingPairs and GetEdit to avoid duplicated code (https://github.com/dotnet/roslyn/issues/16864)
        protected IEnumerable<KeyValuePair<int, int>> GetMatchingPairs(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            var stackOfVs = ComputeEditPaths(oldSequence, oldLength, newSequence, newLength);

            var x = oldLength;
            var y = newLength;

            while (x > 0 || y > 0)
            {
                var currentV = new VArray(stackOfVs.Pop());
                var d = stackOfVs.Count;
                var k = x - y;

                // "snake" == single delete or insert followed by 0 or more diagonals
                // snake end point is in V
                var yEnd = currentV[k];
                var xEnd = yEnd + k;

                // does the snake first go down (insert) or right(delete)?
                var right = (k == d || (k != -d && currentV[k - 1] > currentV[k + 1]));
                var kPrev = right ? k - 1 : k + 1;

                // snake start point
                var yStart = currentV[kPrev];
                var xStart = yStart + kPrev;

                // snake mid point
                var yMid = right ? yStart : yStart + 1;
                var xMid = yMid + k;

                // return the matching pairs between (xMid, yMid) and (xEnd, yEnd) = diagonal part of the snake
                while (xEnd > xMid)
                {
                    Debug.Assert(yEnd > yMid);
                    xEnd--;
                    yEnd--;
                    yield return new KeyValuePair<int, int>(xEnd, yEnd);
                }

                x = xStart;
                y = yStart;
            }

        }

        protected IEnumerable<SequenceEdit> GetEdits(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            var stackOfVs = ComputeEditPaths(oldSequence, oldLength, newSequence, newLength);

            var x = oldLength;
            var y = newLength;

            while (x > 0 || y > 0)
            {
                var currentV = new VArray(stackOfVs.Pop());
                var d = stackOfVs.Count;
                var k = x - y;

                // "snake" == single delete or insert followed by 0 or more diagonals
                // snake end point is in V
                var yEnd = currentV[k];
                var xEnd = yEnd + k;

                // does the snake first go down (insert) or right(delete)?
                var right = (k == d || (k != -d && currentV[k - 1] > currentV[k + 1]));
                var kPrev = right ? k - 1 : k + 1;

                // snake start point
                var yStart = currentV[kPrev];
                var xStart = yStart + kPrev;

                // snake mid point
                var yMid = right ? yStart : yStart + 1;
                var xMid = yMid + k;

                // return the matching pairs between (xMid, yMid) and (xEnd, yEnd) = diagonal part of the snake
                while (xEnd > xMid)
                {
                    Debug.Assert(yEnd > yMid);
                    xEnd--;
                    yEnd--;
                    yield return new SequenceEdit(xEnd, yEnd);
                }

                // return the insert/delete between (xStart, yStart) and (xMid, yMid) = the vertical/horizontal part of the snake
                if (xMid > 0 || yMid > 0)
                {
                    if (xStart == xMid)
                    {
                        // insert
                        yield return new SequenceEdit(-1, --yMid);
                    }
                    else
                    {
                        // delete
                        yield return new SequenceEdit(--xMid, -1);
                    }
                }

                x = xStart;
                y = yStart;
            }
        }

        /// <summary>
        /// Returns a distance [0..1] of the specified sequences.
        /// The smaller distance the more similar the sequences are.
        /// </summary>
        /// <summary>
        /// Returns a distance [0..1] of the specified sequences.
        /// The smaller distance the more similar the sequences are.
        /// </summary>
        protected double ComputeDistance(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            Debug.Assert(oldLength >= 0 && newLength >= 0);

            if (oldLength == 0 || newLength == 0)
            {
                return (oldLength == newLength) ? 0.0 : 1.0;
            }

            // If the sequences differ significantly in size their distance will be very close to 1.0 
            // (even if one is a strict subsequence of the other).
            // Avoid running an expensive LCS algorithm on such sequences.
            double lenghtRatio = (oldLength > newLength) ? oldLength / newLength : newLength / oldLength;
            if (lenghtRatio > 100)
            {
                return 1.0;
            }

            var lcsLength = 0;
            foreach (var pair in GetMatchingPairs(oldSequence, oldLength, newSequence, newLength))
            {
                lcsLength++;
            }

            var max = Math.Max(oldLength, newLength);
            Debug.Assert(lcsLength <= max);
            return 1.0 - (double)lcsLength / (double)max;
        }

        /// <summary>
        /// Calculates a list of "V arrays" using Eugene W. Myers O(ND) Difference Algorithm
        /// </summary>
        /// <remarks>
        /// 
        /// The algorithm was inspired by Myers' Diff Algorithm described in an article by Nicolas Butler:
        /// https://www.codeproject.com/articles/42279/investigating-myers-diff-algorithm-part-of
        /// The author has approved the use of his code from the article under the Apache 2.0 license.
        /// 
        /// The algorithm works on an imaginary edit graph for A and B which has a vertex at each point in the grid(i, j), i in [0, lengthA] and j in [0, lengthB].
        /// The vertices of the edit graph are connected by horizontal, vertical, and diagonal directed edges to form a directed acyclic graph.
        /// Horizontal edges connect each vertex to its right neighbor. 
        /// Vertical edges connect each vertex to the neighbor below it.
        /// Diagonal edges connect vertex (i,j) to vertex (i-1,j-1) if <see cref="ItemsEqual"/>(sequenceA[i-1],sequenceB[j-1]) is true.
        /// 
        /// Move right along horizontal edge (i-1,j)-(i,j) represents a delete of sequenceA[i-1].
        /// Move down along vertical edge (i,j-1)-(i,j) represents an insert of sequenceB[j-1].
        /// Move along diagonal edge (i-1,j-1)-(i,j) represents an match of sequenceA[i-1] to sequenceB[j-1].
        /// The number of diagonal edges on the path from (0,0) to (lengthA, lengthB) is the length of the longest common sub.
        ///
        /// The function does not actually allocate this graph. Instead it uses Eugene W. Myers' O(ND) Difference Algoritm to calculate a list of "V arrays" and returns it in a Stack. 
        /// A "V array" is a list of end points of so called "snakes". 
        /// A "snake" is a path with a single horizontal (delete) or vertical (insert) move followed by 0 or more diagonals (matching pairs).
        /// 
        /// Unlike the algorithm in the article this implementation stores 'y' indexes and prefers 'right' moves instead of 'down' moves in ambiguous situations
        /// to preserve the behavior of the original diff algorithm (deletes first, inserts after).
        /// 
        /// The number of items in the list is the length of the shortest edit script = the number of inserts/edits between the two sequences = D. 
        /// The list can be used to determine the matching pairs in the sequences (GetMatchingPairs method) or the full editing script (GetEdits method).
        /// 
        /// The algorithm uses O(ND) time and memory where D is the number of delete/inserts and N is the sum of lengths of the two sequences.
        /// 
        /// VArrays store just the y index because x can be calculated: x = y + k.
        /// </remarks>
        private Stack<int[]> ComputeEditPaths(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            var stackOfVs = new Stack<int[]>();

            VArray AllocateVArray(int d) => new VArray(new int[2 * d + 1]);

            // special-case: the first "snake" to start at (-1,0 )
            var previousV = AllocateVArray(1);
            VArray currentV;

            var reachedEnd = false;

            for (var d = 0; d <= oldLength + newLength && !reachedEnd; d++)
            {
                // V is in range [-d...d] => use d to offset the k-based array indices to non-negative values
                if (d == 0)
                {
                    currentV = previousV;
                }
                else
                {
                    currentV = AllocateVArray(d);
                    currentV.CopyFrom(previousV);
                }

                for (var k = -d; k <= d; k += 2)
                {
                    // down or right? 
                    var right = k == d || (k != -d && currentV[k - 1] > currentV[k + 1]);
                    var kPrev = right ? k - 1 : k + 1;

                    // start point
                    var yStart = currentV[kPrev];
                    var xStart = yStart + kPrev;

                    // mid point
                    var yMid = right ? yStart : yStart + 1;
                    var xMid = yMid + k;

                    // end point
                    var xEnd = xMid;
                    var yEnd = yMid;

                    // follow diagonal
                    while (xEnd < oldLength && yEnd < newLength && ItemsEqual(oldSequence, xEnd, newSequence, yEnd))
                    {
                        xEnd++;
                        yEnd++;
                    }

                    // save end point
                    currentV[k] = yEnd;
                    Debug.Assert(xEnd == yEnd + k);

                    // check for solution
                    if (xEnd >= oldLength && yEnd >= newLength)
                    {
                        reachedEnd = true;
                    }
                }

                stackOfVs.Push(currentV.UnderlyingArray);
                previousV = currentV;
            }

            return stackOfVs;
        }
    }
}
