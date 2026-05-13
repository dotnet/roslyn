// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class DefaultCodeTargetBuilderTest
{
    [Fact]
    public void Build_CreatesDefaultCodeTarget()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        var builder = new DefaultCodeTargetBuilder(codeDocument);

        var extensions = new ICodeTargetExtension[]
        {
            new MyExtension1(),
            new MyExtension2(),
        };

        foreach (var extension in extensions)
        {
            builder.TargetExtensions.Add(extension);
        }

        // Act
        var result = builder.Build();

        // Assert
        var target = Assert.IsType<DefaultCodeTarget>(result);
        Assert.Equal(extensions, target.Extensions);
    }

    private class MyExtension1 : ICodeTargetExtension
    {
    }

    private class MyExtension2 : ICodeTargetExtension
    {
    }
}
