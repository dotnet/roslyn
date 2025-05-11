// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections;

public class SegmentedCollectionsMarshalTests
{
    [Fact]
    public void SegmentedDictionary_GetValueRefOrNullRefValueType()
    {
        var dict = new SegmentedDictionary<int, Struct>
        {
            {  1, default },
            {  2, default },
        };

        Assert.Equal(2, dict.Count);

        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        Struct itemVal = dict[1];
        itemVal.Value = 1;
        itemVal.Property = 2;

        // Does not change values in dictionary
        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

        Assert.Equal(3, dict[1].Value);
        Assert.Equal(4, dict[1].Property);

        ref Struct itemRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 2);

        Assert.Equal(0, itemRef.Value);
        Assert.Equal(0, itemRef.Property);

        itemRef.Value = 5;
        itemRef.Property = 6;

        Assert.Equal(5, itemRef.Value);
        Assert.Equal(6, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        itemRef = new Struct() { Value = 7, Property = 8 };

        Assert.Equal(7, itemRef.Value);
        Assert.Equal(8, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        // Check for null refs

        Assert.True(Unsafe.IsNullRef(ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3)));
        Assert.Throws<NullReferenceException>(() => SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3).Value = 9);

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void SegmentedDictionary_GetValueRefOrNullRefClass()
    {
        var dict = new SegmentedDictionary<int, IntAsObject>
        {
            {  1, new IntAsObject() },
            {  2, new IntAsObject() },
        };

        Assert.Equal(2, dict.Count);

        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        IntAsObject itemVal = dict[1];
        itemVal.Value = 1;
        itemVal.Property = 2;

        // Does change values in dictionary
        Assert.Equal(1, dict[1].Value);
        Assert.Equal(2, dict[1].Property);

        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

        Assert.Equal(3, dict[1].Value);
        Assert.Equal(4, dict[1].Property);

        ref IntAsObject itemRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 2);

        Assert.Equal(0, itemRef.Value);
        Assert.Equal(0, itemRef.Property);

        itemRef.Value = 5;
        itemRef.Property = 6;

        Assert.Equal(5, itemRef.Value);
        Assert.Equal(6, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        itemRef = new IntAsObject() { Value = 7, Property = 8 };

        Assert.Equal(7, itemRef.Value);
        Assert.Equal(8, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        // Check for null refs

        Assert.True(Unsafe.IsNullRef(ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3)));
        Assert.Throws<NullReferenceException>(() => SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3).Value = 9);

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void SegmentedDictionary_GetValueRefOrNullRefLinkBreaksOnResize()
    {
        var dict = new SegmentedDictionary<int, Struct>
        {
            {  1, new Struct() },
        };

        Assert.Equal(1, dict.Count);

        ref Struct itemRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1);

        Assert.Equal(0, itemRef.Value);
        Assert.Equal(0, itemRef.Property);

        itemRef.Value = 1;
        itemRef.Property = 2;

        Assert.Equal(1, itemRef.Value);
        Assert.Equal(2, itemRef.Property);
        Assert.Equal(dict[1].Value, itemRef.Value);
        Assert.Equal(dict[1].Property, itemRef.Property);

        // Resize
        dict.EnsureCapacity(100);
        for (int i = 2; i <= 50; i++)
        {
            dict.Add(i, new Struct());
        }

        itemRef.Value = 3;
        itemRef.Property = 4;

        Assert.Equal(3, itemRef.Value);
        Assert.Equal(4, itemRef.Property);

        // Check connection broken
        Assert.NotEqual(dict[1].Value, itemRef.Value);
        Assert.NotEqual(dict[1].Property, itemRef.Property);

        Assert.Equal(50, dict.Count);
    }

    [Fact]
    public void ImmutableSegmentedDictionary_GetValueRefOrNullRefValueType()
    {
        var dict = ImmutableSegmentedDictionary.Create<int, Struct>();
        dict = dict.Add(1, default);
        dict = dict.Add(2, new Struct() { Value = 1, Property = 2 });

        Assert.Equal(2, dict.Count);

        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        Struct itemVal = dict[1];
        itemVal.Value = 1;
        itemVal.Property = 2;

        // Does not change values in dictionary
        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        ref readonly Struct itemRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 2);

        Assert.Equal(1, itemRef.Value);
        Assert.Equal(2, itemRef.Property);

        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        // Check for null refs

        Assert.True(Unsafe.IsNullRef(ref Unsafe.AsRef(in SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3))));
        Assert.Throws<NullReferenceException>(() => SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3).Value);

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void ImmutableSegmentedDictionary_GetValueRefOrNullRefClass()
    {
        var dict = ImmutableSegmentedDictionary.Create<int, IntAsObject>();
        dict = dict.Add(1, new IntAsObject());
        dict = dict.Add(2, new IntAsObject());

        Assert.Equal(2, dict.Count);

        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        IntAsObject itemVal = dict[1];
        itemVal.Value = 1;
        itemVal.Property = 2;

        // Does change values in dictionary
        Assert.Equal(1, dict[1].Value);
        Assert.Equal(2, dict[1].Property);

        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

        Assert.Equal(3, dict[1].Value);
        Assert.Equal(4, dict[1].Property);

        ref readonly IntAsObject itemRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 2);

        Assert.Equal(0, itemRef.Value);
        Assert.Equal(0, itemRef.Property);

        itemRef.Value = 5;
        itemRef.Property = 6;

        Assert.Equal(5, itemRef.Value);
        Assert.Equal(6, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        // Check for null refs

        Assert.True(Unsafe.IsNullRef(ref Unsafe.AsRef(in SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3))));
        Assert.Throws<NullReferenceException>(() => SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3).Value = 9);

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void ImmutableSegmentedDictionary_Builder_GetValueRefOrNullRefValueType()
    {
        var dict = ImmutableSegmentedDictionary.CreateBuilder<int, Struct>();
        dict.Add(1, default);
        dict.Add(2, default);

        Assert.Equal(2, dict.Count);

        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        Struct itemVal = dict[1];
        itemVal.Value = 1;
        itemVal.Property = 2;

        // Does not change values in dictionary
        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

        Assert.Equal(3, dict[1].Value);
        Assert.Equal(4, dict[1].Property);

        ref Struct itemRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 2);

        Assert.Equal(0, itemRef.Value);
        Assert.Equal(0, itemRef.Property);

        itemRef.Value = 5;
        itemRef.Property = 6;

        Assert.Equal(5, itemRef.Value);
        Assert.Equal(6, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        itemRef = new Struct() { Value = 7, Property = 8 };

        Assert.Equal(7, itemRef.Value);
        Assert.Equal(8, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        // Check for null refs

        Assert.True(Unsafe.IsNullRef(ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3)));
        Assert.Throws<NullReferenceException>(() => SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3).Value = 9);

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void ImmutableSegmentedDictionary_Builder_GetValueRefOrNullRefClass()
    {
        var dict = ImmutableSegmentedDictionary.CreateBuilder<int, IntAsObject>();
        dict.Add(1, new IntAsObject());
        dict.Add(2, new IntAsObject());

        Assert.Equal(2, dict.Count);

        Assert.Equal(0, dict[1].Value);
        Assert.Equal(0, dict[1].Property);

        IntAsObject itemVal = dict[1];
        itemVal.Value = 1;
        itemVal.Property = 2;

        // Does change values in dictionary
        Assert.Equal(1, dict[1].Value);
        Assert.Equal(2, dict[1].Property);

        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
        SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

        Assert.Equal(3, dict[1].Value);
        Assert.Equal(4, dict[1].Property);

        ref IntAsObject itemRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 2);

        Assert.Equal(0, itemRef.Value);
        Assert.Equal(0, itemRef.Property);

        itemRef.Value = 5;
        itemRef.Property = 6;

        Assert.Equal(5, itemRef.Value);
        Assert.Equal(6, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        itemRef = new IntAsObject() { Value = 7, Property = 8 };

        Assert.Equal(7, itemRef.Value);
        Assert.Equal(8, itemRef.Property);
        Assert.Equal(dict[2].Value, itemRef.Value);
        Assert.Equal(dict[2].Property, itemRef.Property);

        // Check for null refs

        Assert.True(Unsafe.IsNullRef(ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3)));
        Assert.Throws<NullReferenceException>(() => SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 3).Value = 9);

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void ImmutableSegmentedDictionary_Builder_GetValueRefOrNullRefLinkBreaksOnResize()
    {
        var dict = ImmutableSegmentedDictionary.CreateBuilder<int, Struct>();
        dict.Add(1, new Struct());

        Assert.Equal(1, dict.Count);

        ref Struct itemRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dict, 1);

        Assert.Equal(0, itemRef.Value);
        Assert.Equal(0, itemRef.Property);

        itemRef.Value = 1;
        itemRef.Property = 2;

        Assert.Equal(1, itemRef.Value);
        Assert.Equal(2, itemRef.Property);
        Assert.Equal(dict[1].Value, itemRef.Value);
        Assert.Equal(dict[1].Property, itemRef.Property);

        // Resize
        dict.GetTestAccessor().GetOrCreateMutableDictionary().EnsureCapacity(100);
        for (int i = 2; i <= 50; i++)
        {
            dict.Add(i, new Struct());
        }

        itemRef.Value = 3;
        itemRef.Property = 4;

        Assert.Equal(3, itemRef.Value);
        Assert.Equal(4, itemRef.Property);

        // Check connection broken
        Assert.NotEqual(dict[1].Value, itemRef.Value);
        Assert.NotEqual(dict[1].Property, itemRef.Property);

        Assert.Equal(50, dict.Count);
    }

    [Fact]
    public void AsImmutableSegmentedListFromNullSegmentedList()
    {
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<int>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<int?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<Guid>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<Guid?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<string>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<CustomClass>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<ManagedCustomStruct>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<ManagedCustomStruct?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<UnmanagedCustomStruct>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList<UnmanagedCustomStruct?>(null).IsDefault);
    }

    [Fact]
    public void AsImmutableSegmentedListFromEmptySegmentedList()
    {
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<int>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<int?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<Guid>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<Guid?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<string>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<CustomClass>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<ManagedCustomStruct>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<ManagedCustomStruct?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<UnmanagedCustomStruct>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedList(new SegmentedList<UnmanagedCustomStruct?>(0)).IsEmpty);
    }

    [Fact]
    public void AsImmutableSegmentedListFromExistingSegmentedList()
    {
        static void test<T>()
        {
            SegmentedList<T> list = new SegmentedList<T>(new T[17]);
            ImmutableSegmentedList<T> immutableList = SegmentedCollectionsMarshal.AsImmutableSegmentedList(list);

            Assert.False(immutableList.IsDefault);
            Assert.Equal(17, immutableList.Count);

            ref T expectedRef = ref list.GetTestAccessor().Items[0];
            ref T actualRef = ref Unsafe.AsRef(in immutableList.ItemRef(0));

            Assert.True(Unsafe.AreSame(ref expectedRef, ref actualRef));
        }

        test<int>();
        test<int?>();
        test<Guid>();
        test<Guid?>();
        test<string>();
        test<CustomClass>();
        test<ManagedCustomStruct>();
        test<ManagedCustomStruct?>();
        test<UnmanagedCustomStruct>();
        test<UnmanagedCustomStruct?>();
    }

    [Fact]
    public void AsSegmentedListFromDefaultImmutableSegmentedList()
    {
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<int>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<int?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<Guid>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<Guid?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<string>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<CustomClass>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<ManagedCustomStruct>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<ManagedCustomStruct?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<UnmanagedCustomStruct>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedList<UnmanagedCustomStruct?>(default));
    }

    [Fact]
    public void AsSegmentedListFromEmptyImmutableSegmentedList()
    {
        static void test<T>()
        {
            SegmentedList<T>? list = SegmentedCollectionsMarshal.AsSegmentedList(ImmutableSegmentedList<T>.Empty);

            Assert.NotNull(list);
            Assert.Empty(list);
        }

        test<int>();
        test<int?>();
        test<Guid>();
        test<Guid?>();
        test<string>();
        test<CustomClass>();
        test<ManagedCustomStruct>();
        test<ManagedCustomStruct?>();
        test<UnmanagedCustomStruct>();
        test<UnmanagedCustomStruct?>();
    }

    [Fact]
    public void AsSegmentedListFromConstructedImmutableSegmentedList()
    {
        static void test<T>()
        {
            ImmutableSegmentedList<T> immutableList = ImmutableSegmentedList.Create(new T[17]);
            SegmentedList<T>? list = SegmentedCollectionsMarshal.AsSegmentedList(immutableList);

            AssertEx.NotNull(list);
            Assert.Equal(17, list.Count);

            ref T expectedRef = ref Unsafe.AsRef(in immutableList.ItemRef(0));
            ref T actualRef = ref list.GetTestAccessor().Items[0];

            Assert.True(Unsafe.AreSame(ref expectedRef, ref actualRef));
        }

        test<int>();
        test<int?>();
        test<Guid>();
        test<Guid?>();
        test<string>();
        test<CustomClass>();
        test<ManagedCustomStruct>();
        test<ManagedCustomStruct?>();
        test<UnmanagedCustomStruct>();
        test<UnmanagedCustomStruct?>();
    }

    [Fact]
    public void AsImmutableSegmentedHashSetFromNullSegmentedHashSet()
    {
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<int>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<int?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<Guid>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<Guid?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<string>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<CustomClass>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<ManagedCustomStruct>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<ManagedCustomStruct?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<UnmanagedCustomStruct>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet<UnmanagedCustomStruct?>(null).IsDefault);
    }

    [Fact]
    public void AsImmutableSegmentedHashSetFromEmptySegmentedHashSet()
    {
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<int>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<int?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<Guid>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<Guid?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<string>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<CustomClass>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<ManagedCustomStruct>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<ManagedCustomStruct?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<UnmanagedCustomStruct>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(new SegmentedHashSet<UnmanagedCustomStruct?>(0)).IsEmpty);
    }

    [Fact]
    public void AsImmutableSegmentedHashSetFromExistingSegmentedHashSet()
    {
        static void test<T>(IEnumerable<T> values)
        {
            SegmentedHashSet<T> set = new SegmentedHashSet<T>(values);
            ImmutableSegmentedHashSet<T> immutableHashSet = SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet(set);

            Assert.False(immutableHashSet.IsDefault);
            Assert.Equal(17, immutableHashSet.Count);

            Assert.Same(set, SegmentedCollectionsMarshal.AsSegmentedHashSet(immutableHashSet));

            // Currently SegmentedHashSet<T> does not provide direct ref access to values, so the following tests
            // cannot be implemented in an obvious way.

            //ref T expectedRef = ref set.GetTestAccessor().Items[0];
            //ref T actualRef = ref Unsafe.AsRef(in immutableHashSet.ItemRef(0));

            //Assert.True(Unsafe.AreSame(ref expectedRef, ref actualRef));
        }

        test<int>(Enumerable.Range(0, 17));
        test<int?>(Enumerable.Range(0, 17).Cast<int?>());
        test<Guid>(Enumerable.Range(0, 17).Select(_ => Guid.NewGuid()));
        test<Guid?>(Enumerable.Range(0, 17).Select(_ => (Guid?)Guid.NewGuid()));
        test<string>(Enumerable.Range(0, 17).Select(_ => Guid.NewGuid().ToString()));
        test<CustomClass>(Enumerable.Range(0, 17).Select(_ => new CustomClass()));
        test<ManagedCustomStruct>(Enumerable.Range(0, 17).Select(_ => new ManagedCustomStruct() { Bar = Guid.NewGuid() }));
        test<ManagedCustomStruct?>(Enumerable.Range(0, 17).Select(_ => (ManagedCustomStruct?)new ManagedCustomStruct() { Bar = Guid.NewGuid() }));
        test<UnmanagedCustomStruct>(Enumerable.Range(0, 17).Select(_ => new UnmanagedCustomStruct() { Foo = Guid.NewGuid() }));
        test<UnmanagedCustomStruct?>(Enumerable.Range(0, 17).Select(_ => (UnmanagedCustomStruct?)new UnmanagedCustomStruct() { Foo = Guid.NewGuid() }));
    }

    [Fact]
    public void AsSegmentedHashSetFromDefaultImmutableSegmentedHashSet()
    {
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<int>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<int?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<Guid>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<Guid?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<string>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<CustomClass>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<ManagedCustomStruct>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<ManagedCustomStruct?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<UnmanagedCustomStruct>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedHashSet<UnmanagedCustomStruct?>(default));
    }

    [Fact]
    public void AsSegmentedHashSetFromEmptyImmutableSegmentedHashSet()
    {
        static void test<T>()
        {
            SegmentedHashSet<T>? set = SegmentedCollectionsMarshal.AsSegmentedHashSet(ImmutableSegmentedHashSet<T>.Empty);

            Assert.NotNull(set);
            Assert.Empty(set);
        }

        test<int>();
        test<int?>();
        test<Guid>();
        test<Guid?>();
        test<string>();
        test<CustomClass>();
        test<ManagedCustomStruct>();
        test<ManagedCustomStruct?>();
        test<UnmanagedCustomStruct>();
        test<UnmanagedCustomStruct?>();
    }

    [Fact]
    public void AsSegmentedHashSetFromConstructedImmutableSegmentedHashSet()
    {
        static void test<T>(IEnumerable<T> values)
        {
            ImmutableSegmentedHashSet<T> immutableHashSet = ImmutableSegmentedHashSet.CreateRange(values);
            SegmentedHashSet<T>? set = SegmentedCollectionsMarshal.AsSegmentedHashSet(immutableHashSet);

            AssertEx.NotNull(set);
            Assert.Equal(17, set.Count);

            Assert.Same(set, SegmentedCollectionsMarshal.AsSegmentedHashSet(immutableHashSet));

            // Currently SegmentedHashSet<T> does not provide direct ref access to values, so the following tests
            // cannot be implemented in an obvious way.

            //ref T expectedRef = ref Unsafe.AsRef(in immutableHashSet.ItemRef(0));
            //ref T actualRef = ref set.GetTestAccessor().Items[0];

            //Assert.True(Unsafe.AreSame(ref expectedRef, ref actualRef));
        }

        test<int>(Enumerable.Range(0, 17));
        test<int?>(Enumerable.Range(0, 17).Cast<int?>());
        test<Guid>(Enumerable.Range(0, 17).Select(_ => Guid.NewGuid()));
        test<Guid?>(Enumerable.Range(0, 17).Select(_ => (Guid?)Guid.NewGuid()));
        test<string>(Enumerable.Range(0, 17).Select(_ => Guid.NewGuid().ToString()));
        test<CustomClass>(Enumerable.Range(0, 17).Select(_ => new CustomClass()));
        test<ManagedCustomStruct>(Enumerable.Range(0, 17).Select(_ => new ManagedCustomStruct() { Bar = Guid.NewGuid() }));
        test<ManagedCustomStruct?>(Enumerable.Range(0, 17).Select(_ => (ManagedCustomStruct?)new ManagedCustomStruct() { Bar = Guid.NewGuid() }));
        test<UnmanagedCustomStruct>(Enumerable.Range(0, 17).Select(_ => new UnmanagedCustomStruct() { Foo = Guid.NewGuid() }));
        test<UnmanagedCustomStruct?>(Enumerable.Range(0, 17).Select(_ => (UnmanagedCustomStruct?)new UnmanagedCustomStruct() { Foo = Guid.NewGuid() }));
    }

    [Fact]
    public void AsImmutableSegmentedDictionaryFromNullSegmentedDictionary()
    {
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<int, int>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<int, int?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<Guid, Guid>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<Guid, Guid?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<string, string>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<CustomClass, CustomClass>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<ManagedCustomStruct, ManagedCustomStruct>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<ManagedCustomStruct, ManagedCustomStruct?>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<UnmanagedCustomStruct, UnmanagedCustomStruct>(null).IsDefault);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary<UnmanagedCustomStruct, UnmanagedCustomStruct?>(null).IsDefault);
    }

    [Fact]
    public void AsImmutableSegmentedDictionaryFromEmptySegmentedDictionary()
    {
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<int, int>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<int, int?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<Guid, Guid>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<Guid, Guid?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<string, string>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<CustomClass, CustomClass>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<ManagedCustomStruct, ManagedCustomStruct>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<ManagedCustomStruct, ManagedCustomStruct?>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<UnmanagedCustomStruct, UnmanagedCustomStruct>(0)).IsEmpty);
        Assert.True(SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(new SegmentedDictionary<UnmanagedCustomStruct, UnmanagedCustomStruct?>(0)).IsEmpty);
    }

    [Fact]
    public void AsImmutableSegmentedDictionaryFromExistingSegmentedDictionary()
    {
        static void test<TValue>(Func<TValue> createValue)
        {
            SegmentedDictionary<int, TValue> dictionary = new SegmentedDictionary<int, TValue>(Enumerable.Range(0, 17).Select(x => new KeyValuePair<int, TValue>(x, createValue())));
            ImmutableSegmentedDictionary<int, TValue> immutableDictionary = SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary(dictionary);

            Assert.False(immutableDictionary.IsDefault);
            Assert.Equal(17, immutableDictionary.Count);

            ref TValue expectedRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dictionary, 0);
            ref TValue actualRef = ref Unsafe.AsRef(in SegmentedCollectionsMarshal.GetValueRefOrNullRef(immutableDictionary, 0));

            Assert.True(Unsafe.AreSame(ref expectedRef, ref actualRef));
        }

        test<int>(() => default);
        test<int?>(() => null);
        test<Guid>(() => default);
        test<Guid?>(() => null);
        test<string>(() => "");
        test<CustomClass>(() => new CustomClass());
        test<ManagedCustomStruct>(() => new ManagedCustomStruct());
        test<ManagedCustomStruct?>(() => new ManagedCustomStruct());
        test<UnmanagedCustomStruct>(() => new UnmanagedCustomStruct());
        test<UnmanagedCustomStruct?>(() => new UnmanagedCustomStruct());
    }

    [Fact]
    public void AsSegmentedDictionaryFromDefaultImmutableSegmentedDictionary()
    {
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<int, int>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<int, int?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<Guid, Guid>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<Guid, Guid?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<string, string>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<CustomClass, CustomClass>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<ManagedCustomStruct, ManagedCustomStruct>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<ManagedCustomStruct, ManagedCustomStruct?>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<UnmanagedCustomStruct, UnmanagedCustomStruct>(default));
        Assert.Null(SegmentedCollectionsMarshal.AsSegmentedDictionary<UnmanagedCustomStruct, UnmanagedCustomStruct?>(default));
    }

    [Fact]
    public void AsSegmentedDictionaryFromEmptyImmutableSegmentedDictionary()
    {
        static void test<T>()
        {
            SegmentedDictionary<int, T>? dictionary = SegmentedCollectionsMarshal.AsSegmentedDictionary(ImmutableSegmentedDictionary<int, T>.Empty);

            Assert.NotNull(dictionary);
            Assert.Empty(dictionary);
        }

        test<int>();
        test<int?>();
        test<Guid>();
        test<Guid?>();
        test<string>();
        test<CustomClass>();
        test<ManagedCustomStruct>();
        test<ManagedCustomStruct?>();
        test<UnmanagedCustomStruct>();
        test<UnmanagedCustomStruct?>();
    }

    [Fact]
    public void AsSegmentedDictionaryFromConstructedImmutableSegmentedDictionary()
    {
        static void test<T>(Func<T> createValue)
        {
            ImmutableSegmentedDictionary<int, T> immutableDictionary = ImmutableSegmentedDictionary.CreateRange(Enumerable.Range(0, 17).Select(x => new KeyValuePair<int, T>(x, createValue())));
            SegmentedDictionary<int, T>? dictionary = SegmentedCollectionsMarshal.AsSegmentedDictionary(immutableDictionary);

            AssertEx.NotNull(dictionary);
            Assert.Equal(17, dictionary.Count);

            ref T expectedRef = ref Unsafe.AsRef(in SegmentedCollectionsMarshal.GetValueRefOrNullRef(immutableDictionary, 0));
            ref T actualRef = ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dictionary, 0);

            Assert.True(Unsafe.AreSame(ref expectedRef, ref actualRef));
        }

        test<int>(() => default);
        test<int?>(() => null);
        test<Guid>(() => default);
        test<Guid?>(() => null);
        test<string>(() => "");
        test<CustomClass>(() => new CustomClass());
        test<ManagedCustomStruct>(() => new ManagedCustomStruct());
        test<ManagedCustomStruct?>(() => new ManagedCustomStruct());
        test<UnmanagedCustomStruct>(() => new UnmanagedCustomStruct());
        test<UnmanagedCustomStruct?>(() => new UnmanagedCustomStruct());
    }

    private struct Struct
    {
        public int Value;
        public int Property { get; set; }
    }

    private class IntAsObject
    {
        public int Value;
        public int Property { get; set; }
    }

    public class CustomClass
    {
        public object? Foo;
        public Guid Bar;
    }

    public struct ManagedCustomStruct
    {
        public object? Foo;
        public Guid Bar;
    }

    public struct UnmanagedCustomStruct
    {
        public Guid Foo;
        public int Bar;
    }
}
