// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

public class CollectionExtensionsTest
{
    [Fact]
    public void PushReverse1()
    {
        var stack = new Stack<int>();
        stack.PushReverse([1, 2, 3]);
        Assert.Equal(1, stack.Pop());
        Assert.Equal(2, stack.Pop());
        Assert.Equal(3, stack.Pop());
        Assert.Equal(0, stack.Count);
    }

    [Fact]
    public void PushReverse2()
    {
        var stack = new Stack<int>();
        stack.PushReverse(Array.Empty<int>());
        Assert.Equal(0, stack.Count);
    }

    [Fact]
    public void PushReverse3()
    {
        var stack = new Stack<int>();
        stack.Push(3);
        stack.PushReverse([1, 2]);
        Assert.Equal(1, stack.Pop());
        Assert.Equal(2, stack.Pop());
        Assert.Equal(3, stack.Pop());
        Assert.Equal(0, stack.Count);
    }
}
