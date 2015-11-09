// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    public class BloomFilterTests
    {
        private IEnumerable<string> GenerateStrings(int count)
        {
            for (var i = 1; i <= count; i++)
            {
                yield return GenerateString(i);
            }
        }

        private string GenerateString(int value)
        {
            const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var builder = new StringBuilder();

            while (value > 0)
            {
                var v = value % Alphabet.Length;
                var c = Alphabet[v];
                builder.Append(c);
                value /= Alphabet.Length;
            }

            return builder.ToString();
        }

        private void Test(bool isCaseSensitive)
        {
            var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            var strings = GenerateStrings(2000).Skip(500).Take(1000).ToSet(comparer);
            var testStrings = GenerateStrings(100000);

            for (var d = 0.1; d >= 0.0001; d /= 10)
            {
                var filter = new BloomFilter(strings.Count, d, isCaseSensitive);
                filter.AddRange(strings);

                var correctCount = 0.0;
                var incorrectCount = 0.0;
                foreach (var test in testStrings)
                {
                    var actualContains = strings.Contains(test);
                    var filterContains = filter.ProbablyContains(test);

                    if (!filterContains)
                    {
                        // if the filter says no, then it can't be in the real set.
                        Assert.False(actualContains);
                    }

                    if (actualContains == filterContains)
                    {
                        correctCount++;
                    }
                    else
                    {
                        incorrectCount++;
                    }
                }

                var falsePositivePercentage = incorrectCount / (correctCount + incorrectCount);
                Assert.True(falsePositivePercentage < (d * 1.5), string.Format("falsePositivePercentage={0}, d={1}", falsePositivePercentage, d));
            }
        }

        [WpfFact]
        public void Test1()
        {
            Test(isCaseSensitive: true);
        }

        [WpfFact]
        public void TestInsensitive()
        {
            Test(isCaseSensitive: false);
        }

        [WpfFact]
        public void TestEmpty()
        {
            for (var d = 0.1; d >= 0.0001; d /= 10)
            {
                var filter = new BloomFilter(0, d, isCaseSensitive: true);
                Assert.False(filter.ProbablyContains(string.Empty));
                Assert.False(filter.ProbablyContains("a"));
                Assert.False(filter.ProbablyContains("b"));
                Assert.False(filter.ProbablyContains("c"));

                var testStrings = GenerateStrings(100000);
                foreach (var test in testStrings)
                {
                    Assert.False(filter.ProbablyContains(test));
                }
            }
        }

        [WpfFact]
        public void TestSerialization()
        {
            var stream = new MemoryStream();
            var bloomFilter = new BloomFilter(0.001, false, new[] { "Hello, World" });

            using (var writer = new ObjectWriter(stream))
            {
                bloomFilter.WriteTo(writer);
            }

            stream.Position = 0;

            using (var reader = new ObjectReader(stream))
            {
                var rehydratedFilter = BloomFilter.ReadFrom(reader);
                Assert.True(bloomFilter.IsEquivalent(rehydratedFilter));
            }
        }
    }
}
