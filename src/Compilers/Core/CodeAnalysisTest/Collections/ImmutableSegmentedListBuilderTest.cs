// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableListBuilderTest : ImmutableListTestBase
    {
        [Fact]
        public void CreateBuilder()
        {
            ImmutableList<string>.Builder builder = ImmutableList.CreateBuilder<string>();
            Assert.NotNull(builder);
        }

        [Fact]
        public void ToBuilder()
        {
            var builder = ImmutableList<int>.Empty.ToBuilder();
            builder.Add(3);
            builder.Add(5);
            builder.Add(5);
            Assert.Equal(3, builder.Count);
            Assert.True(builder.Contains(3));
            Assert.True(builder.Contains(5));
            Assert.False(builder.Contains(7));

            var list = builder.ToImmutable();
            Assert.Equal(builder.Count, list.Count);
            builder.Add(8);
            Assert.Equal(4, builder.Count);
            Assert.Equal(3, list.Count);
            Assert.True(builder.Contains(8));
            Assert.False(list.Contains(8));
        }

        [Fact]
        public void BuilderFromList()
        {
            var list = ImmutableList<int>.Empty.Add(1);
            var builder = list.ToBuilder();
            Assert.True(builder.Contains(1));
            builder.Add(3);
            builder.Add(5);
            builder.Add(5);
            Assert.Equal(4, builder.Count);
            Assert.True(builder.Contains(3));
            Assert.True(builder.Contains(5));
            Assert.False(builder.Contains(7));

            var list2 = builder.ToImmutable();
            Assert.Equal(builder.Count, list2.Count);
            Assert.True(list2.Contains(1));
            builder.Add(8);
            Assert.Equal(5, builder.Count);
            Assert.Equal(4, list2.Count);
            Assert.True(builder.Contains(8));

            Assert.False(list.Contains(8));
            Assert.False(list2.Contains(8));
        }

        [Fact]
        public void SeveralChanges()
        {
            var mutable = ImmutableList<int>.Empty.ToBuilder();
            var immutable1 = mutable.ToImmutable();
            Assert.Same(immutable1, mutable.ToImmutable()); //, "The Immutable property getter is creating new objects without any differences.");

            mutable.Add(1);
            var immutable2 = mutable.ToImmutable();
            Assert.NotSame(immutable1, immutable2); //, "Mutating the collection did not reset the Immutable property.");
            Assert.Same(immutable2, mutable.ToImmutable()); //, "The Immutable property getter is creating new objects without any differences.");
            Assert.Equal(1, immutable2.Count);
        }

        [Fact]
        public void EnumerateBuilderWhileMutating()
        {
            var builder = ImmutableList<int>.Empty.AddRange(Enumerable.Range(1, 10)).ToBuilder();
            Assert.Equal(Enumerable.Range(1, 10), builder);

            var enumerator = builder.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            builder.Add(11);

            // Verify that a new enumerator will succeed.
            Assert.Equal(Enumerable.Range(1, 11), builder);

            // Try enumerating further with the previous enumerable now that we've changed the collection.
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            enumerator.Reset();
            enumerator.MoveNext(); // resetting should fix the problem.

            // Verify that by obtaining a new enumerator, we can enumerate all the contents.
            Assert.Equal(Enumerable.Range(1, 11), builder);
        }

        [Fact]
        public void BuilderReusesUnchangedImmutableInstances()
        {
            var collection = ImmutableList<int>.Empty.Add(1);
            var builder = collection.ToBuilder();
            Assert.Same(collection, builder.ToImmutable()); // no changes at all.
            builder.Add(2);

            var newImmutable = builder.ToImmutable();
            Assert.NotSame(collection, newImmutable); // first ToImmutable with changes should be a new instance.
            Assert.Same(newImmutable, builder.ToImmutable()); // second ToImmutable without changes should be the same instance.
        }

        [Fact]
        public void Insert()
        {
            var mutable = ImmutableList<int>.Empty.ToBuilder();
            mutable.Insert(0, 1);
            mutable.Insert(0, 0);
            mutable.Insert(2, 3);
            Assert.Equal(new[] { 0, 1, 3 }, mutable);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable.Insert(-1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable.Insert(4, 0));
        }

        [Fact]
        public void InsertRange()
        {
            var mutable = ImmutableList<int>.Empty.ToBuilder();
            mutable.InsertRange(0, new[] { 1, 4, 5 });
            Assert.Equal(new[] { 1, 4, 5 }, mutable);
            mutable.InsertRange(1, new[] { 2, 3 });
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, mutable);
            mutable.InsertRange(5, new[] { 6 });
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, mutable);
            mutable.InsertRange(5, new int[0]);
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, mutable);

            Assert.Throws<ArgumentOutOfRangeException>(() => mutable.InsertRange(-1, new int[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => mutable.InsertRange(mutable.Count + 1, new int[0]));
        }

        [Fact]
        public void AddRange()
        {
            var mutable = ImmutableList<int>.Empty.ToBuilder();
            mutable.AddRange(new[] { 1, 4, 5 });
            Assert.Equal(new[] { 1, 4, 5 }, mutable);
            mutable.AddRange(new[] { 2, 3 });
            Assert.Equal(new[] { 1, 4, 5, 2, 3 }, mutable);
            mutable.AddRange(new int[0]);
            Assert.Equal(new[] { 1, 4, 5, 2, 3 }, mutable);

            AssertExtensions.Throws<ArgumentNullException>("items", () => mutable.AddRange(null));
        }

        [Fact]
        public void Remove()
        {
            var mutable = ImmutableList<int>.Empty.ToBuilder();
            Assert.False(mutable.Remove(5));

            mutable.Add(1);
            mutable.Add(2);
            mutable.Add(3);
            Assert.True(mutable.Remove(2));
            Assert.Equal(new[] { 1, 3 }, mutable);
            Assert.True(mutable.Remove(1));
            Assert.Equal(new[] { 3 }, mutable);
            Assert.True(mutable.Remove(3));
            Assert.Equal(new int[0], mutable);

            Assert.False(mutable.Remove(5));
        }

        [Fact]
        public void RemoveAllBugTest()
        {
            var builder = ImmutableList.CreateBuilder<int>();
            var elemsToRemove = new[]{0, 1, 2, 3, 4, 5}.ToImmutableHashSet();
            // NOTE: this uses Add instead of AddRange because AddRange doesn't exhibit the same issue due to a different order of tree building.
            // Don't change it without testing with the bug repro from https://github.com/dotnet/runtime/issues/22093.
            foreach (var elem in new[]{0, 1, 2, 3, 4, 5, 6})
                builder.Add(elem);
            builder.RemoveAll(elemsToRemove.Contains);
            Assert.Equal(new[]{ 6 }, builder);
        }

        [Fact]
        public void RemoveAt()
        {
            var mutable = ImmutableList<int>.Empty.ToBuilder();
            mutable.Add(1);
            mutable.Add(2);
            mutable.Add(3);
            mutable.RemoveAt(2);
            Assert.Equal(new[] { 1, 2 }, mutable);
            mutable.RemoveAt(0);
            Assert.Equal(new[] { 2 }, mutable);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable.RemoveAt(1));

            mutable.RemoveAt(0);
            Assert.Equal(new int[0], mutable);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable.RemoveAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable.RemoveAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable.RemoveAt(1));
        }

        [Fact]
        public void Reverse()
        {
            var mutable = ImmutableList.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            mutable.Reverse();
            Assert.Equal(Enumerable.Range(1, 3).Reverse(), mutable);
        }

        [Fact]
        public void Clear()
        {
            var mutable = ImmutableList.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            mutable.Clear();
            Assert.Equal(0, mutable.Count);

            // Do it again for good measure. :)
            mutable.Clear();
            Assert.Equal(0, mutable.Count);
        }

        [Fact]
        public void IsReadOnly()
        {
            ICollection<int> builder = ImmutableList.Create<int>().ToBuilder();
            Assert.False(builder.IsReadOnly);
        }

        [Fact]
        public void Indexer()
        {
            var mutable = ImmutableList.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            Assert.Equal(2, mutable[1]);
            mutable[1] = 5;
            Assert.Equal(5, mutable[1]);
            mutable[0] = -2;
            mutable[2] = -3;
            Assert.Equal(new[] { -2, 5, -3 }, mutable);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable[3] = 4);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable[-1] = 4);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable[3]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => mutable[-1]);
        }

        [Fact]
        public void IndexOf()
        {
            IndexOfTests.IndexOfTest(
                seq => ImmutableList.CreateRange(seq).ToBuilder(),
                (b, v) => b.IndexOf(v),
                (b, v, i) => b.IndexOf(v, i),
                (b, v, i, c) => b.IndexOf(v, i, c),
                (b, v, i, c, eq) => b.IndexOf(v, i, c, eq));
        }

        [Fact]
        public void LastIndexOf()
        {
            IndexOfTests.LastIndexOfTest(
                seq => ImmutableList.CreateRange(seq).ToBuilder(),
                (b, v) => b.LastIndexOf(v),
                (b, v, eq) => b.LastIndexOf(v, b.Count > 0 ? b.Count - 1 : 0, b.Count, eq),
                (b, v, i) => b.LastIndexOf(v, i),
                (b, v, i, c) => b.LastIndexOf(v, i, c),
                (b, v, i, c, eq) => b.LastIndexOf(v, i, c, eq));
        }

        [Fact]
        public void GetEnumeratorExplicit()
        {
            ICollection<int> builder = ImmutableList.Create<int>().ToBuilder();
            var enumerator = builder.GetEnumerator();
            Assert.NotNull(enumerator);
        }

        [Fact]
        public void IsSynchronized()
        {
            ICollection collection = ImmutableList.Create<int>().ToBuilder();
            Assert.False(collection.IsSynchronized);
        }

        [Fact]
        public void IListMembers()
        {
            IList list = ImmutableList.Create<int>().ToBuilder();
            Assert.False(list.IsReadOnly);
            Assert.False(list.IsFixedSize);

            Assert.Equal(0, list.Add(5));
            Assert.Equal(1, list.Add(8));
            Assert.True(list.Contains(5));
            Assert.False(list.Contains(7));
            list.Insert(1, 6);
            Assert.Equal(6, list[1]);
            list.Remove(5);
            list[0] = 9;
            Assert.Equal(new[] { 9, 8 }, list.Cast<int>().ToArray());
            list.Clear();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void IList_Remove_NullArgument()
        {
            this.AssertIListBaseline(RemoveFunc, 1, null);
            this.AssertIListBaseline(RemoveFunc, "item", null);
            this.AssertIListBaseline(RemoveFunc, new int?(1), null);
            this.AssertIListBaseline(RemoveFunc, new int?(), null);
        }

        [Fact]
        public void IList_Remove_ArgTypeMismatch()
        {
            this.AssertIListBaseline(RemoveFunc, "first item", new object());
            this.AssertIListBaseline(RemoveFunc, 1, 1.0);

            this.AssertIListBaseline(RemoveFunc, new int?(1), 1);
            this.AssertIListBaseline(RemoveFunc, new int?(1), new int?(1));
            this.AssertIListBaseline(RemoveFunc, new int?(1), string.Empty);
        }

        [Fact]
        public void IList_Remove_EqualsOverride()
        {
            this.AssertIListBaseline(RemoveFunc, new ProgrammaticEquals(v => v is string), "foo");
            this.AssertIListBaseline(RemoveFunc, new ProgrammaticEquals(v => v is string), 3);
        }

        [Fact]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableList.CreateBuilder<int>());
            ImmutableList<string>.Builder builder = ImmutableList.CreateBuilder<string>();
            builder.Add("One");
            builder.Add("Two");
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(builder);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            string[] items = itemProperty.GetValue(info.Instance) as string[];
            Assert.Equal(builder, items);
        }

        [Fact]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableList.CreateBuilder<string>());
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void ItemRef()
        {
            var list = new[] { 1, 2, 3 }.ToImmutableList();
            var builder = new ImmutableList<int>.Builder(list);

            ref readonly var safeRef = ref builder.ItemRef(1);
            ref var unsafeRef = ref Unsafe.AsRef(safeRef);

            Assert.Equal(2, builder.ItemRef(1));

            unsafeRef = 4;

            Assert.Equal(4, builder.ItemRef(1));
        }

        [Fact]
        public void ItemRef_OutOfBounds()
        {
            var list = new[] { 1, 2, 3 }.ToImmutableList();
            var builder = new ImmutableList<int>.Builder(list);

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ItemRef(5));
        }

        [Fact]
        public void ToImmutableList()
        {
            ImmutableList<int>.Builder builder = ImmutableList.CreateBuilder<int>();
            builder.Add(0);
            builder.Add(1);
            builder.Add(2);

            var list = builder.ToImmutableList();
            Assert.Equal(0, builder[0]);
            Assert.Equal(1, builder[1]);
            Assert.Equal(2, builder[2]);

            builder[1] = 5;
            Assert.Equal(5, builder[1]);
            Assert.Equal(1, list[1]);

            builder.Clear();
            Assert.True(builder.ToImmutableList().IsEmpty);
            Assert.False(list.IsEmpty);

            ImmutableList<int>.Builder nullBuilder = null;
            AssertExtensions.Throws<ArgumentNullException>("builder", () => nullBuilder.ToImmutableList());
        }

        protected override IEnumerable<T> GetEnumerableOf<T>(params T[] contents)
        {
            return ImmutableList<T>.Empty.AddRange(contents).ToBuilder();
        }

        protected override void RemoveAllTestHelper<T>(ImmutableList<T> list, Predicate<T> test)
        {
            var builder = list.ToBuilder();
            var bcl = list.ToList();

            int expected = bcl.RemoveAll(test);
            var actual = builder.RemoveAll(test);
            Assert.Equal(expected, actual);
            Assert.Equal<T>(bcl, builder.ToList());
        }

        protected override void ReverseTestHelper<T>(ImmutableList<T> list, int index, int count)
        {
            var expected = list.ToList();
            expected.Reverse(index, count);
            var builder = list.ToBuilder();
            builder.Reverse(index, count);
            Assert.Equal<T>(expected, builder.ToList());
        }

        internal override IImmutableListQueries<T> GetListQuery<T>(ImmutableList<T> list)
        {
            return list.ToBuilder();
        }

        protected override List<T> SortTestHelper<T>(ImmutableList<T> list)
        {
            var builder = list.ToBuilder();
            builder.Sort();
            return builder.ToImmutable().ToList();
        }

        protected override List<T> SortTestHelper<T>(ImmutableList<T> list, Comparison<T> comparison)
        {
            var builder = list.ToBuilder();
            builder.Sort(comparison);
            return builder.ToImmutable().ToList();
        }

        protected override List<T> SortTestHelper<T>(ImmutableList<T> list, IComparer<T> comparer)
        {
            var builder = list.ToBuilder();
            builder.Sort(comparer);
            return builder.ToImmutable().ToList();
        }

        protected override List<T> SortTestHelper<T>(ImmutableList<T> list, int index, int count, IComparer<T> comparer)
        {
            var builder = list.ToBuilder();
            builder.Sort(index, count, comparer);
            return builder.ToImmutable().ToList();
        }
    }
}
