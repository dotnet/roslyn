// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System;
using Xunit;
using SR = Microsoft.AspNetCore.Razor.Utilities.Shared.Resources.SR;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class HashSetExtensionsTests
{
    [Fact]
    public void CopyTo()
    {
        IEnumerable<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var set = new HashSet<int>(source);

        var destination1 = new int[set.Count - 1];
        var exception = Assert.Throws<ArgumentException>(() => set.CopyTo(destination1.AsSpan()));
        Assert.StartsWith(SR.Destination_is_too_short, exception.Message);

        Span<int> destination2 = stackalloc int[set.Count];
        set.CopyTo(destination2);
        AssertElementsEqual(set, destination2);

        Span<int> destination3 = stackalloc int[set.Count + 1];
        set.CopyTo(destination3);
        AssertElementsEqual(set, destination3);

        static void AssertElementsEqual<T>(HashSet<T> set, ReadOnlySpan<T> span)
        {
            var index = 0;

            foreach (var item in set)
            {
                Assert.Equal(item, span[index++]);
            }
        }
    }
}
