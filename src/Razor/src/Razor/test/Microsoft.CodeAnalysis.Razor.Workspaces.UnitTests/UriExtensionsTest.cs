// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor;

public class UriExtensionsTest : ToolingTestBase
{
    public UriExtensionsTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [ConditionalFact(Is.Windows)]
    public void GetAbsoluteOrUNCPath_AbsolutePath_ReturnsAbsolutePath()
    {
        // Arrange
        var uri = new Uri("c:\\Some\\path\\to\\file.cshtml");

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal(uri.AbsolutePath, path);
    }

    [ConditionalFact(Is.Windows)]
    public void GetAbsoluteOrUNCPath_AbsolutePath_HandlesPlusPaths()
    {
        // Arrange
        var uri = new Uri("c:\\Some\\path\\to\\file+2.cshtml");

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal(uri.AbsolutePath, path);
    }

    [ConditionalFact(Is.Windows)]
    public void GetAbsoluteOrUNCPath_AbsolutePath_HandlesSpacePaths()
    {
        // Arrange
        var uri = new Uri("c:\\Some\\path\\to\\file path.cshtml");

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal("c:/Some/path/to/file path.cshtml", path);
    }

    [ConditionalTheory(Is.Windows)]
    [WorkItem("https://github.com/dotnet/razor/issues/9365")]
    [InlineData(@"git:/c%3A/path/to/dir/Index.cshtml", @"c:/_git_/path/to/dir/Index.cshtml")]
    [InlineData(@"git:/c:/path%2Fto/dir/Index.cshtml?%7B%22p", @"c:/_git_/path/to/dir/Index.cshtml")]
    [InlineData(@"git:/c:/path/to/dir/Index.cshtml", @"c:/_git_/path/to/dir/Index.cshtml")]
    [InlineData(@"chat-editing-text-model:/c:/path/to/dir/Index.cshtml", @"c:/_chat-editing-text-model_/path/to/dir/Index.cshtml")]
    public void GetAbsoluteOrUNCPath_AbsolutePath_HandlesGitScheme(string filePath, string expected)
    {
        // Arrange
        var uri = new Uri(filePath);

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal(expected, path);
    }

    [ConditionalTheory(Is.Windows)]
    [InlineData(@"file:///c:/path/to/dir/Index.cshtml", @"c:/path/to/dir/Index.cshtml")]
    [InlineData(@"file:///c:\path/to\dir/Index.cshtml", @"c:/path/to/dir/Index.cshtml")]
    [InlineData(@"file:///C:\path\to\dir\Index.cshtml", @"C:/path/to/dir/Index.cshtml")]
    [InlineData(@"file:///C:\PATH\TO\DIR\Index.cshtml", @"C:/PATH/TO/DIR/Index.cshtml")]
    [InlineData(@"file:\\path\to\dir\Index.cshtml", @"\\path\to\dir\Index.cshtml")]
    [InlineData("file:///path/to/dir/Index.cshtml", @"/path/to/dir/Index.cshtml")]
    public void GetAbsoluteOrUNCPath_AbsolutePath_HandlesFileScheme(string filePath, string expected)
    {
        // Arrange
        var uri = new Uri(filePath);

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal(expected, path);
    }

    [Fact]
    public void GetAbsoluteOrUNCPath_UNCPath_ReturnsLocalPath()
    {
        // Arrange
        var uri = new Uri("//Some/path/to/file.cshtml");

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal(uri.LocalPath, path);
    }

    [Fact]
    public void GetAbsoluteOrUNCPath_UNCPath_HandlesPlusPaths()
    {
        // Arrange
        var uri = new Uri("//Some/path/to/file+2.cshtml");

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal(uri.LocalPath, path);
    }

    [Fact]
    public void GetAbsoluteOrUNCPath_UNCPath_HandlesSpacePaths()
    {
        // Arrange
        var uri = new Uri("//Some/path/to/file path.cshtml");

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal(@"\\some\path\to\file path.cshtml", path);
    }

    [ConditionalTheory(Is.Not.Windows)]
    [WorkItem("https://github.com/dotnet/razor/issues/9365")]
    [InlineData("git:///path/to/dir/Index.cshtml", "/_git_/path/to/dir/Index.cshtml")]
    [InlineData("git:///path%2Fto/dir/Index.cshtml", "/_git_/path/to/dir/Index.cshtml")]
    [InlineData("file:///path/to/dir/Index.cshtml", @"/path/to/dir/Index.cshtml")]
    [InlineData("file:///path%2Fto/dir/Index.cshtml", @"/path/to/dir/Index.cshtml")]
    public void GetAbsoluteOrUNCPath_AbsolutePath_HandlesSchemeProperly(string filePath, string expected)
    {
        // Arrange
        var uri = new Uri(filePath);

        // Act
        var path = uri.GetAbsoluteOrUNCPath();

        // Assert
        Assert.Equal(expected, path);
    }
}
