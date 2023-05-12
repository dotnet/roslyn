// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Differencing
{
    internal abstract class LongestCommonSubsequence
    {
        /// <summary>
        /// Limit the number of tokens used to compute distance between sequences of tokens so that 
        /// we always use the pooled buffers. The combined length of the two sequences being compared
        /// must be less than <see cref="VBuffer.PooledSegmentMaxDepthThreshold"/>.
        /// </summary>
        public const int MaxSequenceLengthForDistanceCalculation = VBuffer.PooledSegmentMaxDepthThreshold / 2;

        // Define the pool in a non-generic base class to allow sharing among instantiations.
        private static readonly ObjectPool<VBuffer> s_pool = new(() => new VBuffer());

        /// <summary>
        /// Underlying storage for <see cref="VArray"/>s allocated on <see cref="VStack"/>.
        /// </summary>
        /// <remarks>
        /// The LCS algorithm allocates <see cref="VArray"/>s of sizes (3, 2*1 + 1, ..., 2*D + 1), always in this order, 
        /// where D is at most the sum of lengths of the compared sequences.
        /// The arrays get pushed on a stack as they are built up, then all consumed in the reverse order (stack pop).
        /// 
        /// Since the exact length of each array in the above sequence is known we avoid allocating each individual array.
        /// Instead we allocate a large buffer serving as a a backing storage of a contiguous sequence of arrays 
        /// corresponding to stack depths <see cref="MinDepth"/> to <see cref="MaxDepth"/>.
        /// If more storage is needed we chain next large buffer to the previous one in a linked list.
        /// 
        /// We pool a few of these linked buffers on <see cref="VStack"/> to conserve allocations.
        /// </remarks>
        protected sealed class VBuffer
        {
            /// <summary>
            /// The max stack depth backed by the fist buffer.
            /// Size of the buffer for 100 is ~10K. 
            /// For 150 it'd be 91KB, which would be allocated on LOH.
            /// The buffers grow by factor of <see cref="GrowFactor"/>, so the next buffer will be allocated on LOH.
            /// </summary>
            public const int FirstSegmentMaxDepth = 100;

            // 3 + Sum { d = 1..maxDepth : 2*d+1 } = (maxDepth + 1)^2 + 2
            private const int FirstSegmentLength = (FirstSegmentMaxDepth + 1) * (FirstSegmentMaxDepth + 1) + 2;

            // Segment     Segment        Total buffer
            // MaxDepth    length            length
            // ---------------------------------------
            // 100           10,204           10,204
            // 150           12,600           22,804
            // 225           28,275           51,079
            // 338           63,845          114,924
            // 507          143,143          258,067
            // 761          322,580          580,647
            // 1142         725,805        1,306,452
            // 1713       1,631,347        2,937,799  <-- last pooled segment
            // 2570       3,672,245        6,610,044
            // 3855       8,258,695       14,868,739
            internal const double GrowFactor = 1.5;

            /// <summary>
            /// Do not expand pooled buffers to more than ~12 MB total size (sum of all linked segment sizes).
            /// This threshold is achieved when <see cref="MaxDepth"/> is greater than <see cref="PooledSegmentMaxDepthThreshold"/> = sqrt(size_limit / sizeof(int)).
            /// </summary>
            internal const int PooledSegmentMaxDepthThreshold = 1800;

            public VBuffer Previous { get; private set; }
            public VBuffer Next { get; private set; }

            public readonly int MinDepth;
            public readonly int MaxDepth;

            private readonly int[] _array;

            public VBuffer()
            {
                _array = new int[FirstSegmentLength];
                MaxDepth = FirstSegmentMaxDepth;
            }

            public VBuffer(VBuffer previous)
            {
                Debug.Assert(previous != null);

                var minDepth = previous.MaxDepth + 1;
                var maxDepth = (int)(previous.MaxDepth * GrowFactor);

                Debug.Assert(minDepth > 0);
                Debug.Assert(minDepth <= maxDepth);

                Previous = previous;
                _array = new int[GetNextSegmentLength(minDepth - 1, maxDepth)];
                MinDepth = minDepth;
                MaxDepth = maxDepth;

                previous.Next = this;
            }

            public VArray GetVArray(int depth)
            {
                var length = GetVArrayLength(depth);
                var start = GetVArrayStart(depth) - GetVArrayStart(MinDepth);

                Debug.Assert(start >= 0);
                Debug.Assert(start + length <= _array.Length);
                return new VArray(_array, start, length);
            }

            public bool IsTooLargeToPool
                => MaxDepth > PooledSegmentMaxDepthThreshold;

            private static int GetVArrayLength(int depth)
                => 2 * Math.Max(depth, 1) + 1;

            // 3 + Sum { d = 1..depth-1 : 2*d+1 } = depth^2 + 2
            private static int GetVArrayStart(int depth)
                => (depth == 0) ? 0 : depth * depth + 2;

            // Sum { d = previousChunkDepth..maxDepth : 2*d+1 } = (maxDepth + 1)^2 - precedingBufferMaxDepth^2
            private static int GetNextSegmentLength(int precedingBufferMaxDepth, int maxDepth)
                => (maxDepth + 1) * (maxDepth + 1) - precedingBufferMaxDepth * precedingBufferMaxDepth;

            public void Unlink()
            {
                Previous.Next = null;
                Previous = null;
            }
        }

        protected struct VStack
        {
            private readonly ObjectPool<VBuffer> _bufferPool;
            private readonly VBuffer _firstBuffer;

            private VBuffer _currentBuffer;
            private int _depth;

            public VStack(ObjectPool<VBuffer> bufferPool)
            {
                _bufferPool = bufferPool;
                _currentBuffer = _firstBuffer = bufferPool.Allocate();
                _depth = 0;
            }

            public VArray Push()
            {
                var depth = _depth++;
                if (depth > _currentBuffer.MaxDepth)
                {
                    // If the buffer is not big enough add another segment to the linked list (the constructor takes care of the linking).
                    // Note that the segments are not pooled on their own. The whole linked list of buffers is pooled.
                    _currentBuffer = _currentBuffer.Next ?? new VBuffer(_currentBuffer);
                }

                return _currentBuffer.GetVArray(depth);
            }

            public IEnumerable<(VArray Array, int Depth)> ConsumeArrays()
            {
                var buffer = _currentBuffer;
                for (var depth = _depth - 1; depth >= 0; depth--)
                {
                    if (depth < buffer.MinDepth)
                    {
                        var previousBuffer = buffer.Previous;

                        // Trim large buffers from the linked list before we return the whole list back into the pool.
                        if (buffer.IsTooLargeToPool)
                        {
                            buffer.Unlink();
                        }

                        buffer = previousBuffer;
                    }

                    yield return (buffer.GetVArray(depth), depth);
                }

                _bufferPool.Free(_firstBuffer);
                _currentBuffer = null;
            }
        }

        // VArray struct enables array indexing in range [-d...d].
        protected readonly struct VArray
        {
            private readonly int[] _buffer;
            private readonly int _start;
            private readonly int _length;

            public VArray(int[] buffer, int start, int length)
            {
                _buffer = buffer;
                _start = start;
                _length = length;
            }

            public void InitializeFrom(VArray other)
            {
                int dstCopyStart, srcCopyStart, copyLength;

                var copyDelta = Offset - other.Offset;
                if (copyDelta >= 0)
                {
                    srcCopyStart = 0;
                    dstCopyStart = copyDelta;
                    copyLength = other._length;
                }
                else
                {
                    srcCopyStart = -copyDelta;
                    dstCopyStart = 0;
                    copyLength = _length;
                }

                // since we might be reusing previously used arrays, we need to clear slots that are not copied over from the other array:
                Array.Clear(_buffer, _start, dstCopyStart);
                Array.Copy(other._buffer, other._start + srcCopyStart, _buffer, _start + dstCopyStart, copyLength);
                Array.Clear(_buffer, _start + dstCopyStart + copyLength, _length - dstCopyStart - copyLength);
            }

            internal void Initialize()
                => Array.Clear(_buffer, _start, _length);

            public int this[int index]
            {
                get => _buffer[_start + index + Offset];
                set => _buffer[_start + index + Offset] = value;
            }

            private int Offset => _length / 2;
        }

        protected static VStack CreateStack()
            => new(s_pool);
    }

    /// <summary>
    /// Calculates Longest Common Subsequence.
    /// </summary>
    internal abstract class LongestCommonSubsequence<TSequence> : LongestCommonSubsequence
    {
        protected abstract bool ItemsEqual(TSequence oldSequence, int oldIndex, TSequence newSequence, int newIndex);

        // TODO: Consolidate return types between GetMatchingPairs and GetEdit to avoid duplicated code (https://github.com/dotnet/roslyn/issues/16864)
        protected IEnumerable<KeyValuePair<int, int>> GetMatchingPairs(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            var stack = ComputeEditPaths(oldSequence, oldLength, newSequence, newLength);

            var x = oldLength;
            var y = newLength;

            var varrays = stack.ConsumeArrays().GetEnumerator();

            while (x > 0 || y > 0)
            {
                var hasNext = varrays.MoveNext();
                Debug.Assert(hasNext);

                var (currentV, d) = varrays.Current;
                var k = x - y;

                // "snake" == single delete or insert followed by 0 or more diagonals
                // snake end point is in V
                var yEnd = currentV[k];
                var xEnd = yEnd + k;

                // does the snake first go down (insert) or right(delete)?
                var right = k == d || (k != -d && currentV[k - 1] > currentV[k + 1]);
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

            // make sure we finish the enumeration as it returns the allocated buffers to the pool
            while (varrays.MoveNext())
            {
            }
        }

        protected IEnumerable<SequenceEdit> GetEdits(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            var stack = ComputeEditPaths(oldSequence, oldLength, newSequence, newLength);

            var x = oldLength;
            var y = newLength;

            var varrays = stack.ConsumeArrays().GetEnumerator();

            while (x > 0 || y > 0)
            {
                var hasNext = varrays.MoveNext();
                Debug.Assert(hasNext);

                var (currentV, d) = varrays.Current;
                var k = x - y;

                // "snake" == single delete or insert followed by 0 or more diagonals
                // snake end point is in V
                var yEnd = currentV[k];
                var xEnd = yEnd + k;

                // does the snake first go down (insert) or right(delete)?
                var right = k == d || (k != -d && currentV[k - 1] > currentV[k + 1]);
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
            return 1.0 - lcsLength / (double)max;
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
        private VStack ComputeEditPaths(TSequence oldSequence, int oldLength, TSequence newSequence, int newLength)
        {
            var reachedEnd = false;
            VArray currentV = default;

            var stack = CreateStack();

            for (var d = 0; d <= oldLength + newLength && !reachedEnd; d++)
            {
                if (d == 0)
                {
                    // the first "snake" to start at (-1, 0)
                    currentV = stack.Push();
                    currentV.Initialize();
                }
                else
                {
                    // V is in range [-d...d] => use d to offset the k-based array indices to non-negative values
                    var previousV = currentV;
                    currentV = stack.Push();
                    currentV.InitializeFrom(previousV);
                }

                for (var k = -d; k <= d; k += 2)
                {
                    // down or right? 
                    var right = k == d || (k != -d && currentV[k - 1] > currentV[k + 1]);
                    var kPrev = right ? k - 1 : k + 1;

                    // start point
                    var yStart = currentV[kPrev];

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
            }

            return stack;
        }
    }
}
