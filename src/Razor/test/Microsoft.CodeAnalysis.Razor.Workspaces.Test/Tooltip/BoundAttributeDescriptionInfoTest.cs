// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

public class BoundAttributeDescriptionInfoTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void ResolveTagHelperTypeName_ExtractsTypeName_SimpleReturnType()
    {
        // Arrange & Act
        var typeName = BoundAttributeDescriptionInfo.ResolveTagHelperTypeName("SomePropertyName", "string SomeTypeName.SomePropertyName");

        // Assert
        Assert.Equal("SomeTypeName", typeName);
    }

    [Fact]
    public void ResolveTagHelperTypeName_ExtractsTypeName_ComplexReturnType()
    {
        // Arrange & Act
        var typeName = BoundAttributeDescriptionInfo.ResolveTagHelperTypeName("SomePropertyName", "SomeReturnTypeName SomeTypeName.SomePropertyName");

        // Assert
        Assert.Equal("SomeTypeName", typeName);
    }
}
