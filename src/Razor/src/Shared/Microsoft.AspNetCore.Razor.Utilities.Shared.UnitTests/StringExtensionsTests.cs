// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class StringExtensionsTests
{
    [Fact]
    public void Build_EmptyBuilder_ReturnsEmptyString()
    {
        var result = string.Build("unused", (ref builder, state) =>
        {
            // Don't add anything to the builder
        });

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Build_SingleChunk_ReturnsCorrectString()
    {
        var text = "Hello, World!";

        var result = string.Build(text, (ref builder, state) =>
        {
            builder.Append(state.AsMemory());
        });

        Assert.Same(text, result);
    }

    [Fact]
    public void Build_MultipleChunks_ConcatenatesCorrectly()
    {
        var parts = new[] { "Hello", ", ", "World", "!" };

        var result = string.Build(parts, (ref builder, state) =>
        {
            foreach (var part in state)
            {
                builder.Append(part.AsMemory());
            }
        });

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Build_WithState_PassesStateCorrectly()
    {
        var testData = new { Prefix = "Start: ", Content = "Middle", Suffix = " :End" };

        var result = string.Build(testData, (ref builder, state) =>
        {
            builder.Append(state.Prefix.AsMemory());
            builder.Append(state.Content.AsMemory());
            builder.Append(state.Suffix.AsMemory());
        });

        Assert.Equal("Start: Middle :End", result);
    }

    [Fact]
    public void Build_WithSubstrings_HandlesCorrectly()
    {
        var originalText = "The quick brown fox jumps over the lazy dog";

        var result = string.Build(originalText, (ref builder, state) =>
        {
            // Extract specific words
            builder.Append(state.AsMemory(4, 5));  // "quick"
            builder.Append(" ".AsMemory());
            builder.Append(state.AsMemory(10, 5)); // "brown"
            builder.Append(" ".AsMemory());
            builder.Append(state.AsMemory(16, 3)); // "fox"
        });

        Assert.Equal("quick brown fox", result);
    }

    [Fact]
    public void Build_WithEmptyChunks_HandlesCorrectly()
    {
        var result = string.Build("test", (ref builder, state) =>
        {
            builder.Append("Start".AsMemory());
            builder.Append(ReadOnlyMemory<char>.Empty);
            builder.Append(" - ".AsMemory());
            builder.Append(ReadOnlyMemory<char>.Empty);
            builder.Append("End".AsMemory());
        });

        Assert.Equal("Start - End", result);
    }

    [Fact]
    public void Build_LargeContent_HandlesCorrectly()
    {
        var count = 1000;

        var result = string.Build(count, (ref builder, state) =>
        {
            for (var i = 0; i < state; i++)
            {
                builder.Append("X".AsMemory());
            }
        });

        Assert.Equal(count, result.Length);
        Assert.True(result.All(c => c == 'X'));
    }

    [Fact]
    public void Build_UnicodeContent_HandlesCorrectly()
    {
        var emojis = new[] { "🌍", "🚀", "⭐", "🎉" };

        var result = string.Build(emojis, (ref builder, state) =>
        {
            builder.Append("Unicode: ".AsMemory());
            foreach (var emoji in state)
            {
                builder.Append(emoji.AsMemory());
                builder.Append(" ".AsMemory());
            }
        });

        Assert.Equal("Unicode: 🌍 🚀 ⭐ 🎉 ", result);
    }

    [Fact]
    public void TryBuild_ReturnsTrue_ReturnsString()
    {
        var result = string.TryBuild("Hello", (ref builder, state) =>
        {
            builder.Append(state.AsMemory());
            builder.Append(" World!".AsMemory());
            return true; // Success
        });

        Assert.NotNull(result);
        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public void TryBuild_ReturnsFalse_ReturnsNull()
    {
        var result = string.TryBuild("test", (ref builder, state) =>
        {
            builder.Append(state.AsMemory());
            return false; // Failure
        });

        Assert.Null(result);
    }

    [Fact]
    public void TryBuild_ConditionalLogic_WorksCorrectly()
    {
        // Test with valid input
        var validResult = string.TryBuild("ValidInput", (ref builder, state) =>
        {
            if (state.StartsWith("Valid"))
            {
                builder.Append("Processed: ".AsMemory());
                builder.Append(state.AsMemory());
                return true;
            }

            return false;
        });

        Assert.NotNull(validResult);
        Assert.Equal("Processed: ValidInput", validResult);

        // Test with invalid input
        var invalidResult = string.TryBuild("InvalidInput", (ref builder, state) =>
        {
            if (state.StartsWith("Valid"))
            {
                builder.Append("Processed: ".AsMemory());
                builder.Append(state.AsMemory());
                return true;
            }

            return false;
        });

        Assert.Null(invalidResult);
    }

    [Fact]
    public void TryBuild_EmptyBuilderOnSuccess_ReturnsEmptyString()
    {
        var result = string.TryBuild("test", (ref builder, state) =>
        {
            // Don't add anything to builder but return success
            return true;
        });

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TryBuild_EmptyBuilderOnFailure_ReturnsNull()
    {
        var result = string.TryBuild("test", (ref builder, state) =>
        {
            // Don't add anything to builder and return failure
            return false;
        });

        Assert.Null(result);
    }

    [Fact]
    public void TryBuild_WithComplexState_WorksCorrectly()
    {
        var testData = new
        {
            ShouldSucceed = true,
            MaxLength = 20,
            Content = "This is a test message that is longer than 20 characters"
        };

        var result = string.TryBuild(testData, (ref builder, state) =>
        {
            if (!state.ShouldSucceed)
            {
                return false;
            }

            var content = state.Content;
            var maxLength = state.MaxLength;

            if (content.Length > maxLength)
            {
                builder.Append(content.AsMemory(0, maxLength));
                builder.Append("...".AsMemory());
            }
            else
            {
                builder.Append(content.AsMemory());
            }

            return true;
        });

        Assert.NotNull(result);
        Assert.Equal("This is a test messa...", result);
    }

    [Fact]
    public void TryBuild_WithValidation_ReturnsNullOnInvalidInput()
    {
        var result = string.TryBuild("", (ref builder, state) =>
        {
            if (string.IsNullOrEmpty(state))
            {
                return false;
            }

            builder.Append("Valid: ".AsMemory());
            builder.Append(state.AsMemory());
            return true;
        });

        Assert.Null(result);
    }

    [Fact]
    public void TryBuild_WithMultipleChunks_ReturnsStringOnSuccess()
    {
        var words = new[] { "one", "two", "three", "four" };

        var result = string.TryBuild(words, (ref builder, state) =>
        {
            if (state.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < state.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append("-".AsMemory());
                }

                builder.Append(state[i].AsMemory());
            }

            return true;
        });

        Assert.NotNull(result);
        Assert.Equal("one-two-three-four", result);
    }

    [Fact]
    public void TryBuild_WithUnicodeAndFailure_ReturnsNull()
    {
        var emojis = new[] { "🌍", "🚀", "⭐" };

        var result = string.TryBuild(emojis, (ref builder, state) =>
        {
            // Add content but then fail
            foreach (var emoji in state)
            {
                builder.Append(emoji.AsMemory());
            }

            // Simulate failure condition
            return false;
        });

        Assert.Null(result);
    }

    [Fact]
    public void TryBuild_LargeContentOnFailure_ReturnsNull()
    {
        var count = 10000;

        var result = string.TryBuild(count, (ref builder, state) =>
        {
            // Build large content but then fail
            for (var i = 0; i < state; i++)
            {
                builder.Append("A".AsMemory());
            }

            return false; // Simulate failure
        });

        Assert.Null(result);
    }

    [Fact]
    public void Build_And_TryBuild_ProduceSameResultOnSuccess()
    {
        var testInput = "Test input";

        var buildResult = string.Build(testInput, (ref builder, state) =>
        {
            builder.Append("Result: ".AsMemory());
            builder.Append(state.AsMemory());
        });

        var tryBuildResult = string.TryBuild(testInput, (ref builder, state) =>
        {
            builder.Append("Result: ".AsMemory());
            builder.Append(state.AsMemory());
            return true; // Success
        });

        Assert.Equal(buildResult, tryBuildResult);
    }

    [Fact]
    public void MemoryBuilder_IsProperlyDisposed_EvenOnException()
    {
        // This test ensures that even if an exception occurs, the MemoryBuilder is disposed
        // We can't directly test disposal, but we can ensure exceptions are propagated correctly

        Assert.Throws<InvalidOperationException>(() =>
        {
            string.Build("test", (ref builder, state) =>
            {
                builder.Append("Some content".AsMemory());
                throw new InvalidOperationException("Test exception");
            });
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            string.TryBuild("test", (ref builder, state) =>
            {
                builder.Append("Some content".AsMemory());
                throw new InvalidOperationException("Test exception");
            });
        });
    }
}
