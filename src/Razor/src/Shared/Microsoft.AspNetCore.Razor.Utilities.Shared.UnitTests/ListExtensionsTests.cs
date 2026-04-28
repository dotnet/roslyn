// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;
using SR = Microsoft.AspNetCore.Razor.Utilities.Shared.Resources.SR;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class ListExtensionsTests
{
    [Fact]
    public void CopyTo()
    {
        IEnumerable<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var list = new List<int>(source);

        var destination1 = new int[list.Count - 1];
        var exception = Assert.Throws<ArgumentException>(() => list.CopyTo(destination1.AsSpan()));
        Assert.StartsWith(SR.Destination_is_too_short, exception.Message);

        Span<int> destination2 = stackalloc int[list.Count];
        list.CopyTo(destination2);
        AssertElementsEqual(list, destination2);

        Span<int> destination3 = stackalloc int[list.Count + 1];
        list.CopyTo(destination3);
        AssertElementsEqual(list, destination3);

        static void AssertElementsEqual<T>(List<T> list, ReadOnlySpan<T> span)
        {
            var count = list.Count;
            for (var i = 0; i < count; i++)
            {
                Assert.Equal(list[i], span[i]);
            }
        }
    }
}
