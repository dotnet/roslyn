// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static System.Math;

namespace Roslyn.Utilities
{
    // NOTE: do not use this class directly.  It is intended for use only by the spell checker.
    //
    // Implementation of the Damerau-Levenshtein edit distance algorithm from:
    // An Extension of the String-to-String Correction Problem:
    // Published in Journal of the ACM (JACM)
    // Volume 22 Issue 2, April 1975.
    //
    // Important, unlike many edit distance algorithms out there, this one implements a true metric
    // that satisfies the triangle inequality.  (Unlike the "Optimal String Alignment" or "Restricted
    // string edit distance" solutions which do not).  This means this edit distance can be used in
    // other domains that require the triangle inequality.
    //
    // Specifically, this implementation satisfies the following inequality: D(x, y) + D(y, z) >= D(x, z)
    // (where D is the edit distance).
    //
    // NOTE: do not use this class directly.  It is intended for use only by the spell checker.
    internal class EditDistance : IDisposable
    {
        // Our edit distance algorithm makes use of an 'infinite' value.  A value so high that it 
        // could never participate in an edit distance (and effectively means the path through it
        // is dead).
        //
        // We do *not* represent this with "int.MaxValue" due to the presence of certain addition
        // operations in the edit distance algorithm. These additions could cause int.MaxValue
        // to roll over to a very negative value (which would then look like the lowest cost
        // path).
        //
        // So we pick a value that is both effectively larger than any possible edit distance,
        // and also has no chance of overflowing.
        private const int Infinity = int.MaxValue >> 2;

        private string _source;
        private char[] _sourceLowerCaseCharacters;

        public EditDistance(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            _source = text;
            _sourceLowerCaseCharacters = ConvertToLowercaseArray(text);
        }

        private static char[] ConvertToLowercaseArray(string text)
        {
            var array = ArrayPool<char>.GetArray(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                array[i] = char.ToLower(text[i]);
            }

            return array;
        }

        public void Dispose()
        {
            ArrayPool<char>.ReleaseArray(this._sourceLowerCaseCharacters);
            _source = null;
            _sourceLowerCaseCharacters = null;
        }

        public static int GetEditDistance(string source, string target, int threshold = int.MaxValue)
        {
            using (var editDistance = new EditDistance(source))
            {
                return editDistance.GetEditDistance(target, threshold);
            }
        }

        public static int GetEditDistance(char[] source, char[] target, int threshold = int.MaxValue)
        {
            return GetEditDistance(new ArraySlice<char>(source), new ArraySlice<char>(target), threshold);
        }

        public int GetEditDistance(string target, int threshold = int.MaxValue)
        {
            if (this._sourceLowerCaseCharacters == null)
            {
                throw new ObjectDisposedException(nameof(EditDistance));
            }

            var targetLowerCaseCharacters = ConvertToLowercaseArray(target);
            try
            {
                return GetEditDistance(
                    new ArraySlice<char>(_sourceLowerCaseCharacters, 0, _source.Length),
                    new ArraySlice<char>(targetLowerCaseCharacters, 0, target.Length),
                    threshold);
            }
            finally
            {
                ArrayPool<char>.ReleaseArray(targetLowerCaseCharacters);
            }
        }

        private const int MaxMatrixPoolDimension = 64;
        private static readonly ObjectPool<int[,]> s_matrixPool = new ObjectPool<int[,]>(() => InitializeMatrix(new int[64, 64]));

        private static int[,] GetMatrix(int width, int height)
        {
            if (width > MaxMatrixPoolDimension || height > MaxMatrixPoolDimension)
            {
                return InitializeMatrix(new int[width, height]);
            }

            return s_matrixPool.Allocate();
        }

        private static int[,] InitializeMatrix(int[,] matrix)
        {
            // All matrices share the following in common:
            //
            // ------------------
            // |∞ ∞ ∞ ∞ ∞ ∞ ∞ ∞ ∞
            // |∞ 0 1 2 3 4 5 6 7
            // |∞ 1
            // |∞ 2
            // |∞ 3
            // |∞ 4
            // |∞ 5
            // |∞ 6
            // |∞ 7
            //
            // So we initialize this once when the matrix is created.  For pooled arrays we only
            // have to do this once, and it will retain this layout for all future computations.


            var width = matrix.GetLength(0);
            var height = matrix.GetLength(1);

            for (int i = 0; i < width; i++)
            {
                matrix[i, 0] = Infinity;

                if (i < width - 1)
                {
                    matrix[i + 1, 1] = i;
                }
            }

            for (int j = 0; j < height; j++)
            {
                matrix[0, j] = Infinity;

                if (j < height - 1)
                {
                    matrix[1, j + 1] = j;
                }
            }

            return matrix;
        }

        private static void ReleaseMatrix(int[,] matrix)
        {
            if (matrix.GetLength(0) <= MaxMatrixPoolDimension && matrix.GetLength(1) <= MaxMatrixPoolDimension)
            {
                s_matrixPool.Free(matrix);
            }
        }

        public static int GetEditDistance(ArraySlice<char> source, ArraySlice<char> target, int threshold  = int.MaxValue)
        {
            return source.Length <= target.Length
                ? GetEditDistanceWorker(source, target, threshold)
                : GetEditDistanceWorker(target, source, threshold);
        }

        private static int GetEditDistanceWorker(ArraySlice<char> source, ArraySlice<char> target, int threshold)
        {
            // Note: sourceLength will always be smaller or equal to targetLength.
            //
            // Also Note: sourceLength and targetLength values will mutate and represent the lengths 
            // of the portions of the arrays we want to compare.  However, even after mutation, hte
            // invariant htat sourceLength is <= targetLength will remain.
            Debug.Assert(source.Length <= target.Length);

            // First:
            // Determine the common prefix/suffix portions of the strings.  We don't even need to 
            // consider them as they won't add anything to the edit cost.
            while (source.Length > 0 && source[source.Length - 1] == target[target.Length - 1])
            {
                source.SetLength(source.Length - 1);
                target.SetLength(target.Length - 1);
            }

            while (source.Length > 0 && source[0] == target[0])
            {
                source.MoveStartForward(amount: 1);
                target.MoveStartForward(amount: 1);
            }

            // 'sourceLength' and 'targetLength' are now the lengths of the substrings of our strings that we
            // want to compare. 'startIndex' is the starting point of the substrings in both array.
            //
            // If we've matched all of the 'source' string in the prefix and suffix of 'target'. then the edit
            // distance is just whatever operations we have to create the remaining target substring.
            //
            // Note: we don't have to check if targetLength is 0.  That's because targetLength being zero would
            // necessarily mean that sourceLength is 0.
            var sourceLength = source.Length;
            var targetLength = target.Length;
            if (sourceLength == 0)
            {
                return targetLength;
            }

            var matrix = GetMatrix(sourceLength + 2, targetLength + 2);

            // Say we want to find the edit distance between "sunday" and "saturday".  Our initial
            // matrix will be.

            //           s u n d a y
            //      ----------------
            //      |∞ ∞ ∞ ∞ ∞ ∞ ∞ ∞
            //      |∞ 0 1 2 3 4 5 6
            //    s |∞ 1 
            //    a |∞ 2
            //    t |∞ 3
            //    u |∞ 4
            //    r |∞ 5
            //    d |∞ 6
            //    a |∞ 7
            //    y |∞ 8
            //
            // Note that the matrix will always be square, or a rectangle that is taller htan it is 
            // longer.  Our 'source' is at the top, and our 'target' is on the left.  The edit distance
            // between any prefix of 'source' and any prefix of 'target' can then be found in 
            // the unfilled area of the matrix.  Specifically, if we have source.substring(0, m) and
            // target.substring(0, n), then the edit distance for them can be found at matrix position
            // (m+1, n+1).  This is why the 1'th row and 1'th column can be prefilled.  They represent
            // the cost to go from the empty target to the full source or the empty source to the full
            // target (respectively).  So, if we wanted to know the edit distance between "sun" and 
            // "sat", we'd look at (3+1, 3+1).  It then follows that our final edit distance between
            // the full source and target is in the lower right corner of this matrix.
            //
            // If we fill out the matrix fully we'll get:
            //          
            //           s u n d a y <-- source
            //      ----------------
            //      |∞ ∞ ∞ ∞ ∞ ∞ ∞ ∞
            //      |∞ 0 1 2 3 4 5 6
            //    s |∞ 1 0 1 2 3 4 5 
            //    a |∞ 2 1 1 2 3 3 4 
            //    t |∞ 3 2 2 2 3 4 4 
            //    u |∞ 4 3 2 3 3 4 5 
            //    r |∞ 5 4 3 3 4 4 5 
            //    d |∞ 6 5 4 4 3 4 5 
            //    a |∞ 7 6 5 5 4 3 4 
            //    y |∞ 8 7 6 6 5 4 3 <--
            //                     ^
            //                     |
            //
            // So in this case, the edit distance is 3.  Or, specifically, the edits:
            //
            //      Sunday -> Replace("n", "r") -> 
            //      Surday -> Insert("a") ->
            //      Saurday -> Insert("t") ->
            //      Saturday
            //
            //
            // Now: in the case where we want to know what the edit distance actually is (for example
            // when making a BKTree), we must fill out this entire array to get the true edit distance.
            //
            // However, in some cases we can do a bit better.  For example, if a client only wants to
            // the edit distance *when the edit distance will be less than some threshold* then we do
            // not need to examine the entire matrix.  We only want to examine until the point where
            // we realize that, no matter what, our final edit distance will be more than that threshold
            // (at which point we can return early).
            //
            // Some things are trivially easy to check.  First, the edit distance between two strings is at
            // *best* the difference of their lengths.  i.e. if i have "aa" and "aaaaa" then the edit
            // distance is 3 (the difference of 5 and 2).  If our threshold is less then 3 then there
            // is no way these two strings could match.  So we can leave early if we can tell it would
            // simply be impossible to get an edit distance within the specified threshold.
            //
            // Second, let's look at our matrix again:
            //
            //           s u n d a y
            //      ----------------
            //      |∞ ∞ ∞ ∞ ∞ ∞ ∞ ∞
            //      |∞ 0 1 2 3 4 5 6
            //    s |∞ 1 
            //    a |∞ 2
            //    t |∞ 3
            //    u |∞ 4
            //    r |∞ 5
            //    d |∞ 6
            //    a |∞ 7
            //    y |∞ 8           *
            //
            // We want to know what the value is at *, and we want to stop as early as possible if it
            // is greater than our threshold.
            //
            // Given the edit distance rules we observe edit distance at any point (i,j) in the matrix will
            // always be greater than or equal to the value in (i-1, j-1).  i.e. the edit distance of
            // any two strings is going to be *at best* equal to the edit distance of those two strings
            // without their final characters.  If their final characters are the same, they'll ahve the
            // same edit distance.  If they are different, the edit distance will be greater.  Given 
            // that we know the final edit distance is in the lower right, we can discover something 
            // useful in the matrix.
            //
            //           s u n d a y
            //      ----------------
            //      |∞ ∞ ∞ ∞ ∞ ∞ ∞ ∞
            //      |∞ 0 1 2 3 4 5 6
            //    s |∞ 1 
            //    a |∞ 2
            //    t |∞ 3 \
            //    u |∞ 4   \
            //    r |∞ 5     \
            //    d |∞ 6       \
            //    a |∞ 7         \
            //    y |∞ 8           *
            //
            // The slashes are the diagonal leading to the lower right.  The value in the lower right will
            // be strictly equal to or greater than any value on this diagonal.  Thus, if that value 
            // exceeds the threshold, we know we can stop immediately as the total edit distance must be
            // greater than the threshold.


            // The is the minimum number of edits we'd have to make.  i.e. if  'source' and 
            // 'target' are the same length, then we might not need to make any edits.  However,
            // if target has length 10 and source has length 7, then we're going to have to
            // make at least 3 edits no matter what.
            var minimumEditCount = targetLength - sourceLength;
            Debug.Assert(minimumEditCount >= 0);

            // If the number of edits we'd have to perform is greater than our threshold, then
            // there's no point in even continuing.
            if (minimumEditCount > threshold)
            {
                return int.MaxValue;
            }

            try
            {
                var characterToLastSeenIndex_inSource = new Dictionary<char, int>();
                for (int i = 1; i <= sourceLength; i++)
                {
                    var lastMatchIndex_inTarget = 0;
                    var sourceChar = source[i - 1];

                    for (int j = 1; j <= targetLength; j++)
                    {
                        var targetChar = target[j - 1];

                        var i1 = GetValue(characterToLastSeenIndex_inSource, targetChar);
                        var j1 = lastMatchIndex_inTarget;

                        var matched = sourceChar == targetChar;
                        if (matched)
                        {
                            lastMatchIndex_inTarget = j;
                        }

                        matrix[i + 1, j + 1] = Min(
                            matrix[i, j] + (matched ? 0 : 1),
                            matrix[i + 1, j] + 1,
                            matrix[i, j + 1] + 1,
                            matrix[i1, j1] + (i - i1 - 1) + 1 + (j - j1 - 1));
                    }

                    characterToLastSeenIndex_inSource[sourceChar] = i;

                    // Recall that minimumEditCount is simply the difference in length of our two
                    // strings.  So matrix[i+1,i+1] is the cost for the upper-left diagonal of the
                    // matrix.  matrix[i+1,i+1+minimumEditCount] is the cost for the lower right diagonal.
                    // Here we are simply getting the lowest cost edit of hese two substrings so far.
                    // If this lowest cost edit is greater than our threshold, then there is no need 
                    // to proceed.
                    if (matrix[i + 1, i + minimumEditCount + 1] > threshold)
                    {
                        return int.MaxValue;
                    }
                }

                return matrix[sourceLength + 1, targetLength + 1];
            }
            finally
            {
                ReleaseMatrix(matrix);
            }
        }

        private static string ToString(int[,] matrix, int width, int height)
        {
            var sb = new StringBuilder();
            for (var j = 0; j < height; j++)
            {
                for (var i = 0; i < width; i++)
                {
                    sb.Append(matrix[i + 2, j + 2] + " ");
                }
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        private static int GetValue(Dictionary<char, int> da, char c)
        {
            int value;
            return da.TryGetValue(c, out value) ? value : 0;
        }

        private static int Min(int v1, int v2, int v3, int v4)
        {
            var min = v1;
            if (v2 < min)
            {
                min = v2;
            }

            if (v3 < min)
            {
                min = v3;
            }

            if (v4 < min)
            {
                min = v4;
            }

            Debug.Assert(min >= 0);
            return min;
        }

        private static void SetValue(int[,] matrix, int i, int j, int val)
        {
            // Matrix is -1 based, so we add 1 to both i and j to make it
            // possible to index into the actual storage.
            matrix[i + 1, j + 1] = val;
        }
    }

    internal static class ArrayPool<T>
    {
        private const int MaxPooledArraySize = 256;

        // Keep around a few arrays of size 256 that we can use for operations without
        // causing lots of garbage to be created.  If we do compare items larger than
        // that, then we will just allocate and release those arrays on demand.
        private static ObjectPool<T[]> s_pool = new ObjectPool<T[]>(() => new T[MaxPooledArraySize]);

        public static T[] GetArray(int size)
        {
            if (size <= MaxPooledArraySize)
            {
                var array = s_pool.Allocate();
                Array.Clear(array, 0, array.Length);
                return array;
            }

            return new T[size];
        }

        public static void ReleaseArray(T[] array)
        {
            if (array.Length <= MaxPooledArraySize)
            {
                s_pool.Free(array);
            }
        }
    }
}