// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class ComponentGenericTypePassParsingTest
{
    [Theory]
    [InlineData("C", 0)]
    [InlineData("T", 0)]
    [InlineData("T[]", 1)]
    [InlineData("T[][]", 1)]
    [InlineData("(T, T)[]", 2)]
    [InlineData("(T X, T Y)[]", 2)]
    [InlineData("(T[], T)[]", 2)]
    [InlineData("(T[] X, T Y)[]", 2)]
    [InlineData("C<T>", 1)]
    [InlineData("C<T[]>", 1)]
    [InlineData("C<T[][]>", 1)]
    [InlineData("C<(T, T)[]>", 2)]
    [InlineData("C<(T X, T Y)[]>", 2)]
    [InlineData("C<(T[], T)[]>", 2)]
    [InlineData("C<(T[] X, T Y)[]>", 2)]
    [InlineData("C<D<T>>", 1), WorkItem("https://github.com/dotnet/razor/issues/9631")]
    [InlineData("C<D<T>[]>>", 1)]
    [InlineData("C<D<T[]>>", 1)]
    [InlineData("C<NS.T>", 0)]
    public void ParseTypeParameters(string input, int expectedNumberOfTs)
    {
        // Arrange.
        var feature = new ComponentGenericTypePass.Visitor();

        // Act.
        var parsed = feature.ParseTypeParameters(input);

        // Assert.
        Assert.Equal(Enumerable.Repeat("T", expectedNumberOfTs), parsed);
    }
}
