// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test;

public class DocumentUriExtensionsTest
{
    [Theory]
    [InlineData(@"C:\path\to\file.razor__virtual.html", true, "C:/path/to/file.razor")]
    [InlineData(@"C:\path\to\file.razor", false, null)]
    public void IsRazorHtmlDocumentUri_ReturnsExpectedResult(string inputUri, bool expectedResult, string? expectedFilePath)
    {
        // Arrange
        var documentUri = new DocumentUri(new Uri(inputUri));

        // Act
        var result = documentUri.IsRazorHtmlDocumentUri(out var razorDocumentUri);

        // Assert
        Assert.Equal(expectedResult, result);
        if (expectedFilePath is null)
        {
            Assert.Null(razorDocumentUri);
        }
        else
        {
            Assert.NotNull(razorDocumentUri);
            Assert.Equal(Uri.UriSchemeFile, razorDocumentUri.Scheme);
            Assert.Equal(expectedFilePath, razorDocumentUri.GetAbsoluteOrUNCPath());
        }
    }

    [Fact]
    public void IsRazorHtmlDocumentUri_HtmlFile_ReturnsExpectedUri()
    {
        // Arrange
        var documentUri = new DocumentUri(new Uri(@"C:\path\to\file.razor__virtual.html"));

        // Act
        var result = documentUri.IsRazorHtmlDocumentUri(out var razorDocumentUri);

        // Assert
        Assert.True(result);
        Assert.NotNull(razorDocumentUri);
        Assert.Equal(Uri.UriSchemeFile, razorDocumentUri.Scheme);
        Assert.Equal(@"C:/path/to/file.razor", razorDocumentUri.GetAbsoluteOrUNCPath());
    }

    [Fact]
    public void IsRazorHtmlDocumentUri_RazorFile_ReturnsExpectedResult()
    {
        // Arrange
        var documentUri = new DocumentUri(new Uri(@"C:\path\to\file.razor"));

        // Act
        var result = documentUri.IsRazorHtmlDocumentUri(out var razorDocumentUri);

        // Assert
        Assert.False(result);
        Assert.Null(razorDocumentUri);
    }

    [Theory]
    [InlineData("razor-html:/C:/path/to/file.razor__virtual.html", "C:/path/to/file.razor")]
    [InlineData("razor-html:///C:/path with spaces/to/file.razor__virtual.html", "C:/path with spaces/to/file.razor")]
    public void IsRazorHtmlDocumentUri_RazorHtmlUri_ReturnsRazorFileUri(string inputUri, string expectedFilePath)
    {
        // Arrange
        var documentUri = new DocumentUri(new Uri(inputUri));

        // Act
        var result = documentUri.IsRazorHtmlDocumentUri(out var razorDocumentUri);

        // Assert
        Assert.True(result);
        Assert.NotNull(razorDocumentUri);
        Assert.Equal(Uri.UriSchemeFile, razorDocumentUri.Scheme);
        Assert.Equal(expectedFilePath, razorDocumentUri.GetAbsoluteOrUNCPath());
    }
}
