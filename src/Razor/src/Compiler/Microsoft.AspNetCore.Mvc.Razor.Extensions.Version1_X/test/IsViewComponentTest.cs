// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public class IsViewComponentTest
{
    private CSharpCompilation Compilation { get; }

    // In practice MVC will provide a marker attribute for ViewComponents. To prevent a circular reference between MVC and Razor
    // we can use a test class as a marker.
    private INamedTypeSymbol TestViewComponentAttributeSymbol { get; }
    private INamedTypeSymbol TestNonViewComponentAttributeSymbol { get; }

    public IsViewComponentTest()
    {
        var assembly = typeof(IsViewComponentTest).GetTypeInfo().Assembly;
        Compilation = TestCompilation.Create(assembly);
        TestViewComponentAttributeSymbol = Compilation.GetTypeByMetadataName(typeof(TestViewComponentAttribute).FullName.AssumeNotNull()).AssumeNotNull();
        TestNonViewComponentAttributeSymbol = Compilation.GetTypeByMetadataName(typeof(TestNonViewComponentAttribute).FullName.AssumeNotNull()).AssumeNotNull();
    }

    [Fact]
    public void IsViewComponent_PlainViewComponent_ReturnsTrue()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_PlainViewComponent).FullName.AssumeNotNull());
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isViewComponent = tagHelperSymbol.IsViewComponent(TestViewComponentAttributeSymbol, TestNonViewComponentAttributeSymbol);

        // Assert
        Assert.True(isViewComponent);
    }

    [Fact]
    public void IsViewComponent_DecoratedViewComponent_ReturnsTrue()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_DecoratedVC).FullName.AssumeNotNull());
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isViewComponent = tagHelperSymbol.IsViewComponent(TestViewComponentAttributeSymbol, TestNonViewComponentAttributeSymbol);

        // Assert
        Assert.True(isViewComponent);
    }

    [Fact]
    public void IsViewComponent_InheritedViewComponent_ReturnsTrue()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_InheritedVC).FullName.AssumeNotNull());
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isViewComponent = tagHelperSymbol.IsViewComponent(TestViewComponentAttributeSymbol, TestNonViewComponentAttributeSymbol);

        // Assert
        Assert.True(isViewComponent);
    }

    [Fact]
    public void IsViewComponent_AbstractViewComponent_ReturnsFalse()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_AbstractViewComponent).FullName.AssumeNotNull());
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isViewComponent = tagHelperSymbol.IsViewComponent(TestViewComponentAttributeSymbol, TestNonViewComponentAttributeSymbol);

        // Assert
        Assert.False(isViewComponent);
    }

    [Fact]
    public void IsViewComponent_GenericViewComponent_ReturnsFalse()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_GenericViewComponent<>).FullName.AssumeNotNull());
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isViewComponent = tagHelperSymbol.IsViewComponent(TestViewComponentAttributeSymbol, TestNonViewComponentAttributeSymbol);

        // Assert
        Assert.False(isViewComponent);
    }

    [Fact]
    public void IsViewComponent_InternalViewComponent_ReturnsFalse()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_InternalViewComponent).FullName.AssumeNotNull());
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isViewComponent = tagHelperSymbol.IsViewComponent(TestViewComponentAttributeSymbol, TestNonViewComponentAttributeSymbol);

        // Assert
        Assert.False(isViewComponent);
    }

    [Fact]
    public void IsViewComponent_DecoratedNonViewComponent_ReturnsFalse()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_DecoratedViewComponent).FullName.AssumeNotNull());
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isViewComponent = tagHelperSymbol.IsViewComponent(TestViewComponentAttributeSymbol, TestNonViewComponentAttributeSymbol);

        // Assert
        Assert.False(isViewComponent);
    }

    [Fact]
    public void IsViewComponent_InheritedNonViewComponent_ReturnsFalse()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_InheritedViewComponent).FullName.AssumeNotNull());
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isViewComponent = tagHelperSymbol.IsViewComponent(TestViewComponentAttributeSymbol, TestNonViewComponentAttributeSymbol);

        // Assert
        Assert.False(isViewComponent);
    }

    public abstract class Invalid_AbstractViewComponent
    {
    }

    public class Invalid_GenericViewComponent<T>
    {
    }

    internal class Invalid_InternalViewComponent
    {
    }

    public class Valid_PlainViewComponent
    {
    }

    [TestViewComponent]
    public class Valid_DecoratedVC
    {
    }

    public class Valid_InheritedVC : Valid_DecoratedVC
    {
    }

    [TestNonViewComponent]
    public class Invalid_DecoratedViewComponent
    {
    }

    [TestViewComponent]
    public class Invalid_InheritedViewComponent : Invalid_DecoratedViewComponent
    {
    }

    public class TestViewComponentAttribute : Attribute
    {
    }

    public class TestNonViewComponentAttribute : Attribute
    {
    }
}
