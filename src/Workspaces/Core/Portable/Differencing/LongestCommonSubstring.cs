// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Differencing;

/// <summary>
/// Calculates longest common substring using Wagner algorithm.
/// </summary>
internal sealed class LongestCommonSubstring : LongestCommonSubsequence<string>
{
    private static readonly LongestCommonSubstring s_instance = new();

    private LongestCommonSubstring()
    {
    }

    protected override bool ItemsEqual(string oldSequence, int oldIndex, string newSequence, int newIndex)
        => oldSequence[oldIndex] == newSequence[newIndex];

    public static double ComputePrefixDistance(string oldValue, int oldLength, string newValue, int newLength)
        => s_instance.ComputeDistance(oldValue, oldLength, newValue, newLength);

    public static IEnumerable<SequenceEdit> GetEdits(string oldValue, string newValue)
        => s_instance.GetEdits(oldValue, oldValue.Length, newValue, newValue.Length);
}
