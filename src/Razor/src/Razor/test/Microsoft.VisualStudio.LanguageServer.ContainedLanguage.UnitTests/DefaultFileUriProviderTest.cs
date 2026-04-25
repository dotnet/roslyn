// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public class DefaultFileUriProviderTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly ITextBuffer _textBuffer = new TestTextBuffer(StringTextSnapshot.Empty);

    [Fact]
    public void AddOrUpdate_Adds()
    {
        // Arrange
        var expectedUri = new Uri("C:/path/to/file.razor");
        var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>(MockBehavior.Strict));

        // Act
        uriProvider.AddOrUpdate(_textBuffer, expectedUri);

        // Assert
        Assert.True(uriProvider.TryGet(_textBuffer, out var uri));
        Assert.Same(expectedUri, uri);
    }

    [Fact]
    public void AddOrUpdate_Updates()
    {
        // Arrange
        var expectedUri = new Uri("C:/path/to/file.razor");
        var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>(MockBehavior.Strict));
        uriProvider.AddOrUpdate(_textBuffer, new Uri("C:/original/uri.razor"));

        // Act
        uriProvider.AddOrUpdate(_textBuffer, expectedUri);

        // Assert
        Assert.True(uriProvider.TryGet(_textBuffer, out var uri));
        Assert.Same(expectedUri, uri);
    }

    [Fact]
    public void TryGet_Exists_ReturnsTrue()
    {
        // Arrange
        var expectedUri = new Uri("C:/path/to/file.razor");
        var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>(MockBehavior.Strict));
        uriProvider.AddOrUpdate(_textBuffer, expectedUri);

        // Act
        var result = uriProvider.TryGet(_textBuffer, out var uri);

        // Assert
        Assert.True(result);
        Assert.Same(expectedUri, uri);
    }

    [Fact]
    public void TryGet_DoesNotExist_ReturnsFalse()
    {
        // Arrange
        var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>(MockBehavior.Strict));

        // Act
        var result = uriProvider.TryGet(_textBuffer, out var uri);

        // Assert
        Assert.False(result);
        Assert.Null(uri);
    }

    [Fact]
    public void GetOrCreate_NoTextDocument_Creates()
    {
        // Arrange
        var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict);
        textDocumentFactoryService.Setup(s => s.TryGetTextDocument(_textBuffer, out It.Ref<ITextDocument>.IsAny)).Returns(false);
        var uriProvider = new DefaultFileUriProvider(textDocumentFactoryService.Object);

        // Act
        var uri = uriProvider.GetOrCreate(_textBuffer);

        // Assert
        Assert.NotNull(uri);
    }

    [Fact]
    public void GetOrCreate_NoTextDocument_MemoizesGeneratedUri()
    {
        // Arrange
        var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict);
        textDocumentFactoryService.Setup(s => s.TryGetTextDocument(_textBuffer, out It.Ref<ITextDocument>.IsAny)).Returns(false);
        var uriProvider = new DefaultFileUriProvider(textDocumentFactoryService.Object);

        // Act
        var uri1 = uriProvider.GetOrCreate(_textBuffer);
        var uri2 = uriProvider.GetOrCreate(_textBuffer);

        // Assert
        Assert.NotNull(uri1);
        Assert.Same(uri1, uri2);
    }

    [Fact]
    public void GetOrCreate_TurnsTextDocumentFilePathIntoUri()
    {
        // Arrange
        var factory = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict);
        var expectedFilePath = "C:/path/to/file.razor";
        var textDocument = Mock.Of<ITextDocument>(document => document.FilePath == expectedFilePath, MockBehavior.Strict);
        factory.Setup(f => f.TryGetTextDocument(_textBuffer, out textDocument))
            .Returns(true);
        var uriProvider = new DefaultFileUriProvider(factory.Object);

        // Act
        var uri = uriProvider.GetOrCreate(_textBuffer);

        // Assert
        Assert.Equal(expectedFilePath, uri.OriginalString);
    }
}
