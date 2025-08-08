// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ImmutableArrayExtensionsTests
    {
        [Fact]
        public void CreateFrom()
        {
            ImmutableArray<int> a;

            a = ImmutableArray.Create<int>(2);
            Assert.Equal(1, a.Length);
            Assert.Equal(2, a[0]);

            Assert.Throws<ArgumentNullException>(() => ImmutableArray.CreateRange<int>((IEnumerable<int>)null));

            a = ImmutableArray.CreateRange<int>(Enumerable.Range(1, 2));
            Assert.Equal(2, a.Length);
            Assert.Equal(1, a[0]);
            Assert.Equal(2, a[1]);
        }

        [Fact]
        public void ReadOnlyArraysBroken()
        {
            var b = new ArrayBuilder<String>();
            b.Add("hello");
            b.Add("world");
            Assert.Equal("hello", b[0]);
            var a = b.AsImmutable();
            Assert.Equal("hello", a[0]);
            var e = (IEnumerable<string>)a;
            Assert.Equal("hello", e.First());
            var aa = e.ToArray();
            Assert.Equal("hello", aa[0]);

            var first = b.FirstOrDefault((x) => true);
            Assert.Equal("hello", first);

            ImmutableArray<int> nullOrEmpty = default(ImmutableArray<int>);
            Assert.True(nullOrEmpty.IsDefault);
            Assert.True(nullOrEmpty.IsDefaultOrEmpty);
            Assert.Throws<NullReferenceException>(() => nullOrEmpty.IsEmpty);

            nullOrEmpty = ImmutableArray.Create<int>();
            Assert.True(nullOrEmpty.IsEmpty);
        }

        [Fact]
        public void InsertAt()
        {
            var builder = new ArrayBuilder<String>();
            builder.Insert(0, "candy");
            builder.Insert(0, "apple");
            builder.Insert(2, "elephant");
            builder.Insert(2, "drum");
            builder.Insert(1, "banana");
            builder.Insert(0, "$$$");

            Assert.Equal("$$$", builder[0]);
            Assert.Equal("apple", builder[1]);
            Assert.Equal("banana", builder[2]);
            Assert.Equal("candy", builder[3]);
            Assert.Equal("drum", builder[4]);
            Assert.Equal("elephant", builder[5]);
        }

        [Fact]
        public void SetEquals()
        {
            var nul = default(ImmutableArray<int>);
            var empty = ImmutableArray.Create<int>();

            var original = new int[] { 1, 2, 3, }.AsImmutableOrNull();
            var notEqualSubset = new int[] { 1, 2, }.AsImmutableOrNull();
            var notEqualSuperset = new int[] { 1, 2, 3, 4, }.AsImmutableOrNull();
            var equalOrder = new int[] { 2, 1, 3, }.AsImmutableOrNull();
            var equalElements = new int[] { -1, -2, -3 }.AsImmutableOrNull();
            var equalDuplicate = new int[] { 1, 2, 3, 3, -3 }.AsImmutableOrNull();

            IEqualityComparer<int> comparer = new AbsoluteValueComparer();

            Assert.True(nul.SetEquals(nul, comparer));
            Assert.True(empty.SetEquals(empty, comparer));
            Assert.True(original.SetEquals(original, comparer));

            Assert.True(original.SetEquals(equalOrder, comparer));
            Assert.True(original.SetEquals(equalElements, comparer));
            Assert.True(original.SetEquals(equalDuplicate, comparer));

            Assert.False(nul.SetEquals(empty, comparer));
            Assert.False(nul.SetEquals(original, comparer));

            Assert.False(empty.SetEquals(nul, comparer));
            Assert.False(empty.SetEquals(original, comparer));

            Assert.False(original.SetEquals(nul, comparer));
            Assert.False(original.SetEquals(empty, comparer));
            Assert.False(original.SetEquals(notEqualSubset, comparer));
            Assert.False(original.SetEquals(notEqualSuperset, comparer));

            //there's a special case for this in the impl, so cover it specially
            var singleton1 = ImmutableArray.Create<int>(1);
            var singleton2 = ImmutableArray.Create<int>(2);

            Assert.True(singleton1.SetEquals(singleton1, comparer));
            Assert.False(singleton1.SetEquals(singleton2, comparer));
        }

        private class AbsoluteValueComparer : IEqualityComparer<int>
        {
            #region IEqualityComparer<int> Members

            bool IEqualityComparer<int>.Equals(int x, int y)
            {
                return Math.Abs(x) == Math.Abs(y);
            }

            int IEqualityComparer<int>.GetHashCode(int x)
            {
                return Math.Abs(x);
            }
            #endregion
        }

        [Fact]
        public void Single()
        {
            Assert.Throws<NullReferenceException>(() => default(ImmutableArray<int>).Single());
            Assert.Throws<InvalidOperationException>(() => ImmutableArray.Create<int>().Single());
            Assert.Equal(1, ImmutableArray.Create<int>(1).Single());
            Assert.Throws<InvalidOperationException>(() => ImmutableArray.Create<int>(1, 2).Single());

            Func<int, bool> isOdd = x => x % 2 == 1;

            Assert.Throws<NullReferenceException>(() => default(ImmutableArray<int>).Single(isOdd));
            Assert.Throws<InvalidOperationException>(() => ImmutableArray.Create<int>().Single(isOdd));
            Assert.Equal(1, ImmutableArray.Create<int>(1, 2).Single(isOdd));
            Assert.Throws<InvalidOperationException>(() => ImmutableArray.Create<int>(1, 2, 3).Single(isOdd));
        }

        [Fact]
        public void Single_Arg()
        {
            Assert.Throws<NullReferenceException>(() => default(ImmutableArray<int>).Single((_, _) => true, 1));
            Assert.Throws<InvalidOperationException>(() => ImmutableArray.Create<int>().Single((x, a) => x == a, 1));
            Assert.Equal(1, ImmutableArray.Create<int>(1).Single((x, a) => x == a, 1));
            Assert.Throws<InvalidOperationException>(() => ImmutableArray.Create<int>(1, 1).Single((x, a) => x == a, 1));

            Assert.Throws<NullReferenceException>(() => default(ImmutableArray<int>).Single((x, a) => x % a == 1, 2));
            Assert.Throws<InvalidOperationException>(() => ImmutableArray.Create<int>().Single((x, a) => x % a == 1, 2));
            Assert.Equal(1, ImmutableArray.Create<int>(1, 2).Single((x, a) => x % a == 1, 2));
            Assert.Throws<InvalidOperationException>(() => ImmutableArray.Create<int>(1, 2, 3).Single((x, a) => x % a == 1, 2));
        }

        [Fact]
        public void IndexOf()
        {
            var roa = new int[] { 1, 2, 3 }.AsImmutableOrNull();
            Assert.Equal(1, roa.IndexOf(2));
        }

        [Fact]
        public void CompareToNull()
        {
            var roaNull = default(ImmutableArray<int>);
            Assert.True(roaNull == null);
            Assert.False(roaNull != null);
            Assert.True(null == roaNull);
            Assert.False(null != roaNull);

            var copy = roaNull;
            Assert.True(copy == roaNull);
            Assert.False(copy != roaNull);

            var notnull = ImmutableArray.Create<int>();
            Assert.False(notnull == null);
            Assert.True(notnull != null);
            Assert.False(null == notnull);
            Assert.True(null != notnull);
        }

        [Fact]
        public void CopyTo()
        {
            var roa = new int?[] { 1, null, 3 }.AsImmutableOrNull();
            var roaCopy = new int?[4];
            roa.CopyTo(roaCopy, 1);
            Assert.False(roaCopy[0].HasValue);
            Assert.Equal(1, roaCopy[1].Value);
            Assert.False(roaCopy[2].HasValue);
            Assert.Equal(3, roaCopy[3].Value);
        }

        [Fact]
        public void ElementAt()
        {
            var roa = new int?[] { 1, null, 3 }.AsImmutableOrNull();
            Assert.Equal(1, roa.ElementAt(0).Value);
            Assert.False(roa.ElementAt(1).HasValue);
            Assert.Equal(3, roa.ElementAt(2).Value);
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            var roa = new int?[] { 1, 2, 3 }.AsImmutableOrNull();
            Assert.False(roa.ElementAtOrDefault(-1).HasValue);
            Assert.Equal(1, roa.ElementAtOrDefault(0).Value);
            Assert.Equal(2, roa.ElementAtOrDefault(1).Value);
            Assert.Equal(3, roa.ElementAtOrDefault(2).Value);
            Assert.False(roa.ElementAtOrDefault(10).HasValue);
        }

        [Fact]
        public void Last()
        {
            var roa = new int?[] { }.AsImmutableOrNull();
            Assert.Throws<InvalidOperationException>(() => roa.Last());
            roa = new int?[] { 1, 2, 3 }.AsImmutableOrNull();
            Assert.Throws<InvalidOperationException>(() => roa.Last(i => i < 1));
            Assert.Equal(3, roa.Last(i => i > 1));
        }

        [Fact]
        public void LastOrDefault()
        {
            var roa = new int?[] { }.AsImmutableOrNull();
            Assert.False(roa.LastOrDefault().HasValue);
            roa = new int?[] { 1, 2, 3 }.AsImmutableOrNull();
            Assert.False(roa.LastOrDefault(i => i < 1).HasValue);
            Assert.Equal(3, roa.LastOrDefault(i => i > 1));
        }

        [Fact]
        public void SingleOrDefault()
        {
            var roa = new int?[] { }.AsImmutableOrNull();
            Assert.False(roa.SingleOrDefault().HasValue);
            roa = new int?[] { 1 }.AsImmutableOrNull();
            Assert.Equal(1, roa.SingleOrDefault());
            roa = new int?[] { 1, 2, 3 }.AsImmutableOrNull();
            Assert.Throws<InvalidOperationException>(() => roa.SingleOrDefault());
            roa = new int?[] { 1, 2, 3 }.AsImmutableOrNull();
            Assert.Equal(2, roa.SingleOrDefault(i => i == 2));
        }

        [Fact]
        public void Concat()
        {
            var empty = ImmutableArray.Create<int>();
            var a = ImmutableArray.Create<int>(0, 2, 4);

            Assert.Equal(empty, empty.Concat(empty));
            Assert.Equal(a, a.Concat(empty));
            Assert.Equal(a, empty.Concat(a));
            Assert.True(a.Concat(a).SequenceEqual(ImmutableArray.Create<int>(0, 2, 4, 0, 2, 4)));
        }

        [Fact]
        public void AddRange()
        {
            var empty = ImmutableArray.Create<int>();
            var a = ImmutableArray.Create<int>(0, 2, 4);

            Assert.Equal(empty, empty.AddRange(empty));
            Assert.Equal(a, a.AddRange(empty));
            Assert.Equal(a, empty.AddRange(a));
            Assert.True(a.AddRange(a).SequenceEqual(ImmutableArray.Create<int>(0, 2, 4, 0, 2, 4)));

            Assert.Equal(empty, empty.AddRange((IEnumerable<int>)empty));
            Assert.Equal(a, a.AddRange((IEnumerable<int>)empty));
            Assert.True(a.SequenceEqual(empty.AddRange((IEnumerable<int>)a)));
            Assert.True(a.AddRange((IEnumerable<int>)a).SequenceEqual(ImmutableArray.Create<int>(0, 2, 4, 0, 2, 4)));
        }

        [Fact]
        public void InsertRange()
        {
            var empty = ImmutableArray.Create<int>();
            var a = ImmutableArray.Create<int>(0, 2, 4);

            Assert.Equal(empty, empty.InsertRange(0, empty));
            Assert.Equal(a, a.InsertRange(0, empty));
            Assert.Equal(a, a.InsertRange(2, empty));
            Assert.True(a.SequenceEqual(empty.InsertRange(0, a)));
            Assert.True(a.InsertRange(2, a).SequenceEqual(ImmutableArray.Create<int>(0, 2, 0, 2, 4, 4)));
        }

        [Fact]
        public void ToDictionary()
        {
            var roa = new string[] { "one:1", "two:2", "three:3" }.AsImmutableOrNull();

            // Call extension method directly to resolve the ambiguity with EnumerableExtensions.ToDictionary
            var dict = System.Linq.ImmutableArrayExtensions.ToDictionary(roa, s => s.Split(':').First());
            Assert.Equal("one:1", dict["one"]);
            Assert.Equal("two:2", dict["two"]);
            Assert.Equal("three:3", dict["three"]);
            Assert.Throws<KeyNotFoundException>(() => dict["One"]);

            dict = System.Linq.ImmutableArrayExtensions.ToDictionary(roa, s => s.Split(':').First(), StringComparer.OrdinalIgnoreCase);
            Assert.Equal("one:1", dict["one"]);
            Assert.Equal("two:2", dict["Two"]);
            Assert.Equal("three:3", dict["THREE"]);
            Assert.Throws<KeyNotFoundException>(() => dict[""]);

            dict = System.Linq.ImmutableArrayExtensions.ToDictionary(roa, s => s.Split(':').First(), s => s.Split(':').Last());
            Assert.Equal("1", dict["one"]);
            Assert.Equal("2", dict["two"]);
            Assert.Equal("3", dict["three"]);
            Assert.Throws<KeyNotFoundException>(() => dict["THREE"]);

            dict = System.Linq.ImmutableArrayExtensions.ToDictionary(roa, s => s.Split(':').First(), s => s.Split(':').Last(), StringComparer.OrdinalIgnoreCase);
            Assert.Equal("1", dict["onE"]);
            Assert.Equal("2", dict["Two"]);
            Assert.Equal("3", dict["three"]);
            Assert.Throws<KeyNotFoundException>(() => dict["four"]);
        }

        [Fact]
        public void SequenceEqual()
        {
            var a = ImmutableArray.Create<string>("A", "B", "C");
            var b = ImmutableArray.Create<string>("A", "b", "c");
            var c = ImmutableArray.Create<string>("A", "b");
            Assert.False(a.SequenceEqual(b));
            Assert.True(a.SequenceEqual(b, StringComparer.OrdinalIgnoreCase));

            Assert.False(a.SequenceEqual(c));
            Assert.False(a.SequenceEqual(c, StringComparer.OrdinalIgnoreCase));
            Assert.False(c.SequenceEqual(a));

            var r = ImmutableArray.Create<int>(1, 2, 3);
            Assert.True(r.SequenceEqual(Enumerable.Range(1, 3)));
            Assert.False(r.SequenceEqual(Enumerable.Range(1, 2)));
            Assert.False(r.SequenceEqual(Enumerable.Range(1, 4)));

            var s = ImmutableArray.Create<int>(10, 20, 30);
            Assert.True(r.SequenceEqual(s, (x, y) => 10 * x == y));
        }

        [Fact]
        public void SelectAsArray()
        {
            var empty = ImmutableArray.Create<object>();
            Assert.True(empty.SequenceEqual(empty.SelectAsArray(item => item)));
            Assert.True(empty.SequenceEqual(empty.SelectAsArray((item, arg) => item, 1)));
            Assert.True(empty.SequenceEqual(empty.SelectAsArray((item, arg) => arg, 2)));
            Assert.True(empty.SequenceEqual(empty.SelectAsArray((item, index, arg) => item, 1)));
            Assert.True(empty.SequenceEqual(empty.SelectAsArray((item, index, arg) => arg, 2)));
            Assert.True(empty.SequenceEqual(empty.SelectAsArray((item, index, arg) => index, 3)));

            var a = ImmutableArray.Create<int>(3, 2, 1, 0);
            var b = ImmutableArray.Create<int>(0, 1, 2, 3);
            var c = ImmutableArray.Create<int>(2, 2, 2, 2);
            Assert.True(a.SequenceEqual(a.SelectAsArray(item => item)));
            Assert.True(a.SequenceEqual(a.SelectAsArray((item, arg) => item, 1)));
            Assert.True(c.SequenceEqual(a.SelectAsArray((item, arg) => arg, 2)));
            Assert.True(a.SequenceEqual(a.SelectAsArray((item, index, arg) => item, 1)));
            Assert.True(c.SequenceEqual(a.SelectAsArray((item, index, arg) => arg, 2)));
            Assert.True(b.SequenceEqual(a.SelectAsArray((item, index, arg) => index, 3)));

            AssertEx.Equal(new[] { 10 }, ImmutableArray.Create(1).SelectAsArray(i => 10 * i));
            AssertEx.Equal(new[] { 10, 20 }, ImmutableArray.Create(1, 2).SelectAsArray(i => 10 * i));
            AssertEx.Equal(new[] { 10, 20, 30 }, ImmutableArray.Create(1, 2, 3).SelectAsArray(i => 10 * i));
            AssertEx.Equal(new[] { 10, 20, 30, 40 }, ImmutableArray.Create(1, 2, 3, 4).SelectAsArray(i => 10 * i));
            AssertEx.Equal(new[] { 10, 20, 30, 40, 50 }, ImmutableArray.Create(1, 2, 3, 4, 5).SelectAsArray(i => 10 * i));
        }

        [Fact]
        public void SelectAsArrayWithPredicate()
        {
            Assert.Empty(ImmutableArray<object>.Empty.SelectAsArray<object, int>(item => throw null, item => throw null));

            var array = ImmutableArray.Create(1, 2, 3, 4, 5);
            AssertEx.Equal(new[] { 2, 3, 4, 5, 6 }, array.SelectAsArray(item => true, item => item + 1));
            AssertEx.Equal(new[] { 3, 5 }, array.SelectAsArray(item => item % 2 == 0, item => item + 1));
            Assert.Empty(array.SelectAsArray<int, int>(item => item < 0, item => throw null));
        }

        [Fact]
        public void ZipAsArray()
        {
            var empty = ImmutableArray.Create<object>();
            Assert.True(empty.SequenceEqual(empty.ZipAsArray(empty, (item1, item2) => item1)));

            var single1 = ImmutableArray.Create(1);
            var single2 = ImmutableArray.Create(10);
            var single3 = ImmutableArray.Create(11);
            Assert.True(single3.SequenceEqual(single1.ZipAsArray(single2, (item1, item2) => item1 + item2)));

            var pair1 = ImmutableArray.Create(1, 2);
            var pair2 = ImmutableArray.Create(10, 11);
            var pair3 = ImmutableArray.Create(11, 13);
            Assert.True(pair3.SequenceEqual(pair1.ZipAsArray(pair2, (item1, item2) => item1 + item2)));

            var triple1 = ImmutableArray.Create(1, 2, 3);
            var triple2 = ImmutableArray.Create(10, 11, 12);
            var triple3 = ImmutableArray.Create(11, 13, 15);
            Assert.True(triple3.SequenceEqual(triple1.ZipAsArray(triple2, (item1, item2) => item1 + item2)));

            var quad1 = ImmutableArray.Create(1, 2, 3, 4);
            var quad2 = ImmutableArray.Create(10, 11, 12, 13);
            var quad3 = ImmutableArray.Create(11, 13, 15, 17);
            Assert.True(quad3.SequenceEqual(quad1.ZipAsArray(quad2, (item1, item2) => item1 + item2)));

            var quin1 = ImmutableArray.Create(1, 2, 3, 4, 5);
            var quin2 = ImmutableArray.Create(10, 11, 12, 13, 14);
            var quin3 = ImmutableArray.Create(11, 13, 15, 17, 19);
            Assert.True(quin3.SequenceEqual(quin1.ZipAsArray(quin2, (item1, item2) => item1 + item2)));
        }

        [Fact]
        public void ZipAsArrayWithIndex()
        {
            var empty = ImmutableArray.Create<object>();
            Assert.True(empty.SequenceEqual(empty.ZipAsArray(empty, (item1, item2) => item1)));

            var single1 = ImmutableArray.Create(1);
            var single2 = ImmutableArray.Create(10);
            var single3 = ImmutableArray.Create(13);
            Assert.True(single3.SequenceEqual(single1.ZipAsArray(single2, 2, (item1, item2, i, arg) => item1 + item2 + i + arg)));

            var pair1 = ImmutableArray.Create(1, 2);
            var pair2 = ImmutableArray.Create(10, 11);
            var pair3 = ImmutableArray.Create(13, 16);
            Assert.True(pair3.SequenceEqual(pair1.ZipAsArray(pair2, 2, (item1, item2, i, arg) => item1 + item2 + i + arg)));

            var triple1 = ImmutableArray.Create(1, 2, 3);
            var triple2 = ImmutableArray.Create(10, 11, 12);
            var triple3 = ImmutableArray.Create(13, 16, 19);
            Assert.True(triple3.SequenceEqual(triple1.ZipAsArray(triple2, 2, (item1, item2, i, arg) => item1 + item2 + i + arg)));

            var quad1 = ImmutableArray.Create(1, 2, 3, 4);
            var quad2 = ImmutableArray.Create(10, 11, 12, 13);
            var quad3 = ImmutableArray.Create(13, 16, 19, 22);
            Assert.True(quad3.SequenceEqual(quad1.ZipAsArray(quad2, 2, (item1, item2, i, arg) => item1 + item2 + i + arg)));

            var quin1 = ImmutableArray.Create(1, 2, 3, 4, 5);
            var quin2 = ImmutableArray.Create(10, 11, 12, 13, 14);
            var quin3 = ImmutableArray.Create(13, 16, 19, 22, 25);
            Assert.True(quin3.SequenceEqual(quin1.ZipAsArray(quin2, 2, (item1, item2, i, arg) => item1 + item2 + i + arg)));
        }

        [Fact]
        public void WhereAsArray()
        {
            var empty = ImmutableArray.Create<object>();
            // All.
            Assert.Equal(empty, empty.WhereAsArray(o => true));
            // None.
            Assert.Equal(empty, empty.WhereAsArray(o => false));

            var a = ImmutableArray.Create<int>(0, 1, 2, 3, 4, 5);
            // All.
            Assert.Equal(a, a.WhereAsArray(i => true));
            // None.
            Assert.True(a.WhereAsArray(i => false).SequenceEqual(ImmutableArray.Create<int>()));
            // All but the first.
            Assert.True(a.WhereAsArray(i => i > 0).SequenceEqual(ImmutableArray.Create<int>(1, 2, 3, 4, 5)));
            // All but the last.
            Assert.True(a.WhereAsArray(i => i < 5).SequenceEqual(ImmutableArray.Create<int>(0, 1, 2, 3, 4)));
            // First only.
            Assert.True(a.WhereAsArray(i => i == 0).SequenceEqual(ImmutableArray.Create<int>(0)));
            // Last only.
            Assert.True(a.WhereAsArray(i => i == 5).SequenceEqual(ImmutableArray.Create<int>(5)));
            // First half.
            Assert.True(a.WhereAsArray(i => i < 3).SequenceEqual(ImmutableArray.Create<int>(0, 1, 2)));
            // Second half.
            Assert.True(a.WhereAsArray(i => i > 2).SequenceEqual(ImmutableArray.Create<int>(3, 4, 5)));
            // Even.
            Assert.True(a.WhereAsArray(i => i % 2 == 0).SequenceEqual(ImmutableArray.Create<int>(0, 2, 4)));
            // Odd.
            Assert.True(a.WhereAsArray(i => i % 2 == 1).SequenceEqual(ImmutableArray.Create<int>(1, 3, 5)));
        }

        [Fact]
        public void WhereAsArray_WithArg()
        {
            var x = new C();
            Assert.Same(x, ImmutableArray.Create<object>(x).WhereAsArray((o, arg) => o == arg, x)[0]);

            var a = ImmutableArray.Create(0, 1, 2, 3, 4, 5);
            AssertEx.Equal(new[] { 3, 4, 5 }, a.WhereAsArray((i, j) => i > j, 2));
        }

        private class C
        {
        }

        private class D : C
        {
        }

        [Fact]
        public void Casting()
        {
            var arrayOfD = new D[] { new D() }.AsImmutableOrNull();
            var arrayOfC = arrayOfD.Cast<D, C>();
            var arrayOfD2 = arrayOfC.As<D>();

            // the underlying arrays are the same:
            //Assert.True(arrayOfD.Equals(arrayOfC));
            Assert.True(arrayOfD2.Equals(arrayOfD));

            // Trying to cast from base to derived. "As" should return null (default)
            Assert.True(new C[] { new C() }.AsImmutableOrNull().As<D>().IsDefault);
        }

        [Theory]
        [InlineData(new int[0], new[] { 1 })]
        [InlineData(new[] { 1 }, new[] { 3, 1, 1, 3, 4 })]
        [InlineData(new[] { 3, 1 }, new[] { 1, 2, 2, 3, 4 })]
        [InlineData(new[] { 3, 3, 3 }, new[] { 1, 2, 3, 4 })]
        [InlineData(new[] { 2, 4, 1, 2 }, new[] { 1, 2, 3, 4 })]
        public void IsSubsetOf_Strict(int[] array, int[] other)
        {
            Assert.True(array.ToImmutableArray().IsSubsetOf(other.ToImmutableArray()));
            Assert.False(other.ToImmutableArray().IsSubsetOf(array.ToImmutableArray()));
        }

        [Theory]
        [InlineData(new int[0], new int[0])]
        [InlineData(new[] { 1 }, new[] { 1 })]
        [InlineData(new[] { 1, 1 }, new[] { 1, 1, 1, 1 })]
        [InlineData(new[] { 1, 1, 1 }, new[] { 1, 1 })]
        public void IsSubsetOf_Equal(int[] array, int[] other)
        {
            Assert.True(array.ToImmutableArray().IsSubsetOf(other.ToImmutableArray()));
            Assert.True(other.ToImmutableArray().IsSubsetOf(array.ToImmutableArray()));
        }

        public sealed class Comparer<T>(Func<T, T, bool> equals, Func<T, int> hashCode) : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _equals = equals;
            private readonly Func<T, int> _hashCode = hashCode;

            public bool Equals(T x, T y) => _equals(x, y);
            public int GetHashCode(T obj) => _hashCode(obj);
        }

        [Fact]
        public void HasDuplicates()
        {
            var comparer = new Comparer<int>((x, y) => x % 10 == y % 10, x => (x % 10).GetHashCode());

            Assert.False(ImmutableArray<int>.Empty.HasDuplicates());
            Assert.False(ImmutableArray<int>.Empty.HasDuplicates(comparer));
            Assert.False(ImmutableArray<int>.Empty.HasDuplicates(i => i + 1));

            Assert.False(ImmutableArray.Create(1).HasDuplicates());
            Assert.False(ImmutableArray.Create(1).HasDuplicates(comparer));
            Assert.False(ImmutableArray.Create(1).HasDuplicates(i => i + 1));

            Assert.False(ImmutableArray.Create(1, 2).HasDuplicates());
            Assert.False(ImmutableArray.Create(1, 2).HasDuplicates(comparer));
            Assert.False(ImmutableArray.Create(1, 2).HasDuplicates(i => i + 1));

            Assert.True(ImmutableArray.Create(1, 1).HasDuplicates());
            Assert.True(ImmutableArray.Create(11, 1).HasDuplicates(comparer));
            Assert.True(ImmutableArray.Create(1, 3).HasDuplicates(i => i % 2));
            Assert.True(ImmutableArray.Create(11.0, 1.2).HasDuplicates(i => (int)i, comparer));

            Assert.False(ImmutableArray.Create(2, 0, 1, 3).HasDuplicates());
            Assert.False(ImmutableArray.Create(2, 0, 1, 13).HasDuplicates(comparer));
            Assert.False(ImmutableArray.Create(2, 0, 1, 53).HasDuplicates(i => i % 10));
            Assert.False(ImmutableArray.Create(2.3, 0.1, 1.3, 53.4).HasDuplicates(i => (int)i, comparer));

            Assert.True(ImmutableArray.Create(2, 0, 1, 2).HasDuplicates());
            Assert.True(ImmutableArray.Create(2, 0, 1, 12).HasDuplicates(comparer));
            Assert.True(ImmutableArray.Create(2, 0, 1, 52).HasDuplicates(i => i % 10));
            Assert.True(ImmutableArray.Create(2.3, 0.1, 1.3, 52.4).HasDuplicates(i => (int)i, comparer));
        }
    }
}
