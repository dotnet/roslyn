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
        var htmlContentType = Mock.Of<IContentType>(MockBehavior.Strict);
        _contentTypeRegistryService = Mock.Of<IContentTypeRegistryService>(
            registry => registry.GetContentType(RazorLSPConstants.HtmlLSPDelegationContentTypeName) == htmlContentType, MockBehavior.Strict);
        var textBufferFactoryService = new Mock<ITextBufferFactoryService>(MockBehavior.Strict);
        var factoryBuffer = Mock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>(MockBehavior.Strict) && buffer.Properties == new PropertyCollection(), MockBehavior.Strict);
        Mock.Get(factoryBuffer).Setup(b => b.ChangeContentType(It.IsAny<IContentType>(), It.IsAny<object>())).Verifiable();
        textBufferFactoryService
            .Setup(factory => factory.CreateTextBuffer())
            .Returns(factoryBuffer);
        _textBufferFactoryService = textBufferFactoryService.Object;

        var razorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(RazorConstants.RazorLSPContentTypeName) == true, MockBehavior.Strict);
        _razorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == razorLSPContentType, MockBehavior.Strict);

        var nonRazorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(It.IsAny<string>()) == false, MockBehavior.Strict);
        _nonRazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == nonRazorLSPContentType, MockBehavior.Strict);

        _textDocumentFactoryService = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict).Object;
        Mock.Get(_textDocumentFactoryService).Setup(s => s.CreateTextDocument(It.IsAny<ITextBuffer>(), It.IsAny<string>())).Returns((ITextDocument)null);
    }

    [Fact]
    public void TryCreateFor_NonRazorLSPBuffer_ReturnsFalse()
    {
        // Arrange
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(It.IsAny<ITextBuffer>()) == uri, MockBehavior.Strict);
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
        var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(_razorLSPBuffer) == uri, MockBehavior.Strict);
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
