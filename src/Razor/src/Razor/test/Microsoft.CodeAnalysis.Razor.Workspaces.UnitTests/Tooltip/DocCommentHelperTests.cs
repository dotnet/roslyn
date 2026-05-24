// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

public class DocCommentHelperTests(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void ReduceTypeName_Plain()
    {
        // Arrange
        var content = "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName";

        // Act
        var reduced = DocCommentHelpers.ReduceTypeName(content);

        // Assert
        Assert.Equal("SomeTypeName", reduced);
    }

    [Fact]
    public void ReduceTypeName_Generics()
    {
        // Arrange
        var content = "System.Collections.Generic.List<System.String>";

        // Act
        var reduced = DocCommentHelpers.ReduceTypeName(content);

        // Assert
        Assert.Equal("List<System.String>", reduced);
    }

    [Fact]
    public void ReduceTypeName_CrefGenerics()
    {
        // Arrange
        var content = "System.Collections.Generic.List{System.String}";

        // Act
        var reduced = DocCommentHelpers.ReduceTypeName(content);

        // Assert
        Assert.Equal("List{System.String}", reduced);
    }

    [Fact]
    public void ReduceTypeName_NestedGenerics()
    {
        // Arrange
        var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType<Foo.Bar<Baz.Phi>>";

        // Act
        var reduced = DocCommentHelpers.ReduceTypeName(content);

        // Assert
        Assert.Equal("SomeType<Foo.Bar<Baz.Phi>>", reduced);
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar<Baz.Phi>>")]
    [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar{Baz.Phi}}")]
    public void ReduceTypeName_UnbalancedDocs_NotRecoverable_ReturnsOriginalContent(string content)
    {
        // Arrange

        // Act
        var reduced = DocCommentHelpers.ReduceTypeName(content);

        // Assert
        Assert.Equal(content, reduced);
    }

    [Fact]
    public void ReduceMemberName_Plain()
    {
        // Arrange
        var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType.SomeProperty";

        // Act
        var reduced = DocCommentHelpers.ReduceMemberName(content);

        // Assert
        Assert.Equal("SomeType.SomeProperty", reduced);
    }

    [Fact]
    public void ReduceMemberName_Generics()
    {
        // Arrange
        var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType<Foo.Bar>.SomeProperty<Foo.Bar>";

        // Act
        var reduced = DocCommentHelpers.ReduceMemberName(content);

        // Assert
        Assert.Equal("SomeType<Foo.Bar>.SomeProperty<Foo.Bar>", reduced);
    }

    [Fact]
    public void ReduceMemberName_CrefGenerics()
    {
        // Arrange
        var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType{Foo.Bar}.SomeProperty{Foo.Bar}";

        // Act
        var reduced = DocCommentHelpers.ReduceMemberName(content);

        // Assert
        Assert.Equal("SomeType{Foo.Bar}.SomeProperty{Foo.Bar}", reduced);
    }

    [Fact]
    public void ReduceMemberName_NestedGenericsMethodsTypes()
    {
        // Arrange
        var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType<Foo.Bar<Baz,Fi>>.SomeMethod(Foo.Bar<System.String>,Baz<Something>.Fi)";

        // Act
        var reduced = DocCommentHelpers.ReduceMemberName(content);

        // Assert
        Assert.Equal("SomeType<Foo.Bar<Baz,Fi>>.SomeMethod(Foo.Bar<System.String>,Baz<Something>.Fi)", reduced);
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar<Baz.Phi>>")]
    [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar{Baz.Phi}}")]
    [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar(Baz.Phi))")]
    [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo{.>")]
    public void ReduceMemberName_UnbalancedDocs_NotRecoverable_ReturnsOriginalContent(string content)
    {
        // Arrange

        // Act
        var reduced = DocCommentHelpers.ReduceMemberName(content);

        // Assert
        Assert.Equal(content, reduced);
    }

    [Fact]
    public void ReduceCrefValue_InvalidShortValue_ReturnsEmptyString()
    {
        // Arrange
        var content = "T:";

        // Act
        var value = DocCommentHelpers.ReduceCrefValue(content);

        // Assert
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void ReduceCrefValue_InvalidUnknownIdentifierValue_ReturnsEmptyString()
    {
        // Arrange
        var content = "X:";

        // Act
        var value = DocCommentHelpers.ReduceCrefValue(content);

        // Assert
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void ReduceCrefValue_Type()
    {
        // Arrange
        var content = "T:Microsoft.AspNetCore.SometTagHelpers.SomeType";

        // Act
        var value = DocCommentHelpers.ReduceCrefValue(content);

        // Assert
        Assert.Equal("SomeType", value);
    }

    [Fact]
    public void ReduceCrefValue_Property()
    {
        // Arrange
        var content = "P:Microsoft.AspNetCore.SometTagHelpers.SomeType.SomeProperty";

        // Act
        var value = DocCommentHelpers.ReduceCrefValue(content);

        // Assert
        Assert.Equal("SomeType.SomeProperty", value);
    }

    [Fact]
    public void ReduceCrefValue_Member()
    {
        // Arrange
        var content = "P:Microsoft.AspNetCore.SometTagHelpers.SomeType.SomeMember";

        // Act
        var value = DocCommentHelpers.ReduceCrefValue(content);

        // Assert
        Assert.Equal("SomeType.SomeMember", value);
    }

    [Fact]
    public void TryExtractSummary_Null_ReturnsFalse()
    {
        // Arrange & Act
        var result = DocCommentHelpers.TryExtractSummary(documentation: null, out var summary);

        // Assert
        Assert.False(result);
        Assert.Null(summary);
    }

    [Fact]
    public void TryExtractSummary_ExtractsSummary_ReturnsTrue()
    {
        // Arrange
        var expectedSummary = " Hello World ";
        var documentation = $@"
Prefixed invalid content


<summary>{expectedSummary}</summary>

Suffixed invalid content";

        // Act
        var result = DocCommentHelpers.TryExtractSummary(documentation, out var summary);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedSummary, summary);
    }

    [Fact]
    public void TryExtractSummary_NoStartSummary_ReturnsFalse()
    {
        // Arrange
        var documentation = @"
Prefixed invalid content


</summary>

Suffixed invalid content";

        // Act
        var result = DocCommentHelpers.TryExtractSummary(documentation, out var summary);

        // Assert
        Assert.True(result);
        Assert.Equal(@"Prefixed invalid content


</summary>

Suffixed invalid content", summary);
    }

    [Fact]
    public void TryExtractSummary_NoEndSummary_ReturnsTrue()
    {
        // Arrange
        var documentation = @"
Prefixed invalid content


<summary>

Suffixed invalid content";

        // Act
        var result = DocCommentHelpers.TryExtractSummary(documentation, out var summary);

        // Assert
        Assert.True(result);
        Assert.Equal(@"Prefixed invalid content


<summary>

Suffixed invalid content", summary);
    }

    [Fact]
    public void TryExtractSummary_XMLButNoSummary_ReturnsFalse()
    {
        // Arrange
        var documentation = @"
<param type=""stuff"">param1</param>
<return>Result</return>
";

        // Act
        var result = DocCommentHelpers.TryExtractSummary(documentation, out var summary);

        // Assert
        Assert.False(result);
        Assert.Null(summary);
    }

    [Fact]
    public void TryExtractSummary_NoXml_ReturnsTrue()
    {
        // Arrange
        var documentation = @"
There is no xml, but I got you this < and the >.
";

        // Act
        var result = DocCommentHelpers.TryExtractSummary(documentation, out var summary);

        // Assert
        Assert.True(result);
        Assert.Equal("There is no xml, but I got you this < and the >.", summary);
    }
}
