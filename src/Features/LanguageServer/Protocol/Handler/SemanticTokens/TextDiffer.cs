// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// This class implements the linear space variation of the difference algorithm described in
    /// "An O(ND) Difference Algorithm and its Variations" by Eugene W. Myers.
    /// </summary>
    /// <remarks>
    /// NOTE: This class is adapted from Razor. For the original see
    /// https://github.com/dotnet/razor-tooling/blob/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/TextDiffer.cs.
    /// We use Razor's implementation over Roslyn's (see <see cref="LongestCommonSubsequence"/>)
    /// since Razor's version of the algorithm performs better for semantic tokens. We may want to
    /// consider unifying the two implementations in the future.
    /// </remarks>
    internal abstract class TextDiffer
    {
        protected abstract bool ContentEquals(
            IReadOnlyList<int> oldArray,
            int oldIndex,
            IReadOnlyList<int> newArray,
            int newIndex);

        protected IReadOnlyList<DiffEdit> ComputeDiff(ArraySegment<int> oldArray, ArraySegment<int> newArray)
        {
            using var _0 = ArrayBuilder<DiffEdit>.GetInstance(out var edits);

            // Initialize the vectors to use for forward and reverse searches.
            var max = newArray.Count + oldArray.Count;
            var capacity = (2 * max) + 1;
            using var _1 = ArrayBuilder<int>.GetInstance(capacity, fillWithValue: 0, out var vf);
            using var _2 = ArrayBuilder<int>.GetInstance(capacity, fillWithValue: 0, out var vr);

            ComputeDiffRecursive(edits, 0, oldArray.Count, 0, newArray.Count, vf, vr, oldArray, newArray);

            return edits.ToArray();
        }

        private void ComputeDiffRecursive(
            ArrayBuilder<DiffEdit> edits,
            int lowA,
            int highA,
            int lowB,
            int highB,
            ArrayBuilder<int> vf,
            ArrayBuilder<int> vr,
            ArraySegment<int> oldArray,
            ArraySegment<int> newArray)
        {
            while (lowA < highA && lowB < highB && ContentEquals(oldArray, lowA, newArray, lowB))
            {
                // Skip equal text at the start.
                lowA++;
                lowB++;
            }

            while (lowA < highA && lowB < highB && ContentEquals(oldArray, highA - 1, newArray, highB - 1))
            {
                // Skip equal text at the end.
                highA--;
                highB--;
            }

            if (lowA == highA)
            {
                // Base case 1: We've reached the end of original text. Insert whatever is remaining in the new text.
                while (lowB < highB)
                {
                    edits.Add(DiffEdit.Insert(lowA, lowB));
                    lowB++;
                }
            }
            else if (lowB == highB)
            {
                // Base case 2: We've reached the end of new text. Delete whatever is remaining in the original text.
                while (lowA < highA)
                {
                    edits.Add(DiffEdit.Delete(lowA));
                    lowA++;
                }
            }
            else
            {
                // Find the midpoint of the optimal path.
                var (middleX, middleY) = FindMiddleSnake(lowA, highA, lowB, highB, vf, vr, oldArray, newArray);

                // Recursively find the midpoint of the left half.
                ComputeDiffRecursive(edits, lowA, middleX, lowB, middleY, vf, vr, oldArray, newArray);

                // Recursively find the midpoint of the right half.
                ComputeDiffRecursive(edits, middleX, highA, middleY, highB, vf, vr, oldArray, newArray);
            }
        }

        private (int, int) FindMiddleSnake(
            int lowA,
            int highA,
            int lowB,
            int highB,
            ArrayBuilder<int> vf,
            ArrayBuilder<int> vr,
            ArraySegment<int> oldArray,
            ArraySegment<int> newArray)
        {
            var n = highA - lowA;
            var m = highB - lowB;
            var delta = n - m;
            var deltaIsEven = delta % 2 == 0;

            var max = n + m;

            // Compute the k-line to start the forward and reverse searches.
            var forwardK = lowA - lowB;
            var reverseK = highA - highB;

            // The paper uses negative indexes but we can't do that here. So we'll add an offset.
            var forwardOffset = max - forwardK;
            var reverseOffset = max - reverseK;

            // Initialize the vector
            vf[forwardOffset + forwardK + 1] = lowA;
            vr[reverseOffset + reverseK - 1] = highA;

            var maxD = Math.Ceiling((double)(m + n) / 2);
            for (var d = 0; d <= maxD; d++) // For D ← 0 to ceil((M + N)/2) Do
            {
                // Run the algorithm in forward direction.
                for (var k = forwardK - d; k <= forwardK + d; k += 2) // For k ← −D to D in steps of 2 Do
                {
                    // Find the end of the furthest reaching forward D-path in diagonal k.
                    int x;
                    if (k == forwardK - d ||
                        (k != forwardK + d && vf[forwardOffset + k - 1] < vf[forwardOffset + k + 1]))
                    {
                        // Down
                        x = vf[forwardOffset + k + 1];
                    }
                    else
                    {
                        // Right
                        x = vf[forwardOffset + k - 1] + 1;
                    }

                    var y = x - k;

                    // Traverse diagonal if possible.
                    while (x < highA && y < highB && ContentEquals(oldArray, x, newArray, y))
                    {
                        x++;
                        y++;
                    }

                    vf[forwardOffset + k] = x;
                    if (deltaIsEven)
                    {
                        // Can't have overlap here.
                    }
                    else if (k > reverseK - d && k < reverseK + d) // If ∆ is odd and k ∈ [∆ − (D − 1) , ∆ + (D − 1)] Then
                    {
                        if (vr[reverseOffset + k] <= vf[forwardOffset + k]) // If the path overlaps the furthest reaching reverse (D − 1)-path in diagonal k Then
                        {
                            // The last snake of the forward path is the middle snake.
                            x = vf[forwardOffset + k];
                            y = x - k;
                            return (x, y);
                        }
                    }
                }

                // Run the algorithm in reverse direction.
                for (var k = reverseK - d; k <= reverseK + d; k += 2) // For k ← −D to D in steps of 2 Do
                {
                    // Find the end of the furthest reaching reverse D-path in diagonal k+∆.
                    int x;
                    if (k == reverseK + d ||
                        (k != reverseK - d && vr[reverseOffset + k - 1] < vr[reverseOffset + k + 1] - 1))
                    {
                        // Up
                        x = vr[reverseOffset + k - 1];
                    }
                    else
                    {
                        // Left
                        x = vr[reverseOffset + k + 1] - 1;
                    }

                    var y = x - k;

                    // Traverse diagonal if possible.
                    while (x > lowA && y > lowB && ContentEquals(oldArray, x - 1, newArray, y - 1))
                    {
                        x--;
                        y--;
                    }

                    vr[reverseOffset + k] = x;
                    if (!deltaIsEven)
                    {
                        // Can't have overlap here.
                    }
                    else if (k >= forwardK - d && k <= forwardK + d) // If ∆ is even and k + ∆ ∈ [−D, D] Then
                    {
                        if (vr[reverseOffset + k] <= vf[forwardOffset + k]) // If the path overlaps the furthest reaching forward D-path in diagonal k+∆ Then
                        {
                            // The last snake of the reverse path is the middle snake.
                            x = vf[forwardOffset + k];
                            y = x - k;
                            return (x, y);
                        }
                    }
                }
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
