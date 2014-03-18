// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Differencing
{
    /// <summary>
    /// Calculates longest common substring using Wagner algorithm.
    /// </summary>
    public sealed class LongestCommonSubstring : LongestCommonSubsequence<string>
    {
        private static readonly LongestCommonSubstring Instance = new LongestCommonSubstring();

        private LongestCommonSubstring()
        {
        }

        protected override bool ItemsEqual(string sequenceA, int indexA, string sequenceB, int indexB)
        {
            return sequenceA[indexA] == sequenceB[indexB];
        }

        public static double ComputeDistance(string s1, string s2)
        {
            return Instance.ComputeDistance(s1, s1.Length, s2, s2.Length);
        }
    }
}
