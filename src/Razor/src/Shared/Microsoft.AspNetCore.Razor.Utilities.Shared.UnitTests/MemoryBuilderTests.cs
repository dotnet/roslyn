// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class MemoryBuilderTests
{
    [Fact]
    public void StartWithDefault()
    {
        using MemoryBuilder<int> builder = default;

        for (var i = 0; i < 1000; i++)
        {
            builder.Append(i);
        }

        var result = builder.AsMemory();

        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(i, result.Span[i]);
        }
    }

    [Fact]
    public void StartWithNew()
    {
        using MemoryBuilder<int> builder = new();

        for (var i = 0; i < 1000; i++)
        {
            builder.Append(i);
        }

        var result = builder.AsMemory();

        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(i, result.Span[i]);
        }
    }

    [Fact]
    public void StartWithInitialCapacity()
    {
        using MemoryBuilder<int> builder = new(1024);

        for (var i = 0; i < 1000; i++)
        {
            builder.Append(i);
        }

        var result = builder.AsMemory();

        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(i, result.Span[i]);
        }
    }

    [Fact]
    public void StartWithInitialArray()
    {
        using MemoryBuilder<int> builder = new(1024);

        for (var i = 0; i < 1000; i++)
        {
            builder.Append(i);
        }

        var result = builder.AsMemory();

        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(i, result.Span[i]);
        }
    }

    [Fact]
    public void AppendChunks()
    {
        using MemoryBuilder<int> builder = default;

        ReadOnlySpan<int> chunk = [1, 2, 3, 4, 5, 6, 7, 8];

        for (var i = 0; i < 1000; i++)
        {
            builder.Append(chunk);
        }

        var result = builder.AsMemory();

        for (var i = 0; i < 1000; i++)
        {
            for (var j = 0; j < chunk.Length; j++)
            {
                Assert.Equal(chunk[j], result.Span[(i * 8) + j]);
            }
        }
    }

    [Fact]
    public void Push_SingleItem_AddsToBuilder()
    {
        using var builder = new MemoryBuilder<int>();

        builder.Push(42);

        Assert.Equal(1, builder.Length);
        Assert.Equal(42, builder[0]);
    }

    [Fact]
    public void Push_MultipleItems_AddsInOrder()
    {
        using var builder = new MemoryBuilder<int>();

        builder.Push(10);
        builder.Push(20);
        builder.Push(30);

        Assert.Equal(3, builder.Length);
        Assert.Equal(10, builder[0]);
        Assert.Equal(20, builder[1]);
        Assert.Equal(30, builder[2]);
    }

    [Fact]
    public void Push_WithInitialCapacity_WorksCorrectly()
    {
        using var builder = new MemoryBuilder<string>(10);

        builder.Push("first");
        builder.Push("second");

        Assert.Equal(2, builder.Length);
        Assert.Equal("first", builder[0]);
        Assert.Equal("second", builder[1]);
    }

    [Fact]
    public void Push_CausesResize_WorksCorrectly()
    {
        using var builder = new MemoryBuilder<int>(2);

        // Fill initial capacity
        builder.Push(1);
        builder.Push(2);
        
        // This should cause a resize
        builder.Push(3);
        builder.Push(4);

        Assert.Equal(4, builder.Length);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void Peek_SingleItem_ReturnsLastItem()
    {
        using var builder = new MemoryBuilder<int>();

        builder.Push(42);

        var result = builder.Peek();

        Assert.Equal(42, result);
        Assert.Equal(1, builder.Length); // Length should remain unchanged
    }

    [Fact]
    public void Peek_MultipleItems_ReturnsLastItem()
    {
        using var builder = new MemoryBuilder<string>();

        builder.Push("first");
        builder.Push("second");
        builder.Push("third");

        var result = builder.Peek();

        Assert.Equal("third", result);
        Assert.Equal(3, builder.Length); // Length should remain unchanged
    }

    [Fact]
    public void Peek_AfterMultiplePushes_ReturnsCorrectItem()
    {
        using var builder = new MemoryBuilder<int>();

        for (var i = 0; i < 100; i++)
        {
            builder.Push(i);
            Assert.Equal(i, builder.Peek());
        }

        Assert.Equal(100, builder.Length);
        Assert.Equal(99, builder.Peek());
    }

    [Fact]
    public void Pop_SingleItem_ReturnsAndRemovesItem()
    {
        using var builder = new MemoryBuilder<int>();

        builder.Push(42);

        var result = builder.Pop();

        Assert.Equal(42, result);
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void Pop_MultipleItems_ReturnsInReverseOrder()
    {
        using var builder = new MemoryBuilder<string>();

        builder.Push("first");
        builder.Push("second");
        builder.Push("third");

        Assert.Equal("third", builder.Pop());
        Assert.Equal(2, builder.Length);

        Assert.Equal("second", builder.Pop());
        Assert.Equal(1, builder.Length);

        Assert.Equal("first", builder.Pop());
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void Pop_Order_WorksCorrectly()
    {
        using var builder = new MemoryBuilder<int>();

        // Push numbers 0-9
        for (var i = 0; i < 10; i++)
        {
            builder.Push(i);
        }

        // Pop should return in reverse order (LIFO)
        for (var i = 9; i >= 0; i--)
        {
            Assert.Equal(i, builder.Pop());
        }

        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TryPop_EmptyBuilder_ReturnsFalse()
    {
        using var builder = new MemoryBuilder<int>();

        var result = builder.TryPop(out var item);

        Assert.False(result);
        Assert.Equal(default, item);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TryPop_SingleItem_ReturnsTrueAndItem()
    {
        using var builder = new MemoryBuilder<string>();

        builder.Push("test");

        var result = builder.TryPop(out var item);

        Assert.True(result);
        Assert.Equal("test", item);
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TryPop_MultipleItems_ReturnsTrueAndLastItem()
    {
        using var builder = new MemoryBuilder<int>();

        builder.Push(10);
        builder.Push(20);
        builder.Push(30);

        var result = builder.TryPop(out var item);

        Assert.True(result);
        Assert.Equal(30, item);
        Assert.Equal(2, builder.Length);
    }

    [Fact]
    public void TryPop_UntilEmpty_WorksCorrectly()
    {
        using var builder = new MemoryBuilder<int>();

        builder.Push(1);
        builder.Push(2);
        builder.Push(3);

        // Pop all items
        Assert.True(builder.TryPop(out var item1));
        Assert.Equal(3, item1);

        Assert.True(builder.TryPop(out var item2));
        Assert.Equal(2, item2);

        Assert.True(builder.TryPop(out var item3));
        Assert.Equal(1, item3);

        // Try to pop from empty builder
        Assert.False(builder.TryPop(out var item4));
        Assert.Equal(default, item4);
    }

    [Fact]
    public void TryPop_WithNullableReferenceTypes_WorksCorrectly()
    {
        using var builder = new MemoryBuilder<string?>();

        builder.Push("test");
        builder.Push(null);

        // Pop null value
        Assert.True(builder.TryPop(out var item1));
        Assert.Null(item1);

        // Pop string value
        Assert.True(builder.TryPop(out var item2));
        Assert.Equal("test", item2);

        // Pop from empty
        Assert.False(builder.TryPop(out var item3));
        Assert.Null(item3);
    }

    [Fact]
    public void StackOperations_MixedUsage_WorksCorrectly()
    {
        using var builder = new MemoryBuilder<int>();

        // Push some items
        builder.Push(1);
        builder.Push(2);
        builder.Push(3);

        // Peek (should not modify)
        Assert.Equal(3, builder.Peek());
        Assert.Equal(3, builder.Length);

        // Pop one item
        Assert.Equal(3, builder.Pop());
        Assert.Equal(2, builder.Length);

        // Push another item
        builder.Push(4);
        Assert.Equal(3, builder.Length);

        // TryPop
        Assert.True(builder.TryPop(out var item));
        Assert.Equal(4, item);
        Assert.Equal(2, builder.Length);

        // Peek again
        Assert.Equal(2, builder.Peek());
    }

    [Fact]
    public void StackOperations_LargeNumberOfItems_WorksCorrectly()
    {
        using var builder = new MemoryBuilder<int>();

        // Push 1000 items
        for (var i = 0; i < 1000; i++)
        {
            builder.Push(i);
        }

        Assert.Equal(1000, builder.Length);
        Assert.Equal(999, builder.Peek());

        // Pop all items in reverse order
        for (var i = 999; i >= 0; i--)
        {
            Assert.True(builder.TryPop(out var item));
            Assert.Equal(i, item);
        }

        Assert.True(builder.IsEmpty);
        Assert.False(builder.TryPop(out _));
    }

    [Fact]
    public void CreateString_EmptyBuilder_ReturnsEmptyString()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            var result = builder.CreateString();

            Assert.Equal(string.Empty, result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_SingleChunk_ReturnsCorrectString()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            var text = "Hello, World!";

            builder.Append(text.AsMemory());

            var result = builder.CreateString();

            Assert.Same(text, result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_SingleChunk_FromSubstring_ReturnsCorrectString()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            var originalText = "Hello, World! This is a test.";
            var substring = originalText.AsMemory(7, 5); // "World"

            builder.Append(substring);

            var result = builder.CreateString();

            Assert.Equal("World", result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_MultipleChunks_ConcatenatesCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            builder.Append("Hello".AsMemory());
            builder.Append(", ".AsMemory());
            builder.Append("World".AsMemory());
            builder.Append("!".AsMemory());

            var result = builder.CreateString();

            Assert.Equal("Hello, World!", result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_MixedChunkTypes_ConcatenatesCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            var fullString = "Original text here";

            // Mix of full strings and substrings
            builder.Append("Start".AsMemory());
            builder.Append(" - ".AsMemory());
            builder.Append(fullString.AsMemory(0, 8)); // "Original"
            builder.Append(" + ".AsMemory());
            builder.Append(fullString.AsMemory(9, 4)); // "text"
            builder.Append(" - End".AsMemory());

            var result = builder.CreateString();

            Assert.Equal("Start - Original + text - End", result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_EmptyChunks_HandlesCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            builder.Append("Hello".AsMemory());
            builder.Append(ReadOnlyMemory<char>.Empty);
            builder.Append(", ".AsMemory());
            builder.Append(ReadOnlyMemory<char>.Empty);
            builder.Append("World".AsMemory());

            var result = builder.CreateString();

            Assert.Equal("Hello, World", result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_OnlyEmptyChunks_ReturnsEmptyString()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            builder.Append(ReadOnlyMemory<char>.Empty);
            builder.Append(ReadOnlyMemory<char>.Empty);
            builder.Append(ReadOnlyMemory<char>.Empty);

            var result = builder.CreateString();

            Assert.Equal(string.Empty, result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_LargeNumberOfChunks_ConcatenatesCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            var expectedLength = 0;

            // Add 100 chunks of "X"
            for (var i = 0; i < 100; i++)
            {
                builder.Append("X".AsMemory());
                expectedLength++;
            }

            var result = builder.CreateString();

            Assert.Equal(expectedLength, result.Length);
            Assert.True(result.All(c => c == 'X'));
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_VaryingChunkSizes_ConcatenatesCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            var chunks = new[]
            {
                "A",           // 1 char
                "BB",          // 2 chars
                "CCC",         // 3 chars
                "DDDD",        // 4 chars
                "EEEEE"        // 5 chars
            };

            foreach (var chunk in chunks)
            {
                builder.Append(chunk.AsMemory());
            }

            var result = builder.CreateString();

            Assert.Equal("ABBCCCDDDDEEEEE", result);
            Assert.Equal(15, result.Length); // 1+2+3+4+5
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_UnicodeCharacters_HandlesCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            builder.Append("Hello ".AsMemory());
            builder.Append("🌍".AsMemory());          // Earth emoji
            builder.Append(" and ".AsMemory());
            builder.Append("🚀".AsMemory());          // Rocket emoji
            builder.Append("!".AsMemory());

            var result = builder.CreateString();

            Assert.Equal("Hello 🌍 and 🚀!", result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_SpecialCharacters_HandlesCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            builder.Append("Line1\n".AsMemory());
            builder.Append("Line2\r\n".AsMemory());
            builder.Append("Tab\t".AsMemory());
            builder.Append("Quote\"".AsMemory());
            builder.Append("Backslash\\".AsMemory());

            var result = builder.CreateString();

            Assert.Equal("Line1\nLine2\r\nTab\tQuote\"Backslash\\", result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_SingleCharacterChunks_ConcatenatesCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            var word = "HELLO";

            foreach (var c in word)
            {
                builder.Append(c.ToString().AsMemory());
            }

            var result = builder.CreateString();

            Assert.Equal(word, result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void CreateString_AfterMultipleOperations_WorksCorrectly()
    {
        var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
        try
        {
            // First, add some content
            builder.Append("Initial".AsMemory());

            // Get initial string
            var initial = builder.CreateString();
            Assert.Equal("Initial", initial);

            // Add more content
            builder.Append(" + More".AsMemory());

            // Get final string
            var final = builder.CreateString();
            Assert.Equal("Initial + More", final);
        }
        finally
        {
            builder.Dispose();
        }
    }
}
