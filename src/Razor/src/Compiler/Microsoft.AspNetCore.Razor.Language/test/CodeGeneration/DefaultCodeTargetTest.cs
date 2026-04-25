// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class DefaultCodeTargetTest
{
    [Fact]
    public void CreateWriter_DesignTime_CreatesDesignTimeNodeWriter()
    {
        // Arrange
        var codeDocument = RazorCodeDocument.Create(
            TestRazorSourceDocument.Create(),
            codeGenerationOptions: RazorCodeGenerationOptions.DesignTimeDefault);
        var target = new DefaultCodeTarget(codeDocument, extensions: []);

        // Act
        var writer = target.CreateNodeWriter();

        // Assert
        Assert.IsType<DesignTimeNodeWriter>(writer);
    }

    [Fact]
    public void CreateWriter_Runtime_CreatesRuntimeNodeWriter()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();
        var target = new DefaultCodeTarget(codeDocument, extensions: []);

        // Act
        var writer = target.CreateNodeWriter();

        // Assert
        Assert.IsType<RuntimeNodeWriter>(writer);
    }

    [Fact]
    public void HasExtension_ReturnsTrue_WhenExtensionFound()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        ImmutableArray<ICodeTargetExtension> extensions = [
            new MyExtension2(),
            new MyExtension1()
        ];

        var target = new DefaultCodeTarget(codeDocument, extensions);

        // Act
        var result = target.HasExtension<MyExtension1>();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasExtension_ReturnsFalse_WhenExtensionNotFound()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        ImmutableArray<ICodeTargetExtension> extensions = [
            new MyExtension2(),
            new MyExtension2()
        ];

        var target = new DefaultCodeTarget(codeDocument, extensions);

        // Act
        var result = target.HasExtension<MyExtension1>();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetExtension_ReturnsExtension_WhenExtensionFound()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        ImmutableArray<ICodeTargetExtension> extensions = [
            new MyExtension2(),
            new MyExtension1()
        ];

        var target = new DefaultCodeTarget(codeDocument, extensions);

        // Act
        var result = target.GetExtension<MyExtension1>();

        // Assert
        Assert.Same(extensions[1], result);
    }

    [Fact]
    public void GetExtension_ReturnsFirstMatch_WhenExtensionFound()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        ImmutableArray<ICodeTargetExtension> extensions = [
            new MyExtension2(),
            new MyExtension1(),
            new MyExtension2(),
            new MyExtension1()
        ];

        var target = new DefaultCodeTarget(codeDocument, extensions);

        // Act
        var result = target.GetExtension<MyExtension1>();

        // Assert
        Assert.Same(extensions[1], result);
    }


    [Fact]
    public void GetExtension_ReturnsNull_WhenExtensionNotFound()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        ImmutableArray<ICodeTargetExtension> extensions = [
            new MyExtension2(),
            new MyExtension2()
        ];

        var target = new DefaultCodeTarget(codeDocument, extensions);

        // Act
        var result = target.GetExtension<MyExtension1>();

        // Assert
        Assert.Null(result);
    }

    private class MyExtension1 : ICodeTargetExtension
    {
    }

    private class MyExtension2 : ICodeTargetExtension
    {
    }
}
