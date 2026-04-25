// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

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
}
