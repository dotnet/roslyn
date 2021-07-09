// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/ImmutableDictionaryTest.nonnetstandard.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableHashSetBuilderTest : ImmutablesTestBase
    {
        [Fact]
        public void CreateBuilder()
        {
            var builder = ImmutableHashSet.CreateBuilder<string>();
            Assert.Same(EqualityComparer<string>.Default, builder.KeyComparer);

            builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, builder.KeyComparer);
        }

        [Fact]
        public void ToBuilder()
        {
            var builder = ImmutableHashSet<int>.Empty.ToBuilder();
            Assert.True(builder.Add(3));
            Assert.True(builder.Add(5));
            Assert.False(builder.Add(5));
            Assert.Equal(2, builder.Count);
            Assert.True(builder.Contains(3));
            Assert.True(builder.Contains(5));
            Assert.False(builder.Contains(7));

            var set = builder.ToImmutable();
            Assert.Equal(builder.Count, set.Count);
            Assert.True(builder.Add(8));
            Assert.Equal(3, builder.Count);
            Assert.Equal(2, set.Count);
            Assert.True(builder.Contains(8));
            Assert.False(set.Contains(8));
        }

        [Fact]
        public void BuilderFromSet()
        {
            var set = ImmutableHashSet<int>.Empty.Add(1);
            var builder = set.ToBuilder();
            Assert.True(builder.Contains(1));
            Assert.True(builder.Add(3));
            Assert.True(builder.Add(5));
            Assert.False(builder.Add(5));
            Assert.Equal(3, builder.Count);
            Assert.True(builder.Contains(3));
            Assert.True(builder.Contains(5));
            Assert.False(builder.Contains(7));

            var set2 = builder.ToImmutable();
            Assert.Equal(builder.Count, set2.Count);
            Assert.True(set2.Contains(1));
            Assert.True(builder.Add(8));
            Assert.Equal(4, builder.Count);
            Assert.Equal(3, set2.Count);
            Assert.True(builder.Contains(8));

            Assert.False(set.Contains(8));
            Assert.False(set2.Contains(8));
        }

        [Fact]
        public void EnumerateBuilderWhileMutating()
        {
            var builder = ImmutableHashSet<int>.Empty.Union(Enumerable.Range(1, 10)).ToBuilder();
            CollectionAssertAreEquivalent(Enumerable.Range(1, 10).ToArray(), builder.ToArray());

            var enumerator = builder.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            builder.Add(11);

            // Verify that a new enumerator will succeed.
            CollectionAssertAreEquivalent(Enumerable.Range(1, 11).ToArray(), builder.ToArray());

            // Try enumerating further with the previous enumerable now that we've changed the collection.
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            enumerator.Reset();
            enumerator.MoveNext(); // resetting should fix the problem.

            // Verify that by obtaining a new enumerator, we can enumerate all the contents.
            CollectionAssertAreEquivalent(Enumerable.Range(1, 11).ToArray(), builder.ToArray());
        }

        [Fact]
        public void BuilderReusesUnchangedImmutableInstances()
        {
            var collection = ImmutableHashSet<int>.Empty.Add(1);
            var builder = collection.ToBuilder();
            Assert.Same(collection, builder.ToImmutable()); // no changes at all.
            builder.Add(2);

            var newImmutable = builder.ToImmutable();
            Assert.NotSame(collection, newImmutable); // first ToImmutable with changes should be a new instance.
            Assert.Same(newImmutable, builder.ToImmutable()); // second ToImmutable without changes should be the same instance.
        }

        [Fact]
        public void EnumeratorTest()
        {
            var builder = ImmutableHashSet.Create(1).ToBuilder();
            ManuallyEnumerateTest(new[] { 1 }, ((IEnumerable<int>)builder).GetEnumerator());
        }

        [Fact]
        public void Clear()
        {
            var set = ImmutableHashSet.Create(1);
            var builder = set.ToBuilder();
            builder.Clear();
            Assert.Equal(0, builder.Count);
        }

        [Fact]
        public void KeyComparer()
        {
            var builder = ImmutableHashSet.Create("a", "B").ToBuilder();
            Assert.Same(EqualityComparer<string>.Default, builder.KeyComparer);
            Assert.True(builder.Contains("a"));
            Assert.False(builder.Contains("A"));

            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Same(StringComparer.OrdinalIgnoreCase, builder.KeyComparer);
            Assert.Equal(2, builder.Count);
            Assert.True(builder.Contains("a"));
            Assert.True(builder.Contains("A"));

            var set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
        }

        [Fact]
        public void KeyComparerCollisions()
        {
            var builder = ImmutableHashSet.Create("a", "A").ToBuilder();
            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Equal(1, builder.Count);
            Assert.True(builder.Contains("a"));

            var set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
            Assert.Equal(1, set.Count);
            Assert.True(set.Contains("a"));
        }

        [Fact]
        public void KeyComparerEmptyCollection()
        {
            var builder = ImmutableHashSet.Create<string>().ToBuilder();
            Assert.Same(EqualityComparer<string>.Default, builder.KeyComparer);
            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Same(StringComparer.OrdinalIgnoreCase, builder.KeyComparer);
            var set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
        }

        [Fact]
        public void UnionWith()
        {
            var builder = ImmutableHashSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.UnionWith(null));
            builder.UnionWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1, 2, 3, 4 }, builder);
        }

        [Fact]
        public void ExceptWith()
        {
            var builder = ImmutableHashSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.ExceptWith(null));
            builder.ExceptWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1 }, builder);
        }

        [Fact]
        public void SymmetricExceptWith()
        {
            var builder = ImmutableHashSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.SymmetricExceptWith(null));
            builder.SymmetricExceptWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1, 4 }, builder);
        }

        [Fact]
        public void IntersectWith()
        {
            var builder = ImmutableHashSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IntersectWith(null));
            builder.IntersectWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 2, 3 }, builder);
        }

        [Fact]
        public void IsProperSubsetOf()
        {
            var builder = ImmutableHashSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsProperSubsetOf(null));
            Assert.False(builder.IsProperSubsetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsProperSubsetOf(Enumerable.Range(1, 5)));
        }

        [Fact]
        public void IsProperSupersetOf()
        {
            var builder = ImmutableHashSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsProperSupersetOf(null));
            Assert.False(builder.IsProperSupersetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsProperSupersetOf(Enumerable.Range(1, 2)));
        }

        [Fact]
        public void IsSubsetOf()
        {
            var builder = ImmutableHashSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsSubsetOf(null));
            Assert.False(builder.IsSubsetOf(Enumerable.Range(1, 2)));
            Assert.True(builder.IsSubsetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsSubsetOf(Enumerable.Range(1, 5)));
        }

        [Fact]
        public void IsSupersetOf()
        {
            var builder = ImmutableHashSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsSupersetOf(null));
            Assert.False(builder.IsSupersetOf(Enumerable.Range(1, 4)));
            Assert.True(builder.IsSupersetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsSupersetOf(Enumerable.Range(1, 2)));
        }

        [Fact]
        public void Overlaps()
        {
            var builder = ImmutableHashSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.Overlaps(null));
            Assert.True(builder.Overlaps(Enumerable.Range(3, 2)));
            Assert.False(builder.Overlaps(Enumerable.Range(4, 3)));
        }

        [Fact]
        public void Remove()
        {
            var builder = ImmutableHashSet.Create("a").ToBuilder();
            Assert.False(builder.Remove("b"));
            Assert.True(builder.Remove("a"));
        }

        [Fact]
        public void SetEquals()
        {
            var builder = ImmutableHashSet.Create("a").ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.SetEquals(null));
            Assert.False(builder.SetEquals(new[] { "b" }));
            Assert.True(builder.SetEquals(new[] { "a" }));
            Assert.True(builder.SetEquals(builder));
        }

        [Fact]
        public void ICollectionOfTMethods()
        {
            ICollection<string> builder = ImmutableHashSet.Create("a").ToBuilder();
            builder.Add("b");
            Assert.True(builder.Contains("b"));

            var array = new string[3];
            builder.CopyTo(array, 1);
            Assert.Null(array[0]);
            CollectionAssertAreEquivalent(new[] { null, "a", "b" }, array);

            Assert.False(builder.IsReadOnly);

            CollectionAssertAreEquivalent(new[] { "a", "b" }, builder.ToArray()); // tests enumerator
        }

        [Fact]
        public void NullHandling()
        {
            var builder = ImmutableHashSet<string>.Empty.ToBuilder();
            Assert.True(builder.Add(null));
            Assert.False(builder.Add(null));
            Assert.True(builder.Contains(null));
            Assert.True(builder.Remove(null));

            builder.UnionWith(new[] { null, "a" });
            Assert.True(builder.IsSupersetOf(new[] { null, "a" }));
            Assert.True(builder.IsSubsetOf(new[] { null, "a" }));
            Assert.True(builder.IsProperSupersetOf(new[] { default(string) }));
            Assert.True(builder.IsProperSubsetOf(new[] { null, "a", "b" }));

            builder.IntersectWith(new[] { default(string) });
            Assert.Equal(1, builder.Count);

            builder.ExceptWith(new[] { default(string) });
            Assert.False(builder.Remove(null));
        }

        [Fact]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableHashSet.CreateBuilder<int>());
        }

        [Fact]
        public void ToImmutableHashSet()
        {
            ImmutableHashSet<int>.Builder builder = ImmutableHashSet.CreateBuilder<int>();
            builder.Add(1);
            builder.Add(2);
            builder.Add(3);

            var set = builder.ToImmutableSortedSet();
            Assert.True(builder.Contains(1));
            Assert.True(builder.Contains(2));
            Assert.True(builder.Contains(3));

            builder.Remove(3);
            Assert.False(builder.Contains(3));
            Assert.True(set.Contains(3));

            builder.Clear();
            Assert.True(builder.ToImmutableHashSet().IsEmpty);
            Assert.False(set.IsEmpty);

            ImmutableHashSet<int>.Builder nullBuilder = null;
            AssertExtensions.Throws<ArgumentNullException>("builder", () => nullBuilder.ToImmutableHashSet());
        }

        [Fact]
        public void TryGetValue()
        {
            var builder = ImmutableHashSet.Create(1, 2, 3).ToBuilder();
            Assert.True(builder.TryGetValue(2, out _));

            builder = ImmutableHashSet.Create(CustomEqualityComparer.Instance, 1, 2, 3, 4).ToBuilder();
            var existing = 0;
            Assert.True(builder.TryGetValue(5, out existing));
            Assert.Equal(4, existing);
        }

        private class CustomEqualityComparer : IEqualityComparer<int>
        {
            private CustomEqualityComparer()
            {
            }

            public static CustomEqualityComparer Instance { get; } = new CustomEqualityComparer();

            public bool Equals(int x, int y) => x >> 1 == y >> 1;

            public int GetHashCode(int obj) => (obj >> 1).GetHashCode();
        }
    }
}
