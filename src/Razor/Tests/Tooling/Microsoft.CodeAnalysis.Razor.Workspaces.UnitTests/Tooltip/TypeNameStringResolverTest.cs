// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

public class TypeNameStringResolverTest : ToolingTestBase
{
    public TypeNameStringResolverTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Fact]
    public void TryGetSimpleName_NonPrimitiveType_ReturnsFalse()
    {
        // Arrange
        var typeName = "Microsoft.AspNetCore.SomeType";

        // Act
        var result = TypeNameStringResolver.TryGetSimpleName(typeName, out var resolvedTypeName);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedTypeName);
    }

    [Theory]
    [InlineData("System.Int32", "int")]
    [InlineData("System.Boolean", "bool")]
    [InlineData("System.String", "string")]
    public void GetSimpleName_SimplifiesPrimitiveTypes_ReturnsTrue(string typeName, string expectedTypeName)
    {
        // Arrange

        // Act
        var result = TypeNameStringResolver.TryGetSimpleName(typeName, out var resolvedTypeName);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedTypeName, resolvedTypeName);
    }
}
