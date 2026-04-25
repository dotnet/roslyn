// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

public class HtmlVirtualDocumentFactoryTest : ToolingTestBase
{
    private readonly ITextBuffer _nonRazorLSPBuffer;
    private readonly ITextBuffer _razorLSPBuffer;
    private readonly IContentTypeRegistryService _contentTypeRegistryService;
    private readonly ITextBufferFactoryService _textBufferFactoryService;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;

    public HtmlVirtualDocumentFactoryTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        var htmlContentType = StrictMock.Of<IContentType>();
        _contentTypeRegistryService = new MockRepository(MockBehavior.Strict).OneOf<IContentTypeRegistryService>(
            registry => registry.GetContentType(RazorLSPConstants.HtmlLSPDelegationContentTypeName) == htmlContentType);
        var textBufferFactoryService = new Mock<ITextBufferFactoryService>(MockBehavior.Strict);
        var factoryBuffer = StrictMock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == StrictMock.Of<ITextSnapshot>() && buffer.Properties == new PropertyCollection());
        Mock.Get(factoryBuffer).Setup(b => b.ChangeContentType(It.IsAny<IContentType>(), It.IsAny<object>())).Verifiable();
        textBufferFactoryService
            .Setup(factory => factory.CreateTextBuffer())
            .Returns(factoryBuffer);
        _textBufferFactoryService = textBufferFactoryService.Object;

        var razorLSPContentType = StrictMock.Of<IContentType>(contentType => contentType.IsOfType(RazorConstants.RazorLSPContentTypeName) == true);
        _razorLSPBuffer = StrictMock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == razorLSPContentType);

        var nonRazorLSPContentType = StrictMock.Of<IContentType>(contentType => contentType.IsOfType(It.IsAny<string>()) == false);
        _nonRazorLSPBuffer = StrictMock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == nonRazorLSPContentType);

        _textDocumentFactoryService = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict).Object;
        Mock.Get(_textDocumentFactoryService).Setup(s => s.CreateTextDocument(It.IsAny<ITextBuffer>(), It.IsAny<string>())).Returns((ITextDocument)null);
    }

    [Fact]
    public void TryCreateFor_NonRazorLSPBuffer_ReturnsFalse()
    {
        // Arrange
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = StrictMock.Of<FileUriProvider>(provider => provider.GetOrCreate(It.IsAny<ITextBuffer>()) == uri);
        var factory = new HtmlVirtualDocumentFactory(_contentTypeRegistryService, _textBufferFactoryService, _textDocumentFactoryService, uriProvider, telemetryReporter: null);

        // Act
        var result = factory.TryCreateFor(_nonRazorLSPBuffer, out var virtualDocument);

        using (virtualDocument)
        {
            // Assert
            Assert.False(result);
            Assert.Null(virtualDocument);
        }
    }

    [Fact]
    public void TryCreateFor_RazorLSPBuffer_ReturnsHtmlVirtualDocumentAndTrue()
    {
        // Arrange
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = StrictMock.Of<FileUriProvider>(provider => provider.GetOrCreate(_razorLSPBuffer) == uri);
        Mock.Get(uriProvider).Setup(p => p.AddOrUpdate(It.IsAny<ITextBuffer>(), It.IsAny<Uri>())).Verifiable();
        var factory = new HtmlVirtualDocumentFactory(_contentTypeRegistryService, _textBufferFactoryService, _textDocumentFactoryService, uriProvider, telemetryReporter: null);

        // Act
        var result = factory.TryCreateFor(_razorLSPBuffer, out var virtualDocument);

        using (virtualDocument)
        {
            // Assert
            Assert.True(result);
            Assert.NotNull(virtualDocument);
            Assert.EndsWith(LanguageServerConstants.HtmlVirtualDocumentSuffix, virtualDocument.Uri.OriginalString, StringComparison.Ordinal);
        }
    }
}
