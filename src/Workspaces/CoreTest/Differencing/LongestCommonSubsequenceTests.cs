// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public void InsertOnly1()
        {
            string str1 = "";
            string str2 = "ABCDE";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 1.0);
        }

        [Fact]
        public void InsertOnly2()
        {
            string str1 = "ABC";
            string str2 = "ABXYZC";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 1, 1 }, { 2, 5 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.5);
        }

        [Fact]
        public void DeleteOnly1()
        {
            string str1 = "ABC";
            string str2 = "";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 1.0);
        }

        [Fact]
        public void DeleteOnly2()
        {
            string str1 = "ABCDE";
            string str2 = "ADE";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 0, 0 }, { 3, 1 }, { 4, 2 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.4);
        }

        [Fact]
        public void Replace1()
        {
            string str1 = "ABC";
            string str2 = "XYZ";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 1.0);
        }

        [Fact]
        public void Replace2()
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

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), new Dictionary<int, int>() { { 1, 1 }, { 3, 2 }, { 4, 3 }, { 6, 4 } });

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(lcs.ComputeDistance(str1, str2), 0.429, 3);
        }
    }
}
