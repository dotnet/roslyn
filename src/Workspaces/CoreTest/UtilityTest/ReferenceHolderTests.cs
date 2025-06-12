// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class ReferenceHolderTests
{
    [Fact]
    public void SameStrongObjectsEqual()
    {
        var obj = new object();
        var first = ReferenceHolder<object?>.Strong(obj);
        var second = ReferenceHolder<object?>.Strong(obj);

        VerifyEqual(first, second);
    }

    [Fact]
    public void SameWeakObjectsEqual()
    {
        var obj = new object();
        var first = ReferenceHolder<object?>.Weak(obj);
        var second = ReferenceHolder<object?>.Weak(obj);

        // 📝 There is no need for a GC.KeepAlive(obj) here. 'VerifyEqual' will produce correct results whether
        // or not the object is still alive. When the object is alive, the equality path is the same as
        // SameStrongObjectsEqual. When the object is not alive, the equality path is the same as
        // ExpiredSameValuesEqual.
        VerifyEqual(first, second);
    }

    [Fact]
    public void SameMixedObjectsEqual()
    {
        var obj = new object();
        var first = ReferenceHolder<object?>.Strong(obj);
        var second = ReferenceHolder<object?>.Weak(obj);

        VerifyEqual(first, second);
    }

    [Fact]
    public void NullValuesEqual()
    {
        var first = ReferenceHolder<object?>.Strong(null);
        var second = ReferenceHolder<object?>.Weak(null);

        VerifyEqual(first, second);
    }

    [Fact]
    public void ExpiredValueNotEqualToNull()
    {
        var strongNull = ReferenceHolder<object?>.Strong(null);
        var weakNull = ReferenceHolder<object?>.Weak(null);
        var expired = ReferenceHolder<object?>.TestAccessor.ReleasedWeak(hashCode: EqualityComparer<object?>.Default.GetHashCode(null!));

        Assert.Equal(strongNull.GetHashCode(), expired.GetHashCode());
        VerifyNotEqual(strongNull, expired);
        VerifyNotEqual(weakNull, expired);
    }

    [Fact]
    public void ExpiredSameValuesEqual()
    {
        var first = ReferenceHolder<object?>.TestAccessor.ReleasedWeak(hashCode: 1);
        var second = ReferenceHolder<object?>.TestAccessor.ReleasedWeak(hashCode: 1);

        Assert.Null(first.TryGetTarget());
        Assert.Null(second.TryGetTarget());
        VerifyEqual(first, second);
    }

    [Fact]
    public void ExpiredDifferentValuesNotEqual()
    {
        var first = ReferenceHolder<object?>.TestAccessor.ReleasedWeak(hashCode: 1);
        var second = ReferenceHolder<object?>.TestAccessor.ReleasedWeak(hashCode: 2);

        Assert.Null(first.TryGetTarget());
        Assert.Null(second.TryGetTarget());
        VerifyNotEqual(first, second);
    }

    private static void VerifyEqual<T>(ReferenceHolder<T> x, ReferenceHolder<T> y)
        where T : class?
    {
        Assert.Equal(x.GetHashCode(), y.GetHashCode());
        Assert.True(x.Equals(y));
        Assert.True(y.Equals(x));
    }

    private static void VerifyNotEqual<T>(ReferenceHolder<T> x, ReferenceHolder<T> y)
        where T : class?
    {
        Assert.False(x.Equals(y));
        Assert.False(y.Equals(x));
    }
}
