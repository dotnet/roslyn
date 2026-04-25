// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRequiredAttributeDescriptorBuilderTest
{
    [Fact]
    public void Build_DisplayNameIsName_NameComparisonFullMatch()
    {
        // Arrange
        var builder = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireAttributeDescriptor(attribute => attribute
                    .Name("asp-action", RequiredAttributeNameComparison.FullMatch)));

        // Act
        var tagHelper = builder.Build();
        var attribute = tagHelper.TagMatchingRules[0].Attributes[0];

        // Assert
        Assert.Equal("asp-action", attribute.DisplayName);
    }

    [Fact]
    public void Build_DisplayNameIsNameWithDots_NameComparisonPrefixMatch()
    {
        // Arrange
        var builder = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireAttributeDescriptor(attribute => attribute
                    .Name("asp-route-", RequiredAttributeNameComparison.PrefixMatch)));

        // Act
        var tagHelper = builder.Build();
        var attribute = tagHelper.TagMatchingRules[0].Attributes[0];

        // Assert
        Assert.Equal("asp-route-...", attribute.DisplayName);
    }
}
