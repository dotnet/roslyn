// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public class LazyContentTests
{
    [Fact]
    public void Create_ThrowsIfFactoryIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => LazyContent.Create("x", null!));
    }

    [Fact]
    public void Value_ReturnsContentFromFactory()
    {
        // Arrange
        var args = (x: 19, y: 23);
        var lazy = LazyContent.Create(args, static arg => (arg.x + arg.y).ToString());

        // Act
        var content = lazy.Value;

        // Assert
        Assert.NotNull(content);
        Assert.Equal("42", content);
    }

    [Fact]
    public void Value_InvokesFactoryOnlyOnce()
    {
        // Arrange
        var callCount = 0;
        var text = "test";

        var lazy = LazyContent.Create(text, arg =>
        {
            Interlocked.Increment(ref callCount);
            return arg;
        });

        // Act
        var value1 = lazy.Value;
        var value2 = lazy.Value;

        // Assert
        Assert.Equal(value1, value2);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Value_IsThreadSafe()
    {
        const int ThreadCount = 20;

        // Arrange
        var callCount = 0;

        var lazy = LazyContent.Create("thread", arg =>
        {
            Interlocked.Increment(ref callCount);
            Thread.Sleep(10); // Simulate work

            return arg;
        });

        var threads = new Thread[ThreadCount];
        var results = new string?[ThreadCount];

        // Act
        for (var i = 0; i < threads.Length; i++)
        {
            var idx = i; // Capture the current index

            threads[i] = new Thread(() => results[idx] = lazy.Value);
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert that all of the results are equal.
        for (var i = 1; i < results.Length; i++)
        {
            Assert.Equal(results[0], results[i]);
        }

        // Assert that the factory was called only once.
        Assert.Equal(1, callCount);
    }
}
