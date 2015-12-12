// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            // matrix will be:

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
            // We'll then fill out the matrix to be:
            //
            //

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
                }

                return matrix[sourceLength + 1, targetLength + 1];
            }
            finally
            {
                ReleaseMatrix(matrix);
            }
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