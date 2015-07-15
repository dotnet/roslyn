// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
    internal static class EditDistance
    {
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
            int[] rowBefore2 = new int[rowWidth];

            // This stores the row before the current one
            int[] rowBefore1 = new int[rowWidth];

            // This stores the current row
            int[] rowCurrent = new int[rowWidth];

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

        // =============================================================================
        // Get the length of the longest common subsequence of the two given strings
        // =============================================================================
        public static int GetLongestCommonSubsequenceLength(string oldString, string newString)
        {
            // Expand the counts by one to include the empty string
            int rowWidth = oldString.Length + 1;
            int columnHeight = newString.Length + 1;

            // This stores the row before the current one
            int[] rowBefore1 = new int[rowWidth];

            // This stores the current row
            int[] rowCurrent = new int[rowWidth];

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
                        // Both chars are the same, so increate the length
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
    }
}
