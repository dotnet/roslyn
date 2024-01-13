// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    /// <summary>
    /// Tests for CachingLookup.
    /// </summary>
    public class CachingLookupTests
    {
        private readonly Random _randomCaseGenerator = new Random(17);

        private int[] RandomNumbers(int length, int seed)
        {
            Random rand = new Random(seed);

            int[] result = new int[length];
            for (int i = 0; i < length; ++i)
            {
                result[i] = rand.Next(100, ((length / 10) + 4) * 100);
            }

            return result;
        }

        private SegmentedHashSet<string> Keys(int[] numbers, bool randomCase, IEqualityComparer<string> comparer)
        {
            var keys = new SegmentedHashSet<string>(comparer);
            foreach (var n in numbers)
            {
                keys.Add(GetKey(n, randomCase));
            }

            return keys;
        }

        private string GetKey(int number, bool randomCase)
        {
            if (randomCase)
            {
                bool upper = _randomCaseGenerator.Next(2) == 0;
                return (upper ? "AA" : "aa") + Right2Chars(number.ToString());
            }
            else
            {
                return "AA" + Right2Chars(number.ToString());
            }
        }

        private ImmutableArray<int> Values(string key, int[] numbers, bool ignoreCase)
        {
            return (from n in numbers
                    where string.Equals(GetKey(n, ignoreCase), key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                    select n).ToArray().AsImmutableOrNull();
        }

        private ILookup<string, int> CreateLookup(int[] numbers, bool randomCase)
        {
            if (randomCase)
            {
                return numbers.ToLookup(n => GetKey(n, randomCase), StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                return numbers.ToLookup(n => GetKey(n, randomCase), StringComparer.Ordinal);
            }
        }

        private string Right2Chars(string s)
        {
            return s.Substring(s.Length - 2);
        }

        private void CheckEqualEnumerable<T>(IEnumerable<T> e1, IEnumerable<T> e2)
        {
            List<T> l1 = e1.ToList();
            List<T> l2 = e2.ToList();

            Assert.Equal(l1.Count, l2.Count);

            foreach (T item in l1)
            {
                Assert.Contains(item, l2);
            }

            foreach (T item in l2)
            {
                Assert.Contains(item, l1);
            }
        }

        private void CompareLookups1(ILookup<string, int> look1, CachingDictionary<string, int> look2, SegmentedHashSet<string> keys)
        {
            foreach (string k in keys)
            {
                Assert.Equal(look1.Contains(k), look2.Contains(k));
                CheckEqualEnumerable(look1[k], look2[k]);
            }

            foreach (string k in new string[] { "goo", "bar", "banana", "flibber" })
            {
                Assert.False(look1.Contains(k));
                Assert.False(look2.Contains(k));
                Assert.Empty(look1[k]);
                Assert.Empty(look2[k]);
            }
        }

        private void CompareLookups2(ILookup<string, int> look1, CachingDictionary<string, int> look2, SegmentedHashSet<string> keys)
        {
            foreach (string k in look1.Select(g => g.Key))
            {
                CheckEqualEnumerable(look1[k], look2[k]);
            }

            foreach (string k in look2.Keys)
            {
                CheckEqualEnumerable(look1[k], look2[k]);
            }

            Assert.Equal(look1.Count, look2.Count);
        }

        private void CompareLookups2(CachingDictionary<string, int> look1, ILookup<string, int> look2, SegmentedHashSet<string> keys)
        {
            foreach (string k in look1.Keys)
            {
                CheckEqualEnumerable(look1[k], look2[k]);
            }

            foreach (string k in look2.Select(g => g.Key))
            {
                CheckEqualEnumerable(look1[k], look2[k]);
            }

            Assert.Equal(look1.Count, look2.Count);
        }

        [Fact]
        public void CachingLookupCorrectResults()
        {
            StringComparer comparer = StringComparer.Ordinal;
            int[] numbers = RandomNumbers(200, 11234);
            var dict = new Dictionary<string, ImmutableArray<int>>(comparer);
            foreach (string k in Keys(numbers, false, comparer))
            {
                dict.Add(k, Values(k, numbers, false));
            }

            var look1 = CreateLookup(numbers, false);
            var look2 = new CachingDictionary<string, int>(
                s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                (c) => Keys(numbers, false, comparer: c), comparer);
            CompareLookups1(look1, look2, Keys(numbers, false, comparer));

            look1 = CreateLookup(numbers, false);
            look2 = new CachingDictionary<string, int>(
                s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                (c) => Keys(numbers, false, comparer: c), comparer);
            CompareLookups2(look1, look2, Keys(numbers, false, comparer));
            CompareLookups1(look1, look2, Keys(numbers, false, comparer));

            look1 = CreateLookup(numbers, false);
            look2 = new CachingDictionary<string, int>(
                s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                (c) => Keys(numbers, false, comparer: c), comparer);
            CompareLookups2(look2, look1, Keys(numbers, false, comparer));
            CompareLookups1(look1, look2, Keys(numbers, false, comparer));
        }

        [Fact]
        public void CachingLookupCaseInsensitive()
        {
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            int[] numbers = RandomNumbers(300, 719);
            var dict = new Dictionary<string, ImmutableArray<int>>(comparer);
            foreach (string k in Keys(numbers, false, comparer))
            {
                dict.Add(k, Values(k, numbers, false));
            }

            var look1 = CreateLookup(numbers, true);
            var look2 = new CachingDictionary<string, int>(
                s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                (c) => Keys(numbers, true, comparer: c), comparer);
            CompareLookups1(look1, look2, Keys(numbers, true, comparer));

            look1 = CreateLookup(numbers, true);
            look2 = new CachingDictionary<string, int>(
                s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                (c) => Keys(numbers, true, comparer: c), comparer);
            CompareLookups2(look1, look2, Keys(numbers, true, comparer));
            CompareLookups1(look1, look2, Keys(numbers, true, comparer));

            look1 = CreateLookup(numbers, true);
            look2 = new CachingDictionary<string, int>(
                s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                (c) => Keys(numbers, true, comparer: c), comparer);
            CompareLookups2(look2, look1, Keys(numbers, true, comparer));
            CompareLookups1(look1, look2, Keys(numbers, true, comparer));
        }

        [Fact]
        public void CachingLookupCaseInsensitiveNoCacheMissingKeys()
        {
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            int[] numbers = RandomNumbers(435, 19874);
            var dict = new Dictionary<string, ImmutableArray<int>>(comparer);
            foreach (string k in Keys(numbers, false, comparer))
            {
                dict.Add(k, Values(k, numbers, false));
            }

            var look1 = CreateLookup(numbers, true);
            var look2 = new CachingDictionary<string, int>(s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                                                                        (c) => Keys(numbers, true, comparer: c), comparer);
            CompareLookups1(look1, look2, Keys(numbers, true, comparer));

            look1 = CreateLookup(numbers, true);
            look2 = new CachingDictionary<string, int>(s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                                                   (c) => Keys(numbers, true, comparer: c), comparer);
            CompareLookups2(look1, look2, Keys(numbers, true, comparer));
            CompareLookups1(look1, look2, Keys(numbers, true, comparer));

            look1 = CreateLookup(numbers, true);
            look2 = new CachingDictionary<string, int>(s => dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>(),
                                                   (c) => Keys(numbers, true, comparer: c), comparer);
            CompareLookups2(look2, look1, Keys(numbers, true, comparer));
            CompareLookups1(look1, look2, Keys(numbers, true, comparer));
        }

        // Ensure that we are called back exactly once per key.
        [Fact]
        public void CallExactlyOncePerKey()
        {
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            int[] numbers = RandomNumbers(435, 19874);
            var dict = new Dictionary<string, ImmutableArray<int>>(comparer);
            foreach (string k in Keys(numbers, false, comparer))
            {
                dict.Add(k, Values(k, numbers, false));
            }

            HashSet<string> lookedUp = new HashSet<string>(comparer);
            bool askedForKeys = false;

            var look1 = new CachingDictionary<string, int>(s =>
            {
                Assert.False(lookedUp.Contains(s));
                lookedUp.Add(s);
                return dict.TryGetValue(s, out var value) ? value : ImmutableArray.Create<int>();
            },
                 (c) =>
            {
                Assert.False(askedForKeys);
                askedForKeys = true;
                return Keys(numbers, true, comparer: c);
            }, comparer);

            string key1 = GetKey(numbers[0], false);
            string key2 = GetKey(numbers[1], false);
            string key3 = GetKey(numbers[2], false);

            ImmutableArray<int> retval;
            retval = look1[key1];
            retval = look1[key2];
            retval = look1[key3];
            retval = look1[key1];
            retval = look1[key2];
            retval = look1[key3];

            retval = look1[key1];
            retval = look1[key2];
            retval = look1[key3];
            retval = look1[key1];
            retval = look1[key2];
            retval = look1[key3];
        }

        [Fact]
        [WorkItem(60259, "https://github.com/dotnet/roslyn/issues/60259")]
        public void FullyPopulateRace_01()
        {
            CachingDictionary<int, FullyPopulateRaceHelper> lookup = null;
            FullyPopulateRaceHelper item = null;
            int itemAccessCount = 0;
            lookup = new CachingDictionary<int, FullyPopulateRaceHelper>(getElementsOfKey, getKeys, EqualityComparer<int>.Default);

            _ = lookup.Count;
            Assert.NotNull(item);
            Assert.Same(item, getItem());

            ImmutableArray<FullyPopulateRaceHelper> getElementsOfKey(int id)
            {
                itemAccessCount++;
                if (item is null && itemAccessCount == 1)
                {
                    item = getItem();
                }

                return ImmutableArray.Create<FullyPopulateRaceHelper>(new FullyPopulateRaceHelper());
            }

            SegmentedHashSet<int> getKeys(IEqualityComparer<int> comparer)
            {
                return new SegmentedHashSet<int>(new[] { 1 }, comparer);
            }

            FullyPopulateRaceHelper getItem()
            {
                return lookup[1][0];
            }
        }

        private class FullyPopulateRaceHelper { }

        [Fact]
        [WorkItem(60259, "https://github.com/dotnet/roslyn/issues/60259")]
        public void FullyPopulateRace_02()
        {
            CachingDictionary<int, FullyPopulateRaceHelper> lookup = null;
            FullyPopulateRaceHelper item = null;
            int itemAccessCount = 0;
            lookup = new CachingDictionary<int, FullyPopulateRaceHelper>(getElementsOfKey, getKeys, EqualityComparer<int>.Default);

            Assert.Empty(lookup[2]);
            _ = lookup.Count;
            Assert.NotNull(item);
            Assert.Same(item, getItem());

            ImmutableArray<FullyPopulateRaceHelper> getElementsOfKey(int id)
            {
                if (id != 1)
                {
                    return ImmutableArray<FullyPopulateRaceHelper>.Empty;
                }

                itemAccessCount++;
                if (item is null && itemAccessCount == 1)
                {
                    item = getItem();
                }

                return ImmutableArray.Create<FullyPopulateRaceHelper>(new FullyPopulateRaceHelper());
            }

            SegmentedHashSet<int> getKeys(IEqualityComparer<int> comparer)
            {
                return new SegmentedHashSet<int>(new[] { 1 }, comparer);
            }

            FullyPopulateRaceHelper getItem()
            {
                return lookup[1][0];
            }
        }
    }
}
