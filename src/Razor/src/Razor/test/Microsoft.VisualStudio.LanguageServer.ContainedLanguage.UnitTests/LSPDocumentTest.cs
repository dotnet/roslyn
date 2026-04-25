// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public class LSPDocumentTest : ToolingTestBase
{
    private readonly Uri _uri;

    public LSPDocumentTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _uri = new Uri("C:/path/to/file.razor");
    }

    [Fact]
    public void TryGetVirtualDocument_NoCSharpDocument_ReturnsFalse()
    {
        // Arrange
        var virtualDocumentMock = new Mock<VirtualDocument>(MockBehavior.Strict);
        virtualDocumentMock.Setup(d => d.Dispose()).Verifiable();
        using var lspDocument = new DefaultLSPDocument(_uri, Mock.Of<ITextBuffer>(MockBehavior.Strict), new[] { virtualDocumentMock.Object });

        // Act
        var result = lspDocument.TryGetVirtualDocument<TestVirtualDocument>(out var virtualDocument);

        // Assert
        Assert.False(result);
        Assert.Null(virtualDocument);
    }

    [Fact]
    public void TryGetVirtualCSharpDocument_CSharpDocument_ReturnsTrue()
    {
        // Arrange
        var textBuffer = new Mock<ITextBuffer>(MockBehavior.Strict);
        textBuffer.SetupGet(b => b.CurrentSnapshot).Returns((ITextSnapshot)null);
        textBuffer.Setup(b => b.ChangeContentType(It.IsAny<IContentType>(), null)).Verifiable();
        textBuffer.SetupGet(b => b.Properties).Returns(new PropertyCollection());
        var testVirtualDocument = new TestVirtualDocument(_uri, textBuffer.Object);
        var virtualDocumentMock = new Mock<VirtualDocument>(MockBehavior.Strict);
        virtualDocumentMock.Setup(d => d.Dispose()).Verifiable();
        using var lspDocument = new DefaultLSPDocument(_uri, Mock.Of<ITextBuffer>(MockBehavior.Strict), new[] { virtualDocumentMock.Object, testVirtualDocument });

        // Act
        var result = lspDocument.TryGetVirtualDocument<TestVirtualDocument>(out var virtualDocument);

        // Assert
        Assert.True(result);
        Assert.Same(testVirtualDocument, virtualDocument);
    }

    [Fact]
    public void TryGetAllVirtualDocument_SpecificDocument_CSharpDocument_ReturnsTrue()
    {
        // Arrange
        var textBuffer1 = new Mock<ITextBuffer>(MockBehavior.Strict);
        textBuffer1.SetupGet(b => b.CurrentSnapshot).Returns((ITextSnapshot)null);
        textBuffer1.Setup(b => b.ChangeContentType(It.IsAny<IContentType>(), null)).Verifiable();
        textBuffer1.SetupGet(b => b.Properties).Returns(new PropertyCollection());
        var testVirtualDocument1 = new TestVirtualDocument(new Uri("C:/path/to/1/file.razor.g.cs"), textBuffer1.Object);
        var textBuffer2 = new Mock<ITextBuffer>(MockBehavior.Strict);
        textBuffer2.SetupGet(b => b.CurrentSnapshot).Returns((ITextSnapshot)null);
        textBuffer2.Setup(b => b.ChangeContentType(It.IsAny<IContentType>(), null)).Verifiable();
        textBuffer2.SetupGet(b => b.Properties).Returns(new PropertyCollection());
        var testVirtualDocument2 = new TestVirtualDocument(new Uri("C:/path/to/2/file.razor.g.cs"), textBuffer2.Object);
        using var lspDocument = new DefaultLSPDocument(_uri, Mock.Of<ITextBuffer>(MockBehavior.Strict), new[] { testVirtualDocument1, testVirtualDocument2 });

        // Act
        var result = lspDocument.TryGetVirtualDocument<TestVirtualDocument>(testVirtualDocument2.Uri, out var virtualDocument);

        // Assert
        Assert.True(result);
        Assert.Same(testVirtualDocument2, virtualDocument);
    }
}
