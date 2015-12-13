// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EditDistanceTests
    {
        private static int VerifyEditDistance(string s, string t)
        {
            // We want the full edit distance, without bailing out early because we crossed the
            // threshold.
            var editDistance1 = EditDistance.GetEditDistance(s, t);
            var editDistance2 = EditDistance.GetEditDistance(s, t, editDistance1);

            Assert.Equal(editDistance1, editDistance2);

            return editDistance1;
        }

        [Fact]
        public void EditDistance0()
        {
            Assert.Equal(VerifyEditDistance("", ""), 0);
            Assert.Equal(VerifyEditDistance("a", "a"), 0);
        }

        [Fact]
        public void EditDistance1()
        {
            Assert.Equal(VerifyEditDistance("", "a"), 1);
            Assert.Equal(VerifyEditDistance("a", ""), 1);
            Assert.Equal(VerifyEditDistance("a", "b"), 1);
            Assert.Equal(VerifyEditDistance("ab", "a"), 1);
            Assert.Equal(VerifyEditDistance("a", "ab"), 1);
            Assert.Equal(VerifyEditDistance("aabb", "abab"), 1);
        }

        [Fact]
        public void EditDistance2()
        {
            Assert.Equal(VerifyEditDistance("", "aa"), 2);
            Assert.Equal(VerifyEditDistance("aa", ""), 2);
            Assert.Equal(VerifyEditDistance("aa", "bb"), 2);
            Assert.Equal(VerifyEditDistance("aab", "a"), 2);
            Assert.Equal(VerifyEditDistance("a", "aab"), 2);
            Assert.Equal(VerifyEditDistance("aababb", "ababab"), 2);
        }

        [Fact]
        public void EditDistance3()
        {
            Assert.Equal(VerifyEditDistance("", "aaa"), 3);
            Assert.Equal(VerifyEditDistance("aaa", ""), 3);
            Assert.Equal(VerifyEditDistance("aaa", "bbb"), 3);
            Assert.Equal(VerifyEditDistance("aaab", "a"), 3);
            Assert.Equal(VerifyEditDistance("a", "aaab"), 3);
            Assert.Equal(VerifyEditDistance("aababbab", "abababaa"), 3);
        }

        [Fact]
        public void EditDistance4()
        {
            Assert.Equal(VerifyEditDistance("XlmReade", "XmlReader"), 2);
        }

        public void EditDistance5()
        {
            Assert.Equal(VerifyEditDistance("Zeil", "trials"), 4);
        }

        [Fact]
        public void EditDistance6()
        {
            Assert.Equal(VerifyEditDistance("barking", "corkliness"), 6);
        }

        [Fact]
        public void EditDistance7()
        {
            Assert.Equal(VerifyEditDistance("kitten", "sitting"), 3);
        }

        [Fact]
        public void EditDistance8()
        {
            Assert.Equal(VerifyEditDistance("sunday", "saturday"), 3);
        }

        [Fact]
        public void EditDistance9()
        {
            Assert.Equal(VerifyEditDistance("meilenstein", "levenshtein"), 4);
        }

        [Fact]
        public void EditDistance10()
        {
            Assert.Equal(VerifyEditDistance("rosettacode", "raisethysword"), 8);
        }

        [Fact]
        public void EditDistance11()
        {
            Assert.Equal(VerifyEditDistance("aaaab", "aaabc"), 2);
            Assert.Equal(VerifyEditDistance("aaaab", "aabcc"), 3);
            Assert.Equal(VerifyEditDistance("aaaab", "abccc"), 4);
            Assert.Equal(VerifyEditDistance("aaaab", "bcccc"), 5);

            Assert.Equal(VerifyEditDistance("aaaabb", "aaabbc"), 2);
            Assert.Equal(VerifyEditDistance("aaaabb", "aabbcc"), 4);
            Assert.Equal(VerifyEditDistance("aaaabb", "abbccc"), 5);
            Assert.Equal(VerifyEditDistance("aaaabb", "bbcccc"), 6);

            Assert.Equal(VerifyEditDistance("aaaabbb", "aaabbbc"), 2);
            Assert.Equal(VerifyEditDistance("aaaabbb", "aabbbcc"), 4);
            Assert.Equal(VerifyEditDistance("aaaabbb", "abbbccc"), 6);
            Assert.Equal(VerifyEditDistance("aaaabbb", "bbbcccc"), 7);

            Assert.Equal(VerifyEditDistance("aaaabbbb", "aaabbbbc"), 2);
            Assert.Equal(VerifyEditDistance("aaaabbbb", "aabbbbcc"), 4);
            Assert.Equal(VerifyEditDistance("aaaabbbb", "abbbbccc"), 6);
            Assert.Equal(VerifyEditDistance("aaaabbbb", "bbbbcccc"), 8);
        }

        [Fact]
        public void TestMetric()
        {
            // If our edit distance is a metric then ED(CA,ABC) = 2 because CA -> AC -> ABC
            // In this case.  This then satisifes the triangle inequality because 
            // ED(CA, AC) + ED(AC, ABC) >= ED(CA, ABC)   ...   1 + 1 >= 2
            //
            // If it's not implemented with a metric (like if we used the Optimal String Alignment
            // algorithm), then the we could get an edit distance of 3 "CA -> A -> AB -> ABC".  
            // This violates the triangle inequality rule because: 
            // 
            // OSA(CA,AC) + OSA(AC,ABC) >= OSA(CA,ABC)  ...   1 + 1 >= 3    is not true.
            //
            // Being a metric is important so that we can properly use this with BKTrees.
            Assert.Equal(VerifyEditDistance("CA", "ABC"), 2);
        }
    }
}
