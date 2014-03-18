// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EditDistanceTests
    {
        [Fact]
        public void EditDistance0()
        {
            Assert.Equal(EditDistance.GetEditDistance("", ""), 0);
            Assert.Equal(EditDistance.GetEditDistance("a", "a"), 0);
        }

        [Fact]
        public void EditDistance1()
        {
            Assert.Equal(EditDistance.GetEditDistance("", "a"), 1);
            Assert.Equal(EditDistance.GetEditDistance("a", ""), 1);
            Assert.Equal(EditDistance.GetEditDistance("a", "b"), 1);
            Assert.Equal(EditDistance.GetEditDistance("ab", "a"), 1);
            Assert.Equal(EditDistance.GetEditDistance("a", "ab"), 1);
            Assert.Equal(EditDistance.GetEditDistance("aabb", "abab"), 1);
        }

        [Fact]
        public void EditDistance2()
        {
            Assert.Equal(EditDistance.GetEditDistance("", "aa"), 2);
            Assert.Equal(EditDistance.GetEditDistance("aa", ""), 2);
            Assert.Equal(EditDistance.GetEditDistance("aa", "bb"), 2);
            Assert.Equal(EditDistance.GetEditDistance("aab", "a"), 2);
            Assert.Equal(EditDistance.GetEditDistance("a", "aab"), 2);
            Assert.Equal(EditDistance.GetEditDistance("aababb", "ababab"), 2);
        }

        [Fact]
        public void EditDistance3()
        {
            Assert.Equal(EditDistance.GetEditDistance("", "aaa"), 3);
            Assert.Equal(EditDistance.GetEditDistance("aaa", ""), 3);
            Assert.Equal(EditDistance.GetEditDistance("aaa", "bbb"), 3);
            Assert.Equal(EditDistance.GetEditDistance("aaab", "a"), 3);
            Assert.Equal(EditDistance.GetEditDistance("a", "aaab"), 3);
            Assert.Equal(EditDistance.GetEditDistance("aababbab", "abababaa"), 3);
        }

        [Fact]
        public void MoreEditDistance()
        {
            Assert.Equal(EditDistance.GetEditDistance("barking", "corkliness"), 6);
        }

        [Fact]
        public void LongestCommonSubstring0()
        {
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("", ""), 0);
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("a", "b"), 0);
        }

        public void LongestCommonSubstring1()
        {
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("a", "a"), 1);
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("ab", "a"), 1);
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("a", "ab"), 1);
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("ba", "ab"), 1);
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("foo", "arf"), 1);
        }

        public void MoreLongestCommonSubstring()
        {
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("aabaaab", "aaa"), 3);
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("kangaroo", "schoolbus"), 2);
            Assert.Equal(EditDistance.GetLongestCommonSubsequenceLength("inexorable", "exorcism"), 4);
        }
    }
}
