// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

public class IsTagHelperTest : TagHelperDescriptorProviderTestBase
{
    public IsTagHelperTest() : base(AdditionalCode)
    {
        Compilation = BaseCompilation;
        ITagHelperSymbol = Compilation.GetTypeByMetadataName(TagHelperTypes.ITagHelper).AssumeNotNull();
    }

    private Compilation Compilation { get; }

    private INamedTypeSymbol ITagHelperSymbol { get; }

    [Fact]
    public void IsTagHelper_PlainTagHelper_ReturnsTrue()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName("TestNamespace.Valid_PlainTagHelper");
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isTagHelper = tagHelperSymbol.IsTagHelper(ITagHelperSymbol);

        // Assert
        Assert.True(isTagHelper);
    }

    [Fact]
    public void IsTagHelper_InheritedTagHelper_ReturnsTrue()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName("TestNamespace.Valid_InheritedTagHelper");
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isTagHelper = tagHelperSymbol.IsTagHelper(ITagHelperSymbol);

        // Assert
        Assert.True(isTagHelper);
    }

    [Fact]
    public void IsTagHelper_AbstractTagHelper_ReturnsFalse()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName("TestNamespace.Invalid_AbstractTagHelper");
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isTagHelper = tagHelperSymbol.IsTagHelper(ITagHelperSymbol);

        // Assert
        Assert.False(isTagHelper);
    }

    [Fact]
    public void IsTagHelper_GenericTagHelper_ReturnsFalse()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName("TestNamespace.Invalid_GenericTagHelper`1");
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isTagHelper = tagHelperSymbol.IsTagHelper(ITagHelperSymbol);

        // Assert
        Assert.False(isTagHelper);
    }

    [Fact]
    public void IsTagHelper_InternalTagHelper_ReturnsFalse()
    {
        // Arrange
        var tagHelperSymbol = Compilation.GetTypeByMetadataName("TestNamespace.Invalid_InternalTagHelper");
        Assert.NotNull(tagHelperSymbol);

        // Act
        var isTagHelper = tagHelperSymbol.IsTagHelper(ITagHelperSymbol);

        // Assert
        Assert.False(isTagHelper);
    }

    private const string AdditionalCode =
        """
        using Microsoft.AspNetCore.Razor.TagHelpers;

        namespace TestNamespace
        {
            public class Invalid_NestedPublicTagHelper : TagHelper
            {
            }

            public class Valid_NestedPublicViewComponent
            {
                public string Invoke(string foo) => null;
            }

            public abstract class Invalid_AbstractTagHelper : TagHelper
            {
            }

            public class Invalid_GenericTagHelper<T> : TagHelper
            {
            }

            internal class Invalid_InternalTagHelper : TagHelper
            {
            }

            public class Valid_PlainTagHelper : TagHelper
            {
            }

            public class Valid_InheritedTagHelper : Valid_PlainTagHelper
            {
            }
        }
        """;
}
