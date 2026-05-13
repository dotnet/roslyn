// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

public class FilePathNormalizerTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [ConditionalFact(Is.Windows)]
    public void Normalize_Windows_StripsPrecedingSlash()
    {
        // Arrange
        var path = "/c:/path/to/something";

        // Act
        path = FilePathNormalizer.Normalize(path);

        // Assert
        Assert.Equal("c:/path/to/something", path);
    }

    [ConditionalFact(Is.Windows)]
    public void Normalize_Windows_StripsPrecedingSlash_ShortPath()
    {
        // Arrange
        var path = "/c";

        // Act
        path = FilePathNormalizer.Normalize(path);

        // Assert
        Assert.Equal("c", path);
    }

    [Fact]
    public void Normalize_NormalizesPathsWithSlashAtPositionOne()
    {
        // Arrange
        var path = @"d\ComputerName\path\to\something";

        // Act
        path = FilePathNormalizer.Normalize(path);

        // Assert
        Assert.Equal("d/ComputerName/path/to/something", path);
    }

    [Fact]
    public void Normalize_FixesUNCPaths()
    {
        // Arrange
        var path = "//ComputerName/path/to/something";

        // Act
        path = FilePathNormalizer.Normalize(path);

        // Assert
        Assert.Equal(@"\\ComputerName/path/to/something", path);
    }

    [Fact]
    public void Normalize_IgnoresUNCPaths()
    {
        // Arrange
        var path = @"\\ComputerName\path\to\something";

        // Act
        path = FilePathNormalizer.Normalize(path);

        // Assert
        Assert.Equal(@"\\ComputerName/path/to/something", path);
    }

    [Fact]
    public void NormalizeDirectory_DedupesBackSlashes()
    {
        // Arrange
        var directory = @"C:\path\to\\directory\";

        // Act
        var normalized = FilePathNormalizer.NormalizeDirectory(directory);

        // Assert
        Assert.Equal("C:/path/to/directory/", normalized);
    }

    [Fact]
    public void NormalizeDirectory_DedupesForwardSlashes()
    {
        // Arrange
        var directory = "C:/path/to//directory/";

        // Act
        var normalized = FilePathNormalizer.NormalizeDirectory(directory);

        // Assert
        Assert.Equal("C:/path/to/directory/", normalized);
    }

    [Fact]
    public void NormalizeDirectory_DedupesMismatchedSlashes()
    {
        // Arrange
        var directory = "C:\\path\\to\\/directory\\";

        // Act
        var normalized = FilePathNormalizer.NormalizeDirectory(directory);

        // Assert
        Assert.Equal("C:/path/to/directory/", normalized);
    }

    [Fact]
    public void NormalizeDirectory_EndsWithSlash()
    {
        // Arrange
        var directory = "C:\\path\\to\\directory\\";

        // Act
        var normalized = FilePathNormalizer.NormalizeDirectory(directory);

        // Assert
        Assert.Equal("C:/path/to/directory/", normalized);
    }

    [Fact]
    public void NormalizeDirectory_EndsWithoutSlash()
    {
        // Arrange
        var directory = "C:\\path\\to\\directory";

        // Act
        var normalized = FilePathNormalizer.NormalizeDirectory(directory);

        // Assert
        Assert.Equal("C:/path/to/directory/", normalized);
    }

    [ConditionalFact(Is.Windows)]
    public void NormalizeDirectory_Windows_HandlesSingleSlashDirectory()
    {
        // Arrange
        var directory = @"\";

        // Act
        var normalized = FilePathNormalizer.NormalizeDirectory(directory);

        // Assert
        Assert.Equal("/", normalized);
    }

    [Fact]
    public void NormalizeDirectory_HandlesSingleSlashDirectory()
    {
        // Arrange
        var directory = "/";

        // Act
        var normalized = FilePathNormalizer.NormalizeDirectory(directory);

        // Assert
        Assert.Equal("/", normalized);
    }

    [Theory]
    [InlineData("path/to/", "path/to/", true)]
    [InlineData("path/to1/", "path/to2/", false)]
    [InlineData("path/to/", "path/to/file.cs", true)]
    [InlineData("path/to/file.cs", "path/to/file.cs", true)]
    [InlineData("path/to/file1.cs", "path/to/file2.cs", true)]
    [InlineData("path/to1/file.cs", "path/to2/file.cs", false)]
    [InlineData("path/to/", @"path\to\", true)]
    [InlineData("path/to1/", @"path\to2\", false)]
    [InlineData("path/to/", @"path\to\file.cs", true)]
    [InlineData("path/to/file.cs", @"path\to\file.cs", true)]
    [InlineData("path/to/file1.cs", @"path\to\file2.cs", true)]
    [InlineData("path/to1/file.cs", @"path\to2\file.cs", false)]
    public void AreDirectoryPathsEquivalent(string path1, string path2, bool expected)
    {
        var result = FilePathNormalizer.AreDirectoryPathsEquivalent(path1, path2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AreFilePathsEquivalent_NotEqualPaths_ReturnsFalse()
    {
        // Arrange
        var filePath1 = "path/to/document.cshtml";
        var filePath2 = "path\\to\\different\\document.cshtml";

        // Act
        var result = FilePathNormalizer.AreFilePathsEquivalent(filePath1, filePath2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreFilePathsEquivalent_NormalizesPathsBeforeComparison_ReturnsTrue()
    {
        // Arrange
        var filePath1 = "path/to/document.cshtml";
        var filePath2 = "path\\to\\document.cshtml";

        // Act
        var result = FilePathNormalizer.AreFilePathsEquivalent(filePath1, filePath2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetDirectory_IncludesTrailingSlash()
    {
        // Arrange
        var filePath = "C:/path/to/document.cshtml";

        // Act
        var normalized = FilePathNormalizer.GetNormalizedDirectoryName(filePath);

        // Assert
        Assert.Equal("C:/path/to/", normalized);
    }

    [Fact]
    public void GetDirectory_NoDirectory_ReturnsRoot()
    {
        // Arrange
        var filePath = "C:/document.cshtml";

        // Act
        var normalized = FilePathNormalizer.GetNormalizedDirectoryName(filePath);

        // Assert
        Assert.Equal("C:/", normalized);
    }

    [Fact]
    public void Normalize_NullFilePath_ReturnsForwardSlash()
    {
        // Act
        var normalized = FilePathNormalizer.Normalize(null);

        // Assert
        Assert.Equal("/", normalized);
    }

    [Fact]
    public void Normalize_EmptyFilePath_ReturnsEmptyString()
    {
        // Act
        var normalized = FilePathNormalizer.Normalize(string.Empty);

        // Assert
        Assert.Equal("/", normalized);
    }

    [ConditionalFact(AlwaysSkip = "https://github.com/dotnet/razor/issues/11660")]
    public void Normalize_NonWindows_AddsLeadingForwardSlash()
    {
        // Arrange
        var filePath = "path/to/document.cshtml";

        // Act
        var normalized = FilePathNormalizer.Normalize(filePath);

        // Assert
        Assert.Equal("/path/to/document.cshtml", normalized);
    }

    [Fact]
    public void Normalize_UrlDecodesFilePath()
    {
        // Arrange
        var filePath = "C:/path%20to/document.cshtml";

        // Act
        var normalized = FilePathNormalizer.Normalize(filePath);

        // Assert
        Assert.Equal("C:/path to/document.cshtml", normalized);
    }

    [Fact]
    public void Normalize_UrlDecodesOnlyOnce()
    {
        // Arrange
        var filePath = "C:/path%2Bto/document.cshtml";

        // Act
        var normalized = FilePathNormalizer.Normalize(filePath);
        normalized = FilePathNormalizer.Normalize(normalized);

        // Assert
        Assert.Equal("C:/path+to/document.cshtml", normalized);
    }

    [Fact]
    public void Normalize_ReplacesBackSlashesWithForwardSlashes()
    {
        // Arrange
        var filePath = "C:\\path\\to\\document.cshtml";

        // Act
        var normalized = FilePathNormalizer.Normalize(filePath);

        // Assert
        Assert.Equal("C:/path/to/document.cshtml", normalized);
    }

    [ConditionalTheory(Is.Windows)]
    [InlineData(@"C:\path\to\document.cshtml")]
    [InlineData(@"c:\path\to\document.cshtml")]
    [InlineData("C:/path/to/document.cshtml")]
    [InlineData("c:/path/to/document.cshtml")]
    public void Comparer_CaseInsensitiveDictionary(string fileName)
    {
        var dictionary = new Dictionary<string, bool>(FilePathNormalizingComparer.Instance)
        {
            { "C:/path/to/document.cshtml", true },
            { "C:/path/to/document1.cshtml", true },
            { "C:/path/to/document2.cshtml", true }
        };

        Assert.True(dictionary.ContainsKey(fileName));
        Assert.True(dictionary.TryGetValue(fileName, out _));
    }
}
