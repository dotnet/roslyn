// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest;

public sealed class DequeTests
{
    [Fact]
    public void Empty()
    {
        using var _ = Deque<int>.GetInstance(out var deque);
        Assert.Equal(0, deque.Count);
    }

    [Fact]
    public void AddLast_IncrementsCount()
    {
        using var _ = Deque<int>.GetInstance(out var deque);
        deque.AddLast(10);
        deque.AddLast(20);
        Assert.Equal(2, deque.Count);
    }

    [Fact]
    public void First_And_Last()
    {
        using var _ = Deque<int>.GetInstance(out var deque);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        Assert.Equal(1, deque.First);
        Assert.Equal(3, deque.Last);
    }

    [Fact]
    public void Indexer_ReturnsInOrder()
    {
        using var _ = Deque<int>.GetInstance(out var deque);
        deque.AddLast(10);
        deque.AddLast(20);
        deque.AddLast(30);
        Assert.Equal(10, deque[0]);
        Assert.Equal(20, deque[1]);
        Assert.Equal(30, deque[2]);
    }

    [Fact]
    public void RemoveFirst_ReturnsAndRemovesFront()
    {
        using var _ = Deque<int>.GetInstance(out var deque);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        Assert.Equal(1, deque.RemoveFirst());
        Assert.Equal(2, deque.Count);
        Assert.Equal(2, deque.First);
    }

    [Fact]
    public void RemoveLast_ReturnsAndRemovesBack()
    {
        using var _ = Deque<int>.GetInstance(out var deque);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        Assert.Equal(3, deque.RemoveLast());
        Assert.Equal(2, deque.Count);
        Assert.Equal(2, deque.Last);
    }

    [Fact]
    public void CircularWrapAround()
    {
        using var _ = Deque<int>.GetInstance(out var deque);

        // Fill and partially drain to force _head to advance past the start.
        for (var i = 0; i < 4; i++)
            deque.AddLast(i);

        deque.RemoveFirst();
        deque.RemoveFirst();

        // Now _head is at index 2. Adding more items must wrap around.
        deque.AddLast(10);
        deque.AddLast(11);
        deque.AddLast(12);

        Assert.Equal(5, deque.Count);
        Assert.Equal(2, deque[0]);
        Assert.Equal(3, deque[1]);
        Assert.Equal(10, deque[2]);
        Assert.Equal(11, deque[3]);
        Assert.Equal(12, deque[4]);
    }

    [Fact]
    public void GrowthPreservesOrder()
    {
        using var _ = Deque<int>.GetInstance(out var deque);

        // Force wrap-around then trigger a resize.
        for (var i = 0; i < 3; i++)
            deque.AddLast(i);
        deque.RemoveFirst();
        deque.RemoveFirst();

        // _head is now near the end of the internal buffer. Add enough to force growth.
        for (var i = 10; i < 20; i++)
            deque.AddLast(i);

        Assert.Equal(11, deque.Count);
        Assert.Equal(2, deque[0]);
        for (var i = 1; i < deque.Count; i++)
            Assert.Equal(9 + i, deque[i]);
    }

    [Fact]
    public void InterleavedAddRemove()
    {
        using var _ = Deque<int>.GetInstance(out var deque);

        deque.AddLast(1);
        deque.AddLast(2);
        Assert.Equal(1, deque.RemoveFirst());
        deque.AddLast(3);
        Assert.Equal(3, deque.RemoveLast());
        Assert.Equal(1, deque.Count);
        Assert.Equal(2, deque.First);
        Assert.Equal(2, deque.Last);
    }

    [Fact]
    public void DrainToEmpty_ThenReuse()
    {
        using var _ = Deque<int>.GetInstance(out var deque);

        deque.AddLast(1);
        deque.AddLast(2);
        deque.RemoveFirst();
        deque.RemoveFirst();
        Assert.Equal(0, deque.Count);

        deque.AddLast(99);
        Assert.Equal(1, deque.Count);
        Assert.Equal(99, deque.First);
    }

    [Fact]
    public void Pooling_ReturnsCleanInstance()
    {
        // Use and return one instance, then get another and verify it's clean.
        using (var d1 = Deque<int>.GetInstance(out var first))
        {
            first.AddLast(42);
        }

        using var _ = Deque<int>.GetInstance(out var second);
        Assert.Equal(0, second.Count);
    }

    [Fact]
    public void SingleElement()
    {
        using var _ = Deque<int>.GetInstance(out var deque);
        deque.AddLast(7);
        Assert.Equal(7, deque.First);
        Assert.Equal(7, deque.Last);
        Assert.Equal(7, deque[0]);
        Assert.Equal(7, deque.RemoveFirst());
        Assert.Equal(0, deque.Count);
    }

    [Fact]
    public void RemoveFirst_ThenRemoveLast()
    {
        using var _ = Deque<int>.GetInstance(out var deque);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        deque.RemoveFirst();
        deque.RemoveLast();
        Assert.Equal(1, deque.Count);
        Assert.Equal(2, deque.First);
    }

    [Fact]
    public void RemoveFirst_ClearsReference()
    {
        var reference = RemoveFirst_ClearsReference_Helper();
        reference.AssertReleased();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ObjectReference<object> RemoveFirst_ClearsReference_Helper()
    {
        using var _ = Deque<object>.GetInstance(out var deque);
        var reference = ObjectReference.Create(new object());
        reference.UseReference(r => deque.AddLast(r));
        deque.AddLast(new object());
        deque.RemoveFirst();
        return reference;
    }

    [Fact]
    public void RemoveLast_ClearsReference()
    {
        var reference = RemoveLast_ClearsReference_Helper();
        reference.AssertReleased();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ObjectReference<object> RemoveLast_ClearsReference_Helper()
    {
        using var _ = Deque<object>.GetInstance(out var deque);
        deque.AddLast(new object());
        var reference = ObjectReference.Create(new object());
        reference.UseReference(r => deque.AddLast(r));
        deque.RemoveLast();
        return reference;
    }
}
