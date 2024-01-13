// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/ImmutableDictionaryTest.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public partial class ImmutableSegmentedDictionaryTest : ImmutableDictionaryTestBase
    {
        [Fact]
        public void AddExistingKeySameValueTest()
        {
            AddExistingKeySameValueTestHelper(Empty<string, string>(StringComparer.Ordinal), "Company", "Microsoft", "Microsoft");
        }

        [Fact]
        public void AddExistingKeyDifferentValueTest()
        {
            AddExistingKeyDifferentValueTestHelper(Empty<string, string>(StringComparer.Ordinal), "Company", "Microsoft", "MICROSOFT");
        }

        [Fact]
        public void UnorderedChangeTest()
        {
            var map = Empty<string, string>(StringComparer.Ordinal)
                .Add("Johnny", "Appleseed")
                .Add("JOHNNY", "Appleseed");
            Assert.Equal(2, map.Count);
            Assert.True(map.ContainsKey("Johnny"));
            Assert.False(map.ContainsKey("johnny"));
            var newMap = map.WithComparer(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(1, newMap.Count);
            Assert.True(newMap.ContainsKey("Johnny"));
            Assert.True(newMap.ContainsKey("johnny")); // because it's case insensitive
        }

        [Fact]
        public void ToSortedTest()
        {
            var map = Empty<string, string>(StringComparer.Ordinal)
                .Add("Johnny", "Appleseed")
                .Add("JOHNNY", "Appleseed");
            var sortedMap = map.ToImmutableSortedDictionary(StringComparer.Ordinal);
            Assert.Equal(sortedMap.Count, map.Count);
            CollectionAssertAreEquivalent<KeyValuePair<string, string>>(sortedMap.ToList(), map.ToList());
        }

        [Fact]
        public void SetItemUpdateEqualKeyTest()
        {
            var map = Empty<string, int>().WithComparer(StringComparer.OrdinalIgnoreCase)
                .SetItem("A", 1);
            map = map.SetItem("a", 2);
            Assert.Equal("A", map.Keys.Single());
        }

        [Fact]
        public void ContainsValueTest()
        {
            ContainsValueTestHelper(ImmutableSegmentedDictionary<int, GenericParameterHelper>.Empty, 1, new GenericParameterHelper());
        }

        [Fact]
        public void ContainsValue_NoSuchValue_ReturnsFalse()
        {
            ImmutableSegmentedDictionary<int, string?> dictionary = new Dictionary<int, string?>
            {
                { 1, "a" },
                { 2, "b" }
            }.ToImmutableSegmentedDictionary();
            Assert.False(dictionary.ContainsValue("c"));
            Assert.False(dictionary.ContainsValue(null));
        }

        [Fact]
        public void Create()
        {
            IEnumerable<KeyValuePair<string, string>> pairs = new Dictionary<string, string> { { "a", "b" } };
            var keyComparer = StringComparer.OrdinalIgnoreCase;

            var dictionary = ImmutableSegmentedDictionary.Create<string, string>();
            Assert.Equal(0, dictionary.Count);
            Assert.Same(EqualityComparer<string>.Default, dictionary.KeyComparer);

            dictionary = ImmutableSegmentedDictionary.Create<string, string>(keyComparer);
            Assert.Equal(0, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);

            dictionary = ImmutableSegmentedDictionary.CreateRange(pairs);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(EqualityComparer<string>.Default, dictionary.KeyComparer);

            dictionary = ImmutableSegmentedDictionary.CreateRange(keyComparer, pairs);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);
        }

        [Fact]
        public void ToImmutableDictionary()
        {
            IEnumerable<KeyValuePair<string, string>> pairs = new Dictionary<string, string> { { "a", "B" } };
            var keyComparer = StringComparer.OrdinalIgnoreCase;

            ImmutableSegmentedDictionary<string, string> dictionary = pairs.ToImmutableSegmentedDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.Same(EqualityComparer<string>.Default, dictionary.KeyComparer);

            dictionary = pairs.ToImmutableSegmentedDictionary(keyComparer);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);

            dictionary = pairs.ToImmutableSegmentedDictionary(p => p.Key.ToUpperInvariant(), p => p.Value.ToLowerInvariant());
            Assert.Equal(1, dictionary.Count);
            Assert.Equal("A", dictionary.Keys.Single());
            Assert.Equal("b", dictionary.Values.Single());
            Assert.Same(EqualityComparer<string>.Default, dictionary.KeyComparer);

            dictionary = pairs.ToImmutableSegmentedDictionary(p => p.Key.ToUpperInvariant(), p => p.Value.ToLowerInvariant(), keyComparer);
            Assert.Equal(1, dictionary.Count);
            Assert.Equal("A", dictionary.Keys.Single());
            Assert.Equal("b", dictionary.Values.Single());
            Assert.Same(keyComparer, dictionary.KeyComparer);

            var list = new int[] { 1, 2 };
            var intDictionary = list.ToImmutableSegmentedDictionary(n => (double)n);
            Assert.Equal(1, intDictionary[1.0]);
            Assert.Equal(2, intDictionary[2.0]);
            Assert.Equal(2, intDictionary.Count);

            var stringIntDictionary = list.ToImmutableSegmentedDictionary(n => n.ToString(), StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, stringIntDictionary.KeyComparer);
            Assert.Equal(1, stringIntDictionary["1"]);
            Assert.Equal(2, stringIntDictionary["2"]);
            Assert.Equal(2, intDictionary.Count);

            Assert.Throws<ArgumentNullException>("keySelector", () => list.ToImmutableSegmentedDictionary<int, int>(null!));
            Assert.Throws<ArgumentNullException>("keySelector", () => list.ToImmutableSegmentedDictionary<int, int, int>(null!, v => v));
            Assert.Throws<ArgumentNullException>("elementSelector", () => list.ToImmutableSegmentedDictionary<int, int, int>(k => k, null!));

            list.ToDictionary(k => k, v => v, null); // verifies BCL behavior is to not throw.
            list.ToImmutableSegmentedDictionary(k => k, v => v, null);
        }

        [Fact]
        public void ToImmutableDictionaryOptimized()
        {
            var dictionary = ImmutableSegmentedDictionary.Create<string, string>();
            var result = dictionary.ToImmutableSegmentedDictionary();
            Assert.True(IsSame(dictionary, result));

            var cultureComparer = StringComparer.CurrentCulture;
            result = dictionary.WithComparer(cultureComparer);
            Assert.Same(cultureComparer, result.KeyComparer);
        }

        [Fact]
        public void WithComparer()
        {
            var map = ImmutableSegmentedDictionary.Create<string, string>().Add("a", "1").Add("B", "1");
            Assert.Same(EqualityComparer<string>.Default, map.KeyComparer);
            Assert.True(map.ContainsKey("a"));
            Assert.False(map.ContainsKey("A"));

            map = map.WithComparer(StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, map.KeyComparer);
            Assert.Equal(2, map.Count);
            Assert.True(map.ContainsKey("a"));
            Assert.True(map.ContainsKey("A"));
            Assert.True(map.ContainsKey("b"));
        }

        [Fact]
        public void WithComparerCollisions()
        {
            // First check where collisions have matching values.
            var map = ImmutableSegmentedDictionary.Create<string, string>()
                .Add("a", "1").Add("A", "1");
            map = map.WithComparer(StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, map.KeyComparer);
            Assert.Equal(1, map.Count);
            Assert.True(map.ContainsKey("a"));
            Assert.Equal("1", map["a"]);

            // Now check where collisions have conflicting values.
            map = ImmutableSegmentedDictionary.Create<string, string>()
              .Add("a", "1").Add("A", "2").Add("b", "3");
            Assert.Throws<ArgumentException>(null, () => map.WithComparer(StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void CollisionExceptionMessageContainsKey()
        {
            var map = ImmutableSegmentedDictionary.Create<string, string>()
                .Add("firstKey", "1").Add("secondKey", "2");
            var exception = Assert.Throws<ArgumentException>(null, () => map.Add("firstKey", "3"));
            Assert.Contains("firstKey", exception.Message);
        }

        [Fact]
        public void WithComparerEmptyCollection()
        {
            var map = ImmutableSegmentedDictionary.Create<string, string>();
            Assert.Same(EqualityComparer<string>.Default, map.KeyComparer);
            map = map.WithComparer(StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, map.KeyComparer);
        }

        [Fact]
        public void GetValueOrDefaultOfIImmutableDictionary()
        {
            IImmutableDictionary<string, int> empty = ImmutableSegmentedDictionary.Create<string, int>();
            IImmutableDictionary<string, int> populated = ImmutableSegmentedDictionary.Create<string, int>().Add("a", 5);
            Assert.Equal(0, empty.GetValueOrDefault("a"));
            Assert.Equal(1, empty.GetValueOrDefault("a", 1));
            Assert.Equal(5, populated.GetValueOrDefault("a"));
            Assert.Equal(5, populated.GetValueOrDefault("a", 1));
        }

        [Fact]
        public void GetValueOrDefaultOfConcreteType()
        {
            var empty = ImmutableSegmentedDictionary.Create<string, int>();
            var populated = ImmutableSegmentedDictionary.Create<string, int>().Add("a", 5);
            Assert.Equal(0, empty.GetValueOrDefault("a"));
            Assert.Equal(1, empty.GetValueOrDefault("a", 1));
            Assert.Equal(5, populated.GetValueOrDefault("a"));
            Assert.Equal(5, populated.GetValueOrDefault("a", 1));
        }

        [Fact(Skip = "Not implemented: https://github.com/dotnet/roslyn/issues/50657")]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableSegmentedDictionary.Create<int, int>());
            ImmutableSegmentedDictionary<string, int> dict = ImmutableSegmentedDictionary.Create<string, int>().Add("One", 1).Add("Two", 2);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(dict);

            object rootNode = DebuggerAttributes.GetFieldValue(ImmutableSegmentedDictionary.Create<string, string>(), "_root") ?? throw new InvalidOperationException();
            DebuggerAttributes.ValidateDebuggerDisplayReferences(rootNode);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>()!.State == DebuggerBrowsableState.RootHidden);
            KeyValuePair<string, int>[]? items = itemProperty.GetValue(info.Instance) as KeyValuePair<string, int>[];
            Assert.Equal(dict, items);
        }

        [Fact(Skip = "Not implemented: https://github.com/dotnet/roslyn/issues/50657")]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableSegmentedDictionary.Create<string, int>());
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object?)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void Clear_NoComparer_ReturnsEmptyWithoutComparer()
        {
            ImmutableSegmentedDictionary<string, int> dictionary = new Dictionary<string, int>
            {
                { "a", 1 }
            }.ToImmutableSegmentedDictionary();
            Assert.True(IsSame(ImmutableSegmentedDictionary<string, int>.Empty, dictionary.Clear()));
            Assert.NotEmpty(dictionary);
        }

        [Fact]
        public void Clear_HasComparer_ReturnsEmptyWithOriginalComparer()
        {
            ImmutableSegmentedDictionary<string, int> dictionary = new Dictionary<string, int>
            {
                { "a", 1 }
            }.ToImmutableSegmentedDictionary(StringComparer.OrdinalIgnoreCase);

            ImmutableSegmentedDictionary<string, int> clearedDictionary = dictionary.Clear();
            Assert.False(IsSame(ImmutableSegmentedDictionary<string, int>.Empty, clearedDictionary.Clear()));
            Assert.NotEmpty(dictionary);

            clearedDictionary = clearedDictionary.Add("a", 1);
            Assert.True(clearedDictionary.ContainsKey("A"));
        }

        [Fact]
        public void Indexer_KeyNotFoundException_ContainsKeyInMessage()
        {
            var map = ImmutableSegmentedDictionary.Create<string, string>()
                .Add("a", "1").Add("b", "2");
            var missingKey = "__ThisKeyDoesNotExist__";
            var exception = Assert.Throws<KeyNotFoundException>(() => map[missingKey]);
            Assert.Contains(missingKey, exception.Message);
        }

        [Fact]
        public void Keys_All()
        {
            var map = ImmutableSegmentedDictionary.Create<string, string>()
                .Add("a", "1")
                .Add("b", "2");

            Assert.False(map.Keys.All((key, arg) => key == arg, "a"));
            Assert.True(map.Keys.All((key, arg) => key.Length == arg, 1));

            Assert.True(ImmutableSegmentedDictionary<int, int>.Empty.Keys.All((_, _) => false, 0));
        }

        [Fact]
        public void Values_All()
        {
            var map = ImmutableSegmentedDictionary.Create<string, string>()
                .Add("a", "1")
                .Add("b", "2");

            Assert.False(map.Values.All((key, arg) => key == arg, "1"));
            Assert.True(map.Values.All((key, arg) => key.Length == arg, 1));

            Assert.True(ImmutableSegmentedDictionary<int, int>.Empty.Values.All((_, _) => false, 0));
        }

        protected override IImmutableDictionary<TKey, TValue> Empty<TKey, TValue>()
        {
            return ImmutableSegmentedDictionaryTest.Empty<TKey, TValue>();
        }

        protected override IImmutableDictionary<string, TValue> Empty<TValue>(StringComparer comparer)
        {
            return ImmutableSegmentedDictionary.Create<string, TValue>(comparer);
        }

        protected override IEqualityComparer<TValue> GetValueComparer<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary)
        {
            return EqualityComparer<TValue>.Default;
        }

        private protected static void ContainsValueTestHelper<TKey, TValue>(ImmutableSegmentedDictionary<TKey, TValue> map, TKey key, TValue value)
            where TKey : notnull
        {
            Assert.False(map.ContainsValue(value));
            Assert.True(map.Add(key, value).ContainsValue(value));
        }

        private static ImmutableSegmentedDictionary<TKey, TValue> Empty<TKey, TValue>(IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
        {
            return ImmutableSegmentedDictionary<TKey, TValue>.Empty.WithComparer(keyComparer);
        }

        /// <summary>
        /// An ordinal comparer for case-insensitive strings.
        /// </summary>
        private class MyStringOrdinalComparer : EqualityComparer<CaseInsensitiveString?>
        {
            public override bool Equals(CaseInsensitiveString? x, CaseInsensitiveString? y)
            {
                return StringComparer.Ordinal.Equals(x!.Value, y!.Value);
            }

            public override int GetHashCode(CaseInsensitiveString obj)
            {
                return StringComparer.Ordinal.GetHashCode(obj.Value);
            }
        }

        /// <summary>
        /// A string-wrapper that considers equality based on case insensitivity.
        /// </summary>
        private class CaseInsensitiveString
        {
            public CaseInsensitiveString(string value)
            {
                Value = value;
            }

            public string Value { get; private set; }
            public override int GetHashCode()
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(this.Value);
            }
            public override bool Equals(object? obj)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(this.Value, ((CaseInsensitiveString?)obj)!.Value);
            }
        }
    }
}
