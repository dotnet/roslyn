// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test;

public class FilePathServiceTest
{
    [Theory]
    [InlineData(@"C:\path\to\file.razor__virtual.html")]
    [InlineData(@"C:\path\to\file.razor")]
    public void GetRazorFilePath_ReturnsExpectedPath(string inputFilePath)
    {
        // Act
        var result = AbstractFilePathService.TestAccessor.GetRazorFilePath(inputFilePath);

        // Assert
        Assert.Equal(@"C:\path\to\file.razor", result);
    }

    [Fact]
    public void GetRazorDocumentUri_HtmlFile_ReturnsExpectedUri()
    {
        // Arrange
        var filePathService = new TestFilePathService();
        // Act
        var result = filePathService.GetRazorDocumentUri(new Uri(@"C:\path\to\file.razor__virtual.html"));

        // Assert
        Assert.Equal(@"C:/path/to/file.razor", result.GetAbsoluteOrUNCPath());
    }

    [Fact]
    public void GetRazorDocumentUri_RazorFile_ReturnsExpectedUri()
    {
        // Arrange
        var filePathService = new TestFilePathService();
        // Act
        var result = filePathService.GetRazorDocumentUri(new Uri(@"C:\path\to\file.razor"));

        // Assert
        Assert.Equal(@"C:/path/to/file.razor", result.GetAbsoluteOrUNCPath());
    }

    private class TestFilePathService() : AbstractFilePathService()
    {
    }
}
