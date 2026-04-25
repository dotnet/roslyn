// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperDescriptorExtensionsTest
{
    [Fact]
    public void IsDefaultKind_ReturnsTrue_IfKindIsDefault()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly").Build();

        // Act
        var isDefault = descriptor.IsDefaultKind();

        // Assert
        Assert.True(isDefault);
    }

    [Fact]
    public void IsDefaultKind_ReturnsFalse_IfKindIsNotDefault()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.CreateViewComponent("TestTagHelper", "TestAssembly").Build();

        // Act
        var isDefault = descriptor.IsDefaultKind();

        // Assert
        Assert.False(isDefault);
    }
}
