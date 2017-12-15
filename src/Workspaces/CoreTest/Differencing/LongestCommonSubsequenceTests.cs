﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Differencing.UnitTests
{
    public class LongestCommonSubsequenceTests
    {
        LongestCommonSubsequenceString lcs = new LongestCommonSubsequenceString();

        private class LongestCommonSubsequenceString : LongestCommonSubsequence<string>
        {
            protected override bool ItemsEqual(string oldSequence, int oldIndex, string newSequence, int newIndex)
            {
                return oldSequence[oldIndex] == newSequence[newIndex];
            }

            public IEnumerable<KeyValuePair<int, int>> GetMatchingPairs(string oldSequence, string newSequence)
            {
                return GetMatchingPairs(oldSequence, oldSequence.Length, newSequence, newSequence.Length);
            }

            public IEnumerable<SequenceEdit> GetEdits(string oldSequence, string newSequence)
            {
                return GetEdits(oldSequence, oldSequence.Length, newSequence, newSequence.Length);
            }

            public double ComputeDistance(string oldSequence, string newSequence)
            {
                return ComputeDistance(oldSequence, oldSequence.Length, newSequence, newSequence.Length);
            }
        }

        private void VerifyMatchingPairs(IEnumerable<KeyValuePair<int, int>> actualPairs, Dictionary<int, int> expectedPairs)
        {
            int actPairsCount = 0;
            foreach (KeyValuePair<int, int> actPair in actualPairs)
            {
                Assert.True(expectedPairs.TryGetValue(actPair.Key, out int expValue));
                Assert.Equal(actPair.Value, expValue);
                actPairsCount++;
            }
            Assert.Equal(actPairsCount, expectedPairs.Count);
        }

        private void VerifyEdits(string oldStr, string newStr, IEnumerable<SequenceEdit> edits)
        {
            char[] oldChars = oldStr.ToCharArray();
            char[] newChars = new char[newStr.Length];

            foreach (SequenceEdit edit in edits)
            {
                Assert.True(edit.Kind == EditKind.Delete || edit.Kind == EditKind.Insert || edit.Kind == EditKind.Update);
                switch (edit.Kind)
                {
                    case EditKind.Delete:
                        Assert.True(edit.OldIndex < oldStr.Length);
                        oldChars[edit.OldIndex] = '\0';
                        break;

                    case EditKind.Insert:
                        Assert.True(edit.NewIndex < newStr.Length);
                        newChars[edit.NewIndex] = newStr[edit.NewIndex];
                        break;

                    case EditKind.Update:
                        Assert.True(edit.OldIndex < oldStr.Length);
                        Assert.True(edit.NewIndex < newStr.Length);
                        newChars[edit.NewIndex] = oldStr[edit.OldIndex];
                        oldChars[edit.OldIndex] = '\0';
                        break;
                }
            }

            string editedStr = new String(newChars);
            Assert.Equal(editedStr, newStr);

            Array.ForEach(oldChars, (c) => { Assert.Equal('\0', c); });
        }

        [Fact]
        public void EmptyStrings()
        {
            string str1 = "";
            string str2 = "";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>(){ });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.0);
        }

        [Fact]
        public void InsertToEmpty()
        {
            string str1 = "";
            string str2 = "ABC";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 1.0);
        }


        [Fact]
        public void InsertAtBeginning()
        {
            string str1 = "ABC";
            string str2 = "XYZABC";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 3 }, { 1, 4 }, { 2, 5 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.5);
        }

        [Fact]
        public void InsertAtEnd()
        {
            string str1 = "ABC";
            string str2 = "ABCXYZ";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 1, 1 }, { 2, 2 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.5);
        }

        [Fact]
        public void InsertInMidlle()
        {
            string str1 = "ABC";
            string str2 = "ABXYC";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 1, 1 }, { 2, 4 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.4);
        }

        [Fact]
        public void DeleteToEmpty()
        {
            string str1 = "ABC";
            string str2 = "";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 1.0);
        }

        [Fact]
        public void DeleteAtBeginning()
        {
            string str1 = "ABCD";
            string str2 = "C";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 2, 0 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.75);
        }

        [Fact]
        public void DeleteAtEnd()
        {
            string str1 = "ABCD";
            string str2 = "AB";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 1, 1 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.5);
        }

        [Fact]
        public void DeleteInMiddle()
        {
            string str1 = "ABCDE";
            string str2 = "ADE";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 3, 1 }, { 4, 2 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.4);
        }

        [Fact]
        public void ReplaceAll()
        {
            string str1 = "ABC";
            string str2 = "XYZ";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 1.0);
        }

        [Fact]
        public void ReplaceAtBeginning()
        {
            string str1 = "ABCD";
            string str2 = "XYD";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 3, 2 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.75);
        }

        [Fact]
        public void ReplaceAtEnd()
        {
            string str1 = "ABCD";
            string str2 = "ABXYZ";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 1, 1 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.6);
        }

        [Fact]
        public void ReplaceInMiddle()
        {
            string str1 = "ABCDE";
            string str2 = "AXDE";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 3, 2 }, { 4, 3 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.4);
        }

        [Fact]
        public void Combination1()
        {
            string str1 = "ABBCDEFIJ";
            string str2 = "AABDEEGH";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 1, 2 }, { 4, 3 }, { 5, 4 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.556, 3);
        }

        [Fact]
        public void Combination2()
        {
            string str1 = "AAABBCCDDD";
            string str2 = "ABXCD";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 3, 1 }, { 5, 3 }, { 7, 4 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.6);
        }

        [Fact]
        public void Combination3()
        {
            string str1 = "ABCABBA";
            string str2 = "CBABAC";

            // 2 possible matches:
            // { { 1, 1 }, { 3, 2 }, { 4, 3 }, { 6, 4 } }
            // { { 2, 0 }, { 3, 2 }, { 4, 3 }, { 6, 4 } }
            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 1, 1 }, { 3, 2 }, { 4, 3 }, { 6, 4 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.429, 3);
        }

        [Fact]
        public void Reorder1()
        {
            string str1 = "AB";
            string str2 = "BA";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 1 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.5);
        }

        [Fact]
        public void LongString()
        {
            var s = "A";

            var x9 = new string('x', 9);
            var x10 = new string('x', 10);
            var x99 = new string('x', 99);
            var x100 = new string('x', 100);
            var x1000 = new string('x', 1000);

            var y1000 = new string('y', 1000);

            var sx9 = s + x9;
            var sx99 = s + x99;
            var sx1000 = s + new string('x', 1000);
            var sx100000000 = s + new string('x', 100000000);
            
            Assert.Equal(0.900, lcs.ComputeDistance(s, sx9), precision: 3);
            Assert.Equal(0.990, lcs.ComputeDistance(s, sx99), precision: 3);
            Assert.Equal(1.000, lcs.ComputeDistance(s, sx1000), precision: 3);
            Assert.Equal(1.000, lcs.ComputeDistance(s, sx100000000), precision: 3);

            Assert.Equal(0.900, lcs.ComputeDistance(sx9, s), precision: 3);
            Assert.Equal(0.990, lcs.ComputeDistance(sx99, s), precision: 3);
            Assert.Equal(1.000, lcs.ComputeDistance(sx1000, s), precision: 3);
            Assert.Equal(1.000, lcs.ComputeDistance(sx100000000, s), precision: 3);

            Assert.Equal(1.000, lcs.ComputeDistance(x10 + y1000, x10), precision: 3);
            Assert.Equal(0.5, lcs.ComputeDistance(x1000 + y1000, x1000), precision: 3);
        }
    }
}
