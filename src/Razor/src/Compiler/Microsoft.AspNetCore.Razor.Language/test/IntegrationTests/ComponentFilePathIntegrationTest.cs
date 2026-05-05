// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Integration tests focused on file path handling for class/namespace names
public class ComponentFilePathIntegrationTest : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    [Fact]
    public void FileNameIsInvalidClassName_SanitizesInvalidClassName()
    {
        // Arrange

        // Act
        var result = CompileToAssembly("Filename with spaces.cshtml", "");

        // Assert
        Assert.Empty(result.CSharpDiagnostics);

        var type = result.Compilation.GetTypeByMetadataName($"{DefaultRootNamespace}.Filename_with_spaces");
        Assert.NotNull(type);
    }

    [Theory]
    [InlineData("ItemAtRoot.cs", "Test", "ItemAtRoot")]
    [InlineData("Dir1\\MyFile.cs", "Test.Dir1", "MyFile")]
    [InlineData("Dir1\\Dir2\\MyFile.cs", "Test.Dir1.Dir2", "MyFile")]
    public void CreatesClassWithCorrectNameAndNamespace(string relativePath, string expectedNamespace, string expectedClassName)
    {
        // Arrange
        relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);

        // Act
        var result = CompileToAssembly(relativePath, "");

        // Assert
        Assert.Empty(result.CSharpDiagnostics);

        var type = result.Compilation.GetTypeByMetadataName($"{expectedNamespace}.{expectedClassName}");
        Assert.NotNull(type);
    }
}
