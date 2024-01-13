// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.8/src/libraries/System.Collections.Immutable/tests/ImmutableHashSetBuilderTest.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public partial class ImmutableSegmentedHashSetTest : ImmutableSetTest
    {
        protected override bool IncludesGetHashCodeDerivative
        {
            get { return true; }
        }

        [Fact]
        public void CustomSort()
        {
            CustomSortTestHelper(
                ImmutableSegmentedHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
                false,
                new[] { "apple", "APPLE" },
                new[] { "apple", "APPLE" });
            CustomSortTestHelper(
                ImmutableSegmentedHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                false,
                new[] { "apple", "APPLE" },
                new[] { "apple" });
        }

        [Fact]
        public void ChangeUnorderedEqualityComparer()
        {
            var ordinalSet = ImmutableSegmentedHashSet<string>.Empty
                .WithComparer(StringComparer.Ordinal)
                .Add("apple")
                .Add("APPLE");
            Assert.Equal(2, ordinalSet.Count); // claimed count
            Assert.False(ordinalSet.Contains("aPpLe"));

            var ignoreCaseSet = ordinalSet.WithComparer(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(1, ignoreCaseSet.Count);
            Assert.True(ignoreCaseSet.Contains("aPpLe"));
        }

        [Fact]
        public void ToSortTest()
        {
            var set = ImmutableSegmentedHashSet<string>.Empty
                .Add("apple")
                .Add("APPLE");
            var sorted = System.Collections.Immutable.ImmutableSortedSet.ToImmutableSortedSet(set);
            CollectionAssertAreEquivalent(set.ToList(), sorted.ToList());
        }

        [Fact]
        public void EnumeratorWithHashCollisionsTest()
        {
            var emptySet = EmptyTyped<int>().WithComparer(new BadHasher<int>());
            EnumeratorTestHelper(emptySet, null, 3, 1, 5);
        }

        [Fact]
        public void EnumeratorWithHashCollisionsTest_RefType()
        {
            var emptySet = EmptyTyped<string>().WithComparer(new BadHasher<string>());
            EnumeratorTestHelper(emptySet, null, "c", "a", "e");
        }

        [Fact]
        public void EnumeratorRecyclingMisuse()
        {
            var collection = ImmutableSegmentedHashSet.Create<int>().Add(5);
            var enumerator = collection.GetEnumerator();
            var enumeratorCopy = enumerator;
            Assert.True(enumerator.MoveNext());
            Assert.False(enumerator.MoveNext());
            enumerator.Dispose();
            Assert.False(enumerator.MoveNext());
            enumerator.Reset();
            Assert.Equal(0, enumerator.Current);
            Assert.True(enumeratorCopy.MoveNext());
            enumeratorCopy.Reset();
            Assert.Equal(0, enumeratorCopy.Current);
            enumerator.Dispose(); // double-disposal should not throw
            enumeratorCopy.Dispose();

            // We expect that acquiring a new enumerator will use the same underlying Stack<T> object,
            // but that it will not throw exceptions for the new enumerator.
            enumerator = collection.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.False(enumerator.MoveNext());
            Assert.Equal(0, enumerator.Current);
            enumerator.Dispose();
        }

        [Fact]
        public void Create()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;

            var set = ImmutableSegmentedHashSet.Create<string?>();
            Assert.Equal(0, set.Count);
            Assert.Same(EqualityComparer<string>.Default, set.KeyComparer);

            set = ImmutableSegmentedHashSet.Create<string?>(comparer);
            Assert.Equal(0, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableSegmentedHashSet.Create<string?>("a");
            Assert.Equal(1, set.Count);
            Assert.Same(EqualityComparer<string>.Default, set.KeyComparer);

            set = ImmutableSegmentedHashSet.Create<string?>(comparer, "a");
            Assert.Equal(1, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableSegmentedHashSet.Create<string?>("a", "b");
            Assert.Equal(2, set.Count);
            Assert.Same(EqualityComparer<string>.Default, set.KeyComparer);

            set = ImmutableSegmentedHashSet.Create<string?>(comparer, "a", "b");
            Assert.Equal(2, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableSegmentedHashSet.CreateRange<string?>((IEnumerable<string>)new[] { "a", "b" });
            Assert.Equal(2, set.Count);
            Assert.Same(EqualityComparer<string>.Default, set.KeyComparer);

            set = ImmutableSegmentedHashSet.CreateRange<string?>(comparer, (IEnumerable<string>)new[] { "a", "b" });
            Assert.Equal(2, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableSegmentedHashSet.Create((string?)null);
            Assert.Equal(1, set.Count);

            set = ImmutableSegmentedHashSet.CreateRange(new[] { null, "a", null, "b" });
            Assert.Equal(3, set.Count);
        }

        /// <summary>
        /// Verifies the non-removal of an item that does not belong to the set,
        /// but which happens to have a colliding hash code with another value
        /// that *is* in the set.
        /// </summary>
        [Fact]
        public void RemoveValuesFromCollidedHashCode()
        {
            var set = ImmutableSegmentedHashSet.Create<int>(new BadHasher<int>(), 5, 6);
            Assert.True(IsSame(set, set.Remove(2)));
            var setAfterRemovingFive = set.Remove(5);
            Assert.Equal(1, setAfterRemovingFive.Count);
            Assert.Equal(new[] { 6 }, setAfterRemovingFive);
        }

        /// <summary>
        /// Verifies the non-removal of an item that does not belong to the set,
        /// but which happens to have a colliding hash code with another value
        /// that *is* in the set.
        /// </summary>
        [Fact]
        public void RemoveValuesFromCollidedHashCode_RefType()
        {
            var set = ImmutableSegmentedHashSet.Create<string>(new BadHasher<string>(), "a", "b");
            Assert.True(IsSame(set, set.Remove("c")));
            var setAfterRemovingA = set.Remove("a");
            Assert.Equal(1, setAfterRemovingA.Count);
            Assert.Equal(new[] { "b" }, setAfterRemovingA);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/54716")]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableSegmentedHashSet.Create<string>());
            ImmutableSegmentedHashSet<int> set = ImmutableSegmentedHashSet.Create(1, 2, 3);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(set);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>()?.State == DebuggerBrowsableState.RootHidden);
            int[]? items = itemProperty.GetValue(info.Instance) as int[];
            Assert.Equal(set, items);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/54716")]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableSegmentedHashSet.Create<string>());
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object?)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void SymmetricExceptWithComparerTests()
        {
            var set = ImmutableSegmentedHashSet.Create<string>("a").WithComparer(StringComparer.OrdinalIgnoreCase);
            var otherCollection = new[] { "A" };

            var expectedSet = new HashSet<string>(set, set.KeyComparer);
            expectedSet.SymmetricExceptWith(otherCollection);

            var actualSet = set.SymmetricExcept(otherCollection);
            CollectionAssertAreEquivalent(expectedSet.ToList(), actualSet.ToList());
        }

        protected override System.Collections.Immutable.IImmutableSet<T> Empty<T>()
        {
            return ImmutableSegmentedHashSet<T>.Empty;
        }

        private protected static ImmutableSegmentedHashSet<T> EmptyTyped<T>()
        {
            return ImmutableSegmentedHashSet<T>.Empty;
        }

        protected override ISet<T> EmptyMutable<T>()
        {
            return new HashSet<T>();
        }
    }
}
