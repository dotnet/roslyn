// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Math;

namespace Roslyn.Utilities
{
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
    internal class EditDistance : IDisposable
    {
        private struct CacheResult
        {
            public readonly string CandidateText;
            public readonly bool IsCloseMatch;
            public readonly double MatchCost;

            public CacheResult(string candidate, bool isCloseMatch, double matchCost)
            {
                CandidateText = candidate;
                IsCloseMatch = isCloseMatch;
                MatchCost = matchCost;
            }
        }

        private string _source;
        private char[] _sourceLowerCaseCharacters;
        private readonly int _threshold;

        // Cache the result of the last call to IsCloseMatch.  We'll often be called with the same
        // value multiple times in a row, so we can avoid expensive computation by returning the
        // same value immediately.
        private CacheResult _lastIsCloseMatchResult;


        public EditDistance(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            _source = text;
            _sourceLowerCaseCharacters = ConvertToLowercaseArray(text);

            _threshold = GetThreshold(_source);
        }

        internal static int GetThreshold(string value)
        {
            return value.Length <= 4 ? 1 : 2;
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

        public static bool IsCloseMatch(string originalText, string candidateText)
        {
            double dummy;
            return IsCloseMatch(originalText, candidateText, out dummy);
        }

        /// <summary>
        /// Returns true if 'value1' and 'value2' are likely a misspelling of each other.
        /// Returns false otherwise.  If it is a likely misspelling a matchCost is provided
        /// to help rank the match.  Lower costs mean it was a better match.
        /// </summary>
        public static bool IsCloseMatch(string originalText, string candidateText, out double matchCost)
        {
            using (var editDistance = new EditDistance(originalText))
            {
                return editDistance.IsCloseMatch(candidateText, out matchCost);
            }
        }

        public static int GetEditDistance(string source, string target)
        {
            using (var editDistance = new EditDistance(source))
            {
                return editDistance.GetEditDistance(target);
            }
        }

        public static int GetEditDistance(char[] source, char[] target)
        {
            return GetEditDistance(new ArraySlice<char>(source), new ArraySlice<char>(target));
        }

        public int GetEditDistance(string target)
        {
            return GetEditDistance(target, useThreshold: false);
        }

        private int GetEditDistance(string target, bool useThreshold)
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
                    new ArraySlice<char>(targetLowerCaseCharacters, 0, target.Length));
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
            var width = matrix.GetLength(0);
            for (int i = 0; i < width - 1; i++)
            {
                matrix[i + 1, 1] = i;
            }

            var height = matrix.GetLength(1);
            for (int j = 0; j < height - 1; j++)
            {
                matrix[1, j + 1] = j;
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

        public static int GetEditDistance(ArraySlice<char> source, ArraySlice<char> target)
        {
            return GetEditDistance(source, target, useThreshold: false);
        }

        private static int GetEditDistance(ArraySlice<char> source, ArraySlice<char> target, bool useThreshold)
        {
            return source.Length <= target.Length
                ? GetEditDistanceWorker(source, target, useThreshold)
                : GetEditDistanceWorker(target, source, useThreshold);
        }

        private static int GetEditDistanceWorker(ArraySlice<char> source, ArraySlice<char> target, bool useThreshold)
        {
            // Note: sourceLength and targetLength values will mutate and represent the lengths 
            // of the portions of the arrays we want to compare.
            //
            // Also note: sourceLength will always be smaller or equal to targetLength.
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
            try
            {
                InitializeMaxValues(sourceLength, targetLength, matrix);

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

        private static void InitializeMaxValues(int sourceLength, int targetLength, int[,] matrix)
        {
            var max = sourceLength + targetLength + 1;
            matrix[0, 0] = max;
            for (int i = 0; i <= sourceLength; i++)
            {
                matrix[i + 1, 0] = max;
            }

            for (int j = 0; j <= targetLength; j++)
            {
                matrix[0, j + 1] = max;
            }
        }

        private static int GetValue(Dictionary<char, int> da, char c)
        {
            int value;
            return da.TryGetValue(c, out value) ? value : 0;
        }

        public bool IsCloseMatch(string candidateText)
        {
            double matchCost;
            return IsCloseMatch(candidateText, out matchCost);
        }

        public bool IsCloseMatch(string candidateText, out double matchCost)
        {
            if (_source.Length < 3)
            {
                // If we're comparing strings that are too short, we'll find 
                // far too many spurious hits.  Don't even bother in this case.
                matchCost = double.MaxValue;
                return false;
            }
            
            if (_lastIsCloseMatchResult.CandidateText == candidateText)
            {
                matchCost = _lastIsCloseMatchResult.MatchCost;
                return _lastIsCloseMatchResult.IsCloseMatch;
            }

            var candidateCharArray = ConvertToLowercaseArray(candidateText);
            try
            {
                var result = IsCloseMatchWorker(candidateText, out matchCost);
                _lastIsCloseMatchResult = new CacheResult(candidateText, result, matchCost);
                return result;
            }
            finally
            {
                ArrayPool<char>.ReleaseArray(candidateCharArray);
            }
        }

        private bool IsCloseMatchWorker(string candidateText, out double matchCost)
        {
            matchCost = double.MaxValue;

            // If the two strings differ by more characters than the cost threshold, then there's 
            // no point in even computing the edit distance as it would necessarily take at least
            // that many additions/deletions.
            if (Math.Abs(_source.Length - candidateText.Length) <= _threshold)
            {
                matchCost = GetEditDistance(candidateText, useThreshold: true);
            }

            if (matchCost > _threshold)
            {
                // it had a high cost.  However, the string the user typed was contained
                // in the string we're currently looking at.  That's enough to consider it
                // although we place it just at the threshold (i.e. it's worse than all
                // other matches).
                if (candidateText.IndexOf(_source, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchCost = _threshold;
                }
                else
                {
                    return false;
                }
            }

            Debug.Assert(matchCost <= _threshold);

            matchCost += Penalty(candidateText, this._source);
            return true;
        }

        private static double Penalty(string candidateText, string originalText)
        {
            int lengthDifference = Math.Abs(originalText.Length - candidateText.Length);
            if (lengthDifference != 0)
            {
                // For all items of the same edit cost, we penalize those that are 
                // much longer than the original text versus those that are only 
                // a little longer.
                //
                // Note: even with this penalty, all matches of cost 'X' will all still
                // cost less than matches of cost 'X + 1'.  i.e. the penalty is in the 
                // range [0, 1) and only serves to order matches of the same cost.
                //
                // Here's the relation of the first few values of length diff and penalty:
                // LengthDiff   -> Penalty
                // 1            -> .5
                // 2            -> .66
                // 3            -> .75
                // 4            -> .8
                // And so on and so forth.
                double penalty = 1.0 - (1.0 / (lengthDifference + 1));
                return penalty;
            }

            return 0;
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