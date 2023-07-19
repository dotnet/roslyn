// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/ImmutableDictionaryBuilderTestBase.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public abstract partial class ImmutableDictionaryBuilderTestBase : ImmutablesTestBase
    {
        [Fact]
        public void Add()
        {
            var builder = this.GetBuilder<string, int>();
            builder.Add("five", 5);
            builder.Add(new KeyValuePair<string, int>("six", 6));
            Assert.Equal(5, builder["five"]);
            Assert.Equal(6, builder["six"]);
            Assert.False(builder.ContainsKey("four"));
        }

        /// <summary>
        /// Verifies that "adding" an entry to the dictionary that already exists
        /// with exactly the same key and value will *not* throw an exception.
        /// </summary>
        /// <remarks>
        /// The BCL Dictionary type would throw in this circumstance.
        /// But in an immutable world, not only do we not care so much since the result is the same.
        /// </remarks>
        [Fact]
        public void AddExactDuplicate()
        {
            var builder = this.GetBuilder<string, int>();
            builder.Add("five", 5);
            builder.Add("five", 5);
            Assert.Equal(1, builder.Count);
        }

        [Fact]
        public void AddExistingKeyWithDifferentValue()
        {
            var builder = this.GetBuilder<string, int>();
            builder.Add("five", 5);
            Assert.Throws<ArgumentException>(null, () => builder.Add("five", 6));
        }

        [Fact]
        public void Indexer()
        {
            var builder = this.GetBuilder<string, int>();

            // Set and set again.
            builder["five"] = 5;
            Assert.Equal(5, builder["five"]);
            builder["five"] = 5;
            Assert.Equal(5, builder["five"]);

            // Set to a new value.
            builder["five"] = 50;
            Assert.Equal(50, builder["five"]);

            // Retrieve an invalid value.
            Assert.Throws<KeyNotFoundException>(() => builder["foo"]);
        }

        [Fact]
        public void ContainsPair()
        {
            var map = this.GetEmptyImmutableDictionary<string, int>().Add("five", 5);
            var builder = this.GetBuilder(map);
            Assert.True(builder.Contains(new KeyValuePair<string, int>("five", 5)));
        }

        [Fact]
        public void RemovePair()
        {
            var map = this.GetEmptyImmutableDictionary<string, int>().Add("five", 5).Add("six", 6);
            var builder = this.GetBuilder(map);
            Assert.True(builder.Remove(new KeyValuePair<string, int>("five", 5)));
            Assert.False(builder.Remove(new KeyValuePair<string, int>("foo", 1)));
            Assert.Equal(1, builder.Count);
            Assert.Equal(6, builder["six"]);
        }

        [Fact]
        public void RemoveKey()
        {
            var map = this.GetEmptyImmutableDictionary<string, int>().Add("five", 5).Add("six", 6);
            var builder = this.GetBuilder(map);
            builder.Remove("five");
            Assert.Equal(1, builder.Count);
            Assert.Equal(6, builder["six"]);
        }

        [Fact]
        public void CopyTo()
        {
            var map = this.GetEmptyImmutableDictionary<string, int>().Add("five", 5);
            var builder = this.GetBuilder(map);
            var array = new KeyValuePair<string, int>[2]; // intentionally larger than source.
            builder.CopyTo(array, 1);
            Assert.Equal(new KeyValuePair<string, int>(), array[0]);
            Assert.Equal(new KeyValuePair<string, int>("five", 5), array[1]);

            Assert.Throws<ArgumentNullException>("array", () => builder.CopyTo(null!, 0));
        }

        [Fact]
        public void IsReadOnly()
        {
            var builder = this.GetBuilder<string, int>();
            Assert.False(builder.IsReadOnly);
        }

        [Fact]
        public void Keys()
        {
            var map = this.GetEmptyImmutableDictionary<string, int>().Add("five", 5).Add("six", 6);
            var builder = this.GetBuilder(map);
            CollectionAssertAreEquivalent(new[] { "five", "six" }, builder.Keys);
            CollectionAssertAreEquivalent(new[] { "five", "six" }, ((IReadOnlyDictionary<string, int>)builder).Keys.ToArray());
        }

        [Fact]
        public void Values()
        {
            var map = this.GetEmptyImmutableDictionary<string, int>().Add("five", 5).Add("six", 6);
            var builder = this.GetBuilder(map);
            CollectionAssertAreEquivalent(new[] { 5, 6 }, builder.Values);
            CollectionAssertAreEquivalent(new[] { 5, 6 }, ((IReadOnlyDictionary<string, int>)builder).Values.ToArray());
        }

        [Fact]
        public void TryGetValue()
        {
            var map = this.GetEmptyImmutableDictionary<string, int>().Add("five", 5).Add("six", 6);
            var builder = this.GetBuilder(map);
            Assert.True(builder.TryGetValue("five", out int value) && value == 5);
            Assert.True(builder.TryGetValue("six", out value) && value == 6);
            Assert.False(builder.TryGetValue("four", out value));
            Assert.Equal(0, value);
        }

        [Fact]
        public void EnumerateTest()
        {
            var map = this.GetEmptyImmutableDictionary<string, int>().Add("five", 5).Add("six", 6);
            var builder = this.GetBuilder(map);
            using (var enumerator = builder.GetEnumerator())
            {
                Assert.True(enumerator.MoveNext());
                Assert.True(enumerator.MoveNext());
                Assert.False(enumerator.MoveNext());
            }

            var manualEnum = builder.GetEnumerator();
            Assert.Equal(default(KeyValuePair<string, int>), manualEnum.Current);
            while (manualEnum.MoveNext()) { }
            Assert.False(manualEnum.MoveNext());
            Assert.Equal(default(KeyValuePair<string, int>), manualEnum.Current);
        }

        [Fact]
        public void IDictionaryMembers()
        {
            var builder = this.GetBuilder<string, int>();
            var dictionary = (IDictionary)builder;
            dictionary.Add("a", 1);
            Assert.True(dictionary.Contains("a"));
            Assert.Equal(1, dictionary["a"]);
            Assert.Equal(new[] { "a" }, dictionary.Keys.Cast<string>().ToArray());
            Assert.Equal(new[] { 1 }, dictionary.Values.Cast<int>().ToArray());
            dictionary["a"] = 2;
            Assert.Equal(2, dictionary["a"]);
            dictionary.Remove("a");
            Assert.False(dictionary.Contains("a"));
            Assert.False(dictionary.IsFixedSize);
            Assert.False(dictionary.IsReadOnly);
        }

        [Fact]
        public void IDictionaryEnumerator()
        {
            var builder = this.GetBuilder<string, int>();
            var dictionary = (IDictionary)builder;
            dictionary.Add("a", 1);
            var enumerator = dictionary.GetEnumerator();
            Assert.Equal(new DictionaryEntry(null!, 0), enumerator.Current);
            Assert.Null(enumerator.Key);
            Assert.Equal(0, enumerator.Value);
            Assert.Equal(new DictionaryEntry(null!, 0), enumerator.Entry);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(enumerator.Entry, enumerator.Current);
            Assert.Equal(enumerator.Key, enumerator.Entry.Key);
            Assert.Equal(enumerator.Value, enumerator.Entry.Value);
            Assert.Equal("a", enumerator.Key);
            Assert.Equal(1, enumerator.Value);
            Assert.False(enumerator.MoveNext());
            Assert.Equal(new DictionaryEntry(null!, 0), enumerator.Current);
            Assert.Null(enumerator.Key);
            Assert.Equal(0, enumerator.Value);
            Assert.Equal(new DictionaryEntry(null!, 0), enumerator.Entry);
            Assert.False(enumerator.MoveNext());

            enumerator.Reset();
            Assert.Equal(new DictionaryEntry(null!, 0), enumerator.Current);
            Assert.Null(enumerator.Key);
            Assert.Equal(0, enumerator.Value);
            Assert.Equal(new DictionaryEntry(null!, 0), enumerator.Entry);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(enumerator.Key, ((DictionaryEntry)enumerator.Current).Key);
            Assert.Equal(enumerator.Value, ((DictionaryEntry)enumerator.Current).Value);
            Assert.Equal("a", enumerator.Key);
            Assert.Equal(1, enumerator.Value);
            Assert.False(enumerator.MoveNext());
            Assert.Equal(new DictionaryEntry(null!, 0), enumerator.Current);
            Assert.Null(enumerator.Key);
            Assert.Equal(0, enumerator.Value);
            Assert.Equal(new DictionaryEntry(null!, 0), enumerator.Entry);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void ICollectionMembers()
        {
            var builder = this.GetBuilder<string, int>();
            var collection = (ICollection)builder;

            collection.CopyTo(Array.Empty<object>(), 0);

            builder.Add("b", 2);
            Assert.True(builder.ContainsKey("b"));

            var array = new object[builder.Count + 1];
            collection.CopyTo(array, 1);
            Assert.Null(array[0]);
            Assert.Equal(new object?[] { null, new KeyValuePair<string, int>("b", 2) }, array);

            var entryArray = new DictionaryEntry[builder.Count + 1];
            collection.CopyTo(entryArray, 1);
            Assert.Equal(default(DictionaryEntry), entryArray[0]);
            Assert.Equal(new DictionaryEntry[] { default, new DictionaryEntry("b", 2) }, entryArray);

            Assert.False(collection.IsSynchronized);
            Assert.NotNull(collection.SyncRoot);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }

        protected abstract bool TryGetKeyHelper<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey equalKey, out TKey actualKey)
            where TKey : notnull;

        /// <summary>
        /// Gets the Builder for a given dictionary instance.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <returns>The builder.</returns>
        protected abstract IDictionary<TKey, TValue> GetBuilder<TKey, TValue>(IImmutableDictionary<TKey, TValue>? basis = null)
            where TKey : notnull;

        /// <summary>
        /// Gets an empty immutable dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <returns>The immutable dictionary.</returns>
        protected abstract IImmutableDictionary<TKey, TValue> GetEmptyImmutableDictionary<TKey, TValue>()
            where TKey : notnull;

        protected abstract IImmutableDictionary<string, TValue> Empty<TValue>(StringComparer comparer);
    }
}
