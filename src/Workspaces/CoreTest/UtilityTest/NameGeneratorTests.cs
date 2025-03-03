// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class NameGeneratorTests
{
    [Fact]
    public void TestGenerateUniqueName()
    {
        var a = NameGenerator.GenerateUniqueName("ABC", "txt", _ => true);
        Assert.True(a.StartsWith("ABC", StringComparison.Ordinal));
        Assert.True(a.EndsWith(".txt", StringComparison.Ordinal));
        Assert.False(a.EndsWith("..txt", StringComparison.Ordinal));

        var b = NameGenerator.GenerateUniqueName("ABC", ".txt", _ => true);
        Assert.True(b.StartsWith("ABC", StringComparison.Ordinal));
        Assert.True(b.EndsWith(".txt", StringComparison.Ordinal));
        Assert.False(b.EndsWith("..txt", StringComparison.Ordinal));

        var c = NameGenerator.GenerateUniqueName("ABC", "\u0640.txt", _ => true);
        Assert.True(c.StartsWith("ABC", StringComparison.Ordinal));
        Assert.True(c.EndsWith(".\u0640.txt", StringComparison.Ordinal));
        Assert.False(c.EndsWith("..txt", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("test", new[] { "a", "b", "c" }, "test")]
    [InlineData("test", new[] { "test", "test", "test" }, "test1")]
    [InlineData("test", new[] { "test", "test", "Test1" }, "test1")]
    [InlineData("test", new[] { "test", "test1", "test2" }, "test3")]
    [InlineData("test", new[] { "test", "test1", "test3" }, "test2")]
    public void EnsureUniqueness(string baseName, string[] reservedNames, string expectedResult)
    {
        var result = NameGenerator.EnsureUniqueness(baseName, reservedNames);
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("Test", new[] { "a", "b", "c" }, "Test")]
    [InlineData("Test", new[] { "test", "test", "test" }, "Test1")]
    [InlineData("Test", new[] { "test", "test", "Test1" }, "Test2")]
    [InlineData("Test", new[] { "test", "test1", "test2" }, "Test3")]
    [InlineData("Test", new[] { "test", "test1", "test3" }, "Test2")]
    public void EnsureUniquenessCaseInsensitive(string baseName, string[] reservedNames, string expectedResult)
    {
        var result = NameGenerator.EnsureUniqueness(baseName, reservedNames, isCaseSensitive: false);
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(new[] { "test", "Test", "test", "Test" }, new[] { "test1", "Test1", "test2", "Test2" })]
    public void EnsureUniquenessInPlaceCaseSensitive(string[] names, string[] expectedResult)
    {
        VerifyEnsureUniquenessInPlace(names, isFixed: null, canUse: null, isCaseSensitive: true, expectedResult);
    }

    [Theory]
    [InlineData(new[] { "test", "Test", "test", "Test" }, new[] { "test1", "Test2", "test3", "Test4" })]
    public void EnsureUniquenessInPlaceNotCaseSensitive(string[] names, string[] expectedResult)
    {
        VerifyEnsureUniquenessInPlace(names, isFixed: null, canUse: null, isCaseSensitive: false, expectedResult);
    }

    [Theory]
    [InlineData(new[] { "test", "test", "test" }, new[] { "test", "test", "test" })]
    public void EnsureUniquenessInPlaceAllFixed(string[] names, string[] expectedResult)
    {
        var isFixed = Enumerable.Repeat(true, names.Length).ToArray();

        VerifyEnsureUniquenessInPlace(names, isFixed, canUse: null, isCaseSensitive: true, expectedResult);
    }

    [Theory]
    [InlineData(new[] { "test", "test", "test" }, new[] { "test1", "test2", "test3" })]
    public void EnsureUniquenessInPlaceNoneFixed(string[] names, string[] expectedResult)
    {
        VerifyEnsureUniquenessInPlace(names, isFixed: null, canUse: null, isCaseSensitive: true, expectedResult);
    }

    [Theory]
    [InlineData(new[] { "test", "test", "test" }, new[] { "test10", "test11", "test12" })]
    public void EnsureUniquenessInPlaceCanUseNotIncludingFirst10(string[] names, string[] expectedResult)
    {
        Func<string, bool> canUse = (s) => s.Length > 5;

        VerifyEnsureUniquenessInPlace(names, isFixed: null, canUse, isCaseSensitive: true, expectedResult);
    }

    private static void VerifyEnsureUniquenessInPlace(string[] names, bool[]? isFixed, Func<string, bool>? canUse, bool isCaseSensitive, string[] expectedResult)
    {
        using var _1 = ArrayBuilder<string>.GetInstance(out var namesBuilder);
        namesBuilder.AddRange(names);

        using var _2 = ArrayBuilder<bool>.GetInstance(out var isFixedBuilder);
        isFixedBuilder.AddRange(isFixed ?? Enumerable.Repeat(false, names.Length));

        NameGenerator.EnsureUniquenessInPlace(namesBuilder, isFixedBuilder, canUse, isCaseSensitive);

        Assert.True(Enumerable.SequenceEqual(expectedResult, namesBuilder));
    }
}
