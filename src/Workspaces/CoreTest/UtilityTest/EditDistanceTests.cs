// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EditDistanceTests
    {
        private static int GetEditDistance(string s, string t)
        {
            // We want the full edit distance, without bailing out early because we crossed the
            // threshold.
            return EditDistance.GetEditDistance(s, t);
        }

        [Fact]
        public void EditDistance0()
        {
            Assert.Equal(GetEditDistance("", ""), 0);
            Assert.Equal(GetEditDistance("a", "a"), 0);
        }

        [Fact]
        public void EditDistance1()
        {
            Assert.Equal(GetEditDistance("", "a"), 1);
            Assert.Equal(GetEditDistance("a", ""), 1);
            Assert.Equal(GetEditDistance("a", "b"), 1);
            Assert.Equal(GetEditDistance("ab", "a"), 1);
            Assert.Equal(GetEditDistance("a", "ab"), 1);
            Assert.Equal(GetEditDistance("aabb", "abab"), 1);
        }

        [Fact]
        public void EditDistance2()
        {
            Assert.Equal(GetEditDistance("", "aa"), 2);
            Assert.Equal(GetEditDistance("aa", ""), 2);
            Assert.Equal(GetEditDistance("aa", "bb"), 2);
            Assert.Equal(GetEditDistance("aab", "a"), 2);
            Assert.Equal(GetEditDistance("a", "aab"), 2);
            Assert.Equal(GetEditDistance("aababb", "ababab"), 2);
        }

        [Fact]
        public void EditDistance3()
        {
            Assert.Equal(GetEditDistance("", "aaa"), 3);
            Assert.Equal(GetEditDistance("aaa", ""), 3);
            Assert.Equal(GetEditDistance("aaa", "bbb"), 3);
            Assert.Equal(GetEditDistance("aaab", "a"), 3);
            Assert.Equal(GetEditDistance("a", "aaab"), 3);
            Assert.Equal(GetEditDistance("aababbab", "abababaa"), 3);
        }

        [Fact]
        public void EditDistance4()
        {
            Assert.Equal(GetEditDistance("XlmReade", "XmlReader"), 2);
        }

        public void EditDistance5()
        {
            Assert.Equal(GetEditDistance("Zeil", "trials"), 4);
        }

        [Fact]
        public void EditDistance6()
        {
            Assert.Equal(GetEditDistance("barking", "corkliness"), 6);
        }

        [Fact]
        public void EditDistance7()
        {
            Assert.Equal(GetEditDistance("kitten", "sitting"), 3);
        }

        [Fact]
        public void EditDistance8()
        {
            Assert.Equal(GetEditDistance("sunday", "saturday"), 3);
        }

        [Fact]
        public void EditDistance9()
        {
            Assert.Equal(GetEditDistance("meilenstein", "levenshtein"), 4);
        }

        [Fact]
        public void EditDistance10()
        {
            Assert.Equal(GetEditDistance("rosettacode", "raisethysword"), 8);
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
            Assert.Equal(GetEditDistance("CA", "ABC"), 2);
        }
    }
}
