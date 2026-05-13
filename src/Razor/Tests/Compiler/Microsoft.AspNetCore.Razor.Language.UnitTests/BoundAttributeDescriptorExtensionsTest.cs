// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class BoundAttributeDescriptorExtensionsTest
{
    [Fact]
    public void IsDefaultKind_ReturnsTrue_IfKindIsDefault()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("IntProperty")
                .TypeName(typeof(int).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var isDefault = boundAttribute.IsDefaultKind();

        // Assert
        Assert.True(isDefault);
    }

    [Fact]
    public void IsDefaultKind_ReturnsFalse_IfKindIsNotDefault()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ViewComponent, "TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("IntProperty")
                .TypeName(typeof(int).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var isDefault = boundAttribute.IsDefaultKind();

        // Assert
        Assert.False(isDefault);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsTrue_ForStringProperty()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("BoundProp")
                .TypeName(typeof(string).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var result = boundAttribute.ExpectsStringValue("test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsFalse_ForNonStringProperty()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("BoundProp")
                .TypeName(typeof(bool).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var result = boundAttribute.ExpectsStringValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsTrue_StringIndexerAndNameMatch()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("BoundProp")
                .TypeName("System.Collection.Generic.IDictionary<string, string>")
                .AsDictionary("prefix-test-", typeof(string).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var result = boundAttribute.ExpectsStringValue("prefix-test-key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsFalse_StringIndexerAndNameMismatch()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("BoundProp")
                .TypeName("System.Collection.Generic.IDictionary<string, string>")
                .AsDictionary("prefix-test-", typeof(string).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var result = boundAttribute.ExpectsStringValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsTrue_ForBooleanProperty()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("BoundProp")
                .TypeName(typeof(bool).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var result = boundAttribute.ExpectsBooleanValue("test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsFalse_ForNonBooleanProperty()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("BoundProp")
                .TypeName(typeof(int).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var result = boundAttribute.ExpectsBooleanValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsTrue_BooleanIndexerAndNameMatch()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("BoundProp")
                .TypeName("System.Collection.Generic.IDictionary<string, bool>")
                .AsDictionary("prefix-test-", typeof(bool).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var result = boundAttribute.ExpectsBooleanValue("prefix-test-key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsFalse_BooleanIndexerAndNameMismatch()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TypeName("TestTagHelper")
            .BoundAttributeDescriptor(attribute => attribute
                .Name("test")
                .PropertyName("BoundProp")
                .TypeName("System.Collection.Generic.IDictionary<string, bool>")
                .AsDictionary("prefix-test-", typeof(bool).FullName))
            .Build();

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);

        // Act
        var result = boundAttribute.ExpectsBooleanValue("test");

        // Assert
        Assert.False(result);
    }
}
