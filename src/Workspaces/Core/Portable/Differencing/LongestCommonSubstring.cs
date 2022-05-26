﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Differencing
{
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

        public static double ComputeDistance(string oldValue, string newValue)
            => s_instance.ComputeDistance(oldValue, oldValue.Length, newValue, newValue.Length);

        public static IEnumerable<SequenceEdit> GetEdits(string oldValue, string newValue)
            => s_instance.GetEdits(oldValue, oldValue.Length, newValue, newValue.Length);
    }
}
