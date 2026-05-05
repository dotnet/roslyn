// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultBoundAttributeDescriptorBuilderTest
{
    [Fact]
    public void DisplayName_SetsDescriptorsDisplayName()
    {
        // Arrange
        var expectedDisplayName = "ExpectedDisplayName";

        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .BoundAttributeDescriptor(builder => builder.DisplayName(expectedDisplayName))
            .Build();

        // Act & Assert
        var descriptor = tagHelper.BoundAttributes[0];

        Assert.Equal(expectedDisplayName, descriptor.DisplayName);
    }

    [Fact]
    public void DisplayName_DefaultsToPropertyLookingDisplayName()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(builder => builder
                .TypeName(typeof(int).FullName)
                .PropertyName("SomeProperty"))
            .Build();

        // Act
        var descriptor = tagHelper.BoundAttributes[0];

        Assert.Equal("int TestTagHelper.SomeProperty", descriptor.DisplayName);
    }
}
