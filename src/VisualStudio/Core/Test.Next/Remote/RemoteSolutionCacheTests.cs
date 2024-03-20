// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Remote;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote;

public class RemoteSolutionCacheTests
{
    [Fact]
    public void TestAddAndFind1()
    {
        var cache = new RemoteSolutionCache<int, string>(maxCapacity: 4);
        cache.Add(1, "1");
        Assert.Equal("1", cache.Find(1));
        Assert.Null(cache.Find(2));
    }

    [Fact]
    public void TestAddAndFindOverwrite1()
    {
        var cache = new RemoteSolutionCache<int, string>(maxCapacity: 4);
        cache.Add(1, "1");
        cache.Add(1, "2");
        Assert.Equal("2", cache.Find(1));
        Assert.Null(cache.Find(2));
    }

    [Fact]
    public void TestAddAndFindOverwrite2()
    {
        var cache = new RemoteSolutionCache<int, string>(maxCapacity: 4);
        cache.Add(1, "1");
        cache.Add(2, "2");
        cache.Add(1, "3");
        Assert.Equal("3", cache.Find(1));
        Assert.Equal("2", cache.Find(2));
        Assert.Null(cache.Find(3));
    }

    [Fact]
    public void TestAddAndFindFour()
    {
        var cache = new RemoteSolutionCache<int, string>(maxCapacity: 4);
        cache.Add(1, "1");
        cache.Add(2, "2");
        cache.Add(3, "3");
        cache.Add(4, "4");
        Assert.Equal("1", cache.Find(1));
        Assert.Equal("2", cache.Find(2));
        Assert.Equal("3", cache.Find(3));
        Assert.Equal("4", cache.Find(4));
        Assert.Null(cache.Find(5));
    }

    [Fact]
    public void TestAddAndFindFive_A()
    {
        var cache = new RemoteSolutionCache<int, string>(maxCapacity: 4);
        cache.Add(1, "1");
        cache.Add(2, "2");
        cache.Add(3, "3");
        cache.Add(4, "4");
        cache.Add(5, "5");
        Assert.Null(cache.Find(1));
        Assert.Equal("2", cache.Find(2));
        Assert.Equal("3", cache.Find(3));
        Assert.Equal("4", cache.Find(4));
        Assert.Equal("5", cache.Find(5));
        Assert.Null(cache.Find(6));
    }

    [Fact]
    public void TestAddAndFindFive_B()
    {
        var cache = new RemoteSolutionCache<int, string>(maxCapacity: 4);
        cache.Add(1, "1");
        cache.Add(2, "2");
        cache.Add(3, "3");
        cache.Add(4, "4");
        cache.Add(1, "1"); // re-add.  should ensure that this doesn't fall out of the cache.
        cache.Add(5, "5");
        Assert.Equal("1", cache.Find(1));
        Assert.Null(cache.Find(2));
        Assert.Equal("3", cache.Find(3));
        Assert.Equal("4", cache.Find(4));
        Assert.Equal("5", cache.Find(5));
        Assert.Null(cache.Find(6));
    }

    [Fact]
    public void TestLargeHistory_A()
    {
        var cache = new RemoteSolutionCache<int, string>(maxCapacity: 4, totalHistory: 16);

        for (var i = 0; i < 20; i++)
            cache.Add(i, $"{i}");

        for (var i = 0; i < 20; i++)
        {
            if (i < 16)
            {
                Assert.Null(cache.Find(i));
            }
            else
            {
                Assert.Equal($"{i}", cache.Find(i));
            }
        }
    }

    [Fact]
    public void TestLargeHistory_B()
    {
        var cache = new RemoteSolutionCache<int, string>(maxCapacity: 4, totalHistory: 16);

        for (var i = 0; i < 20; i++)
            cache.Add(i, $"{i}");

        for (var i = 20 - 1; i >= 0; i--)
            cache.Add(i, $"{i}");

        for (var i = 0; i < 20; i++)
        {
            if (i >= 4)
            {
                Assert.Null(cache.Find(i));
            }
            else
            {
                Assert.Equal($"{i}", cache.Find(i));
            }
        }
    }
}
