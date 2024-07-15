// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    public class BloomFilterTests
    {
        private static IEnumerable<string> GenerateStrings(int count)
        {
            for (var i = 1; i <= count; i++)
            {
                yield return GenerateString(i);
            }
        }

        private static string GenerateString(int value)
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

        private static void Test(bool isCaseSensitive)
        {
            var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            var strings = new HashSet<string>(GenerateStrings(2000).Skip(500).Take(1000), comparer);
            var testStrings = GenerateStrings(100000);

            for (var d = 0.1; d >= 0.0001; d /= 10)
            {
                var filter = new BloomFilter(d, isCaseSensitive, strings);

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

        [Fact]
        public void Test1()
            => Test(isCaseSensitive: true);

        [Fact]
        public void TestInsensitive()
            => Test(isCaseSensitive: false);

        [Fact]
        public void TestEmpty()
        {
            for (var d = 0.1; d >= 0.0001; d /= 10)
            {
                var filter = new BloomFilter(d, isCaseSensitive: true, []);
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

        [Fact]
        public void TestSerialization()
        {
            var stream = new MemoryStream();
            var bloomFilter = new BloomFilter(0.001, isCaseSensitive: false, ["Hello, World"]);

            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                bloomFilter.WriteTo(writer);
            }

            stream.Position = 0;

            using var reader = ObjectReader.TryGetReader(stream);
            var rehydratedFilter = BloomFilter.ReadFrom(reader);
            Assert.True(bloomFilter.IsEquivalent(rehydratedFilter));
        }

        [Fact]
        public void TestSerialization2()
        {
            var stream = new MemoryStream();
            var bloomFilter = new BloomFilter(0.001, ["Hello, World"], [long.MaxValue, -1, 0, 1, long.MinValue]);

            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                bloomFilter.WriteTo(writer);
            }

            stream.Position = 0;

            using var reader = ObjectReader.TryGetReader(stream);
            var rehydratedFilter = BloomFilter.ReadFrom(reader);
            Assert.True(bloomFilter.IsEquivalent(rehydratedFilter));
        }

        [Fact]
        public void TestInt64()
        {
            var longs = CreateLongs(GenerateStrings(2000).Skip(500).Take(1000).Select(s => s.GetHashCode()).ToList());
            var testLongs = CreateLongs(GenerateStrings(100000).Select(s => s.GetHashCode()).ToList());

            for (var d = 0.1; d >= 0.0001; d /= 10)
            {
                var filter = new BloomFilter(d, [], longs);

                var correctCount = 0.0;
                var incorrectCount = 0.0;
                foreach (var test in testLongs)
                {
                    var actualContains = longs.Contains(test);
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

        private static HashSet<long> CreateLongs(List<int> ints)
        {
            var result = new HashSet<long>();

            for (var i = 0; i < ints.Count; i += 2)
            {
                var long1 = ((long)ints[i]) << 32;
                var long2 = (long)ints[i + 1];

                result.Add(long1 | long2);
            }

            return result;
        }
    }
}
