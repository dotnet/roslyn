// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
    internal static class EditDistance
    {
        private const int MaxPooledArraySize = 256;

        // Keep around a few arrays of size 256 that we can use for operations without
        // causing lots of garbage to be created.  If we do compare items larger than
        // that, then we will just allocate and release those arrays on demand.
        private static ObjectPool<int[]> s_pool = new ObjectPool<int[]>(() => new int[MaxPooledArraySize]);

        public static int[] GetArray(int size)
        {
            if (size <= MaxPooledArraySize)
            {
                var array = s_pool.Allocate();
                Array.Clear(array, 0, array.Length);
                return array;
            }

            return new int[size];
        }

        public static void ReleaseArray(int[] array)
        {
            if (array.Length <= MaxPooledArraySize)
            {
                s_pool.Free(array);
            }
        }

        public static int GetEditDistance(string oldString, string newString)
        {
            // Cost constants
            const int Copy = 0;
            const int Insert = 1;
            const int Delete = 1;
            const int Replace = 1;
            const int Twiddle = 1;

            // Expand the counts by one to include the empty string
            int rowWidth = oldString.Length + 1;
            int columnHeight = newString.Length + 1;

            // This stores data two rows before the current one
            int[] rowBefore2 = GetArray(rowWidth);

            // This stores the row before the current one
            int[] rowBefore1 = GetArray(rowWidth);

            // This stores the current row
            int[] rowCurrent = GetArray(rowWidth);

            try
            {
                // Initialize the first row, which represents deleting the entire string
                for (int column = 1; column < rowWidth; column++)
                {
                    rowCurrent[column] = rowCurrent[column - 1] + Delete;
                }

                for (int row = 1; row < columnHeight; row++)
                {
                    // Shift the row data upwards, rowBefore2 falls off and the memory is
                    // recycled for the current row
                    var temp = rowBefore2;
                    rowBefore2 = rowBefore1;
                    rowBefore1 = rowCurrent;
                    rowCurrent = temp;

                    // First element of the row represents inserting the new string
                    rowCurrent[0] = rowBefore1[0] + Insert;

                    for (int column = 1; column < rowWidth; column++)
                    {
                        // Copy = top left neighbor + cost_copy      if current chars are equal
                        //        infinite                           otherwise
                        int copyCost = (char.ToLower(oldString[column - 1]) == char.ToLower(newString[row - 1])) ?
                                            rowBefore1[column - 1] + Copy :
                                            int.MaxValue;

                        // Insert = top neighbor + cost_insert
                        int insertCost = rowBefore1[column] + Insert;

                        // Delete = left neighbor + cost_delete
                        int deleteCost = rowCurrent[column - 1] + Delete;

                        // Replace = top left neighbor + cost_replace
                        int replaceCost = rowBefore1[column - 1] + Replace;

                        // Twiddle = top left neighbor of the top left neighbor + cost_twiddle   if chars are swapped
                        //           infinite                                                    otherwise
                        int twiddleCost = (column > 1 && row > 1 &&
                                        char.ToLower(oldString[column - 1]) == char.ToLower(newString[row - 2]) &&
                                        char.ToLower(oldString[column - 2]) == char.ToLower(newString[row - 1])) ?
                                            rowBefore2[column - 2] + Twiddle :
                                            int.MaxValue;

                        // Store the smallest of the costs
                        rowCurrent[column] = Math.Min(Math.Min(Math.Min(Math.Min(copyCost, insertCost), deleteCost), replaceCost), twiddleCost);
                    }
                }

                // The edit distance is the last element in the current row
                return rowCurrent[rowWidth - 1];
            }
            finally
            {
                ReleaseArray(rowBefore2);
                ReleaseArray(rowBefore1);
                ReleaseArray(rowCurrent);
            }
        }

        // =============================================================================
        // Get the length of the longest common subsequence of the two given strings
        // =============================================================================
        public static int GetLongestCommonSubsequenceLength(string oldString, string newString)
        {
            // Expand the counts by one to include the empty string
            int rowWidth = oldString.Length + 1;
            int columnHeight = newString.Length + 1;

            // This stores the row before the current one
            int[] rowBefore1 = GetArray(rowWidth);

            // This stores the current row
            int[] rowCurrent = GetArray(rowWidth);
            try
            {
                for (int row = 1; row < columnHeight; row++)
                {
                    // Shift the row data upwards, rowBefore1 falls off and the memory is
                    // recycled for the current row
                    var temp = rowBefore1;
                    rowBefore1 = rowCurrent;
                    rowCurrent = temp;

                    // First element of the row is always 0
                    rowCurrent[0] = 0;

                    for (int column = 1; column < rowWidth; column++)
                    {
                        int currentLength = 0;

                        if (char.ToLower(oldString[column - 1]) == char.ToLower(newString[row - 1]))
                        {
                            // Both chars are the same, so increment the length
                            currentLength = rowBefore1[column - 1] + 1;
                        }
                        else
                        {
                            // The chars are not the same, so pick the maximum of the
                            // left and upper entries
                            currentLength = Math.Max(rowCurrent[column - 1], rowBefore1[column]);
                        }

                        rowCurrent[column] = currentLength;
                    }
                }

                // The LCS length is the last element in the current row
                return rowCurrent[rowWidth - 1];
            }
            finally
            {
                ReleaseArray(rowBefore1);
                ReleaseArray(rowCurrent);
            }
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
            return IsCloseMatch(originalText, candidateText, GetCloseMatchThreshold(originalText), out matchCost);
        }

        public static int GetCloseMatchThreshold(string originalText)
        {
            return Math.Min(4, originalText.Length / 2);
        }

        /// <summary>
        /// Returns true if 'value1' and 'value2' are likely a misspelling of each other.
        /// Returns false otherwise.  If it is a likely misspelling a matchCost is provided
        /// to help rank the match.  Lower costs mean it was a better match.
        /// </summary>
        public static bool IsCloseMatch(string originalText, string candidateText, int costThreshold, out double matchCost)
        {
            matchCost = EditDistance.GetEditDistance(originalText, candidateText);

            if (matchCost > costThreshold)
            {
                // it had a high cost.  However, the string the user typed was contained
                // in the string we're currently looking at.  That's enough to consider it
                // although we place it just at the threshold (i.e. it's worse than all
                // other matches).
                if (candidateText.IndexOf(originalText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchCost = costThreshold;
                }
            }

            if (matchCost > costThreshold)
            {
                return false;
            }

            matchCost += Penalty(candidateText, originalText);
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
                double penalty = 1.0 - (1.0 / lengthDifference);
                return penalty;
            }

            return 0;
        }
    }
}