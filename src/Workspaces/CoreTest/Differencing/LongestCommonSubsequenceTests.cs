// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
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

        private void VerifyMatchingPairs(IEnumerable<KeyValuePair<int, int>> actualPairs, string expectedPairsStr)
        {
            var sb = new StringBuilder(expectedPairsStr.Length);
            foreach (var actPair in actualPairs)
            {
                sb.AppendFormat("[{0},{1}]", actPair.Key, actPair.Value);
            }
            var actualPairsStr = sb.ToString();
            Assert.Equal(expectedPairsStr, actualPairsStr);
        }

        private void VerifyEdits(string oldStr, string newStr, IEnumerable<SequenceEdit> edits)
        {
            var oldChars = oldStr.ToCharArray();
            var newChars = new char[newStr.Length];

            foreach (var edit in edits)
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

            var editedStr = new String(newChars);
            Assert.Equal(editedStr, newStr);

            Array.ForEach(oldChars, (c) => { Assert.Equal('\0', c); });
        }

        [Fact]
        public void EmptyStrings()
        {
            var str1 = "";
            var str2 = "";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.0, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void InsertToEmpty()
        {
            var str1 = "";
            var str2 = "ABC";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(1.0, lcs.ComputeDistance(str1, str2));
        }


        [Fact]
        public void InsertAtBeginning()
        {
            var str1 = "ABC";
            var str2 = "XYZABC";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[2,5][1,4][0,3]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.5, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void InsertAtEnd()
        {
            var str1 = "ABC";
            var str2 = "ABCXYZ";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[2,2][1,1][0,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.5, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void InsertInMidlle()
        {
            var str1 = "ABC";
            var str2 = "ABXYC";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[2,4][1,1][0,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.4, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void DeleteToEmpty()
        {
            var str1 = "ABC";
            var str2 = "";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(1.0, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void DeleteAtBeginning()
        {
            var str1 = "ABCD";
            var str2 = "C";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[2,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.75, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void DeleteAtEnd()
        {
            var str1 = "ABCD";
            var str2 = "AB";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[1,1][0,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.5, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void DeleteInMiddle()
        {
            var str1 = "ABCDE";
            var str2 = "ADE";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[4,2][3,1][0,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.4, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void ReplaceAll()
        {
            var str1 = "ABC";
            var str2 = "XYZ";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(1.0, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void ReplaceAtBeginning()
        {
            var str1 = "ABCD";
            var str2 = "XYD";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[3,2]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.75, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void ReplaceAtEnd()
        {
            var str1 = "ABCD";
            var str2 = "ABXYZ";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[1,1][0,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.6, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void ReplaceInMiddle()
        {
            var str1 = "ABCDE";
            var str2 = "AXDE";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[4,3][3,2][0,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.4, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void Combination1()
        {
            var str1 = "ABBCDEFIJ";
            var str2 = "AABDEEGH";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[5,4][4,3][1,2][0,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.556, lcs.ComputeDistance(str1, str2), precision: 3);
        }

        [Fact]
        public void Combination2()
        {
            var str1 = "AAABBCCDDD";
            var str2 = "ABXCD";

            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[7,4][5,3][3,1][0,0]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.6, lcs.ComputeDistance(str1, str2));
        }

        [Fact]
        public void Combination3()
        {
            var str1 = "ABCABBA";
            var str2 = "CBABAC";

            // 2 possible matches:
            // "[6,4][4,3][3,2][1,1]" <- this one is backwards compatible
            // "[6,4][4,3][3,2][2,0]"
            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[6,4][4,3][3,2][1,1]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.429, lcs.ComputeDistance(str1, str2), precision: 3);
        }

        [Fact]
        public void Reorder1()
        {
            var str1 = "AB";
            var str2 = "BA";

            // 2 possible matches:
            // "[0,1]" <- this one is backwards compatible
            // "[1,0]"
            VerifyMatchingPairs(lcs.GetMatchingPairs(str1, str2), "[0,1]");

            VerifyEdits(str1, str2, lcs.GetEdits(str1, str2));

            Assert.Equal(0.5, lcs.ComputeDistance(str1, str2));
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
