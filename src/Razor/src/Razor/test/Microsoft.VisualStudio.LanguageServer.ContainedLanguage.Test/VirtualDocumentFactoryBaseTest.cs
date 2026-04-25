// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Test;

public class VirtualDocumentFactoryBaseTest : ToolingTestBase
{
    private readonly ITextBuffer _nonHostLSPBuffer;
    private readonly ITextBuffer _hostLSPBuffer;
    private readonly IContentTypeRegistryService _contentTypeRegistry;
    private readonly ITextBufferFactoryService _textBufferFactoryService;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;

    public VirtualDocumentFactoryBaseTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _contentTypeRegistry = Mock.Of<IContentTypeRegistryService>(MockBehavior.Strict);
        var textBufferFactoryService = new Mock<ITextBufferFactoryService>(MockBehavior.Strict);
        var factoryBuffer = Mock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>(MockBehavior.Strict) && buffer.Properties == new PropertyCollection() && buffer.ContentType == TestVirtualDocumentFactory.LanguageLSPContentTypeInstance, MockBehavior.Strict);
        Mock.Get(factoryBuffer).Setup(b => b.ChangeContentType(It.IsAny<IContentType>(), It.IsAny<object>())).Verifiable();
        textBufferFactoryService
            .Setup(factory => factory.CreateTextBuffer())
            .Returns(factoryBuffer);
        _textBufferFactoryService = textBufferFactoryService.Object;

        var hostContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(TestVirtualDocumentFactory.HostDocumentContentTypeNameConst) == true, MockBehavior.Strict);
        _hostLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == hostContentType, MockBehavior.Strict);

        var nonHostLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(It.IsAny<string>()) == false, MockBehavior.Strict);
        _nonHostLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == nonHostLSPContentType, MockBehavior.Strict);

        _textDocumentFactoryService = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict).Object;
        Mock.Get(_textDocumentFactoryService).Setup(s => s.CreateTextDocument(It.IsAny<ITextBuffer>(), It.IsAny<string>())).Returns((ITextDocument)null);
    }

    [Fact]
    public void TryCreateFor_IncompatibleHostDocumentBuffer_ReturnsFalse()
    {
        // Arrange
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(It.IsAny<ITextBuffer>()) == uri, MockBehavior.Strict);
        var factory = new TestVirtualDocumentFactory(_contentTypeRegistry, _textBufferFactoryService, _textDocumentFactoryService, uriProvider);

        // Act
        var result = factory.TryCreateFor(_nonHostLSPBuffer, out var virtualDocument);
        using (virtualDocument)
        {
            // Assert
            Assert.False(result);
            Assert.Null(virtualDocument);
        }
    }

    [Fact]
    public void TryCreateFor_ReturnsLanguageVirtualDocumentAndTrue()
    {
        // Arrange
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(_hostLSPBuffer) == uri, MockBehavior.Strict);
        Mock.Get(uriProvider).Setup(p => p.AddOrUpdate(It.IsAny<ITextBuffer>(), It.IsAny<Uri>())).Verifiable();
        var factory = new TestVirtualDocumentFactory(_contentTypeRegistry, _textBufferFactoryService, _textDocumentFactoryService, uriProvider);

        // Act
        var result = factory.TryCreateFor(_hostLSPBuffer, out var virtualDocument);

        using (virtualDocument)
        {
            // Assert
            Assert.True(result);
            Assert.NotNull(virtualDocument);
            Assert.EndsWith(TestVirtualDocumentFactory.LanguageFileNameSuffixConst, virtualDocument.Uri.OriginalString, StringComparison.Ordinal);
            Assert.Equal(TestVirtualDocumentFactory.LanguageLSPContentTypeInstance, virtualDocument.TextBuffer.ContentType);
            Assert.True(TestVirtualDocumentFactory.LanguageBufferPropertiesInstance.Keys.All(
                (key) => virtualDocument.TextBuffer.Properties.TryGetProperty(key, out object value) && TestVirtualDocumentFactory.LanguageBufferPropertiesInstance[key] == value
                ));
        }
    }

    private class TestVirtualDocumentFactory : VirtualDocumentFactoryBase
    {
        public const string HostDocumentContentTypeNameConst = "TestHostContentTypeName";
        public const string LanguageContentTypeNameConst = "TestLanguageContentTypeName";
        public const string LanguageFileNameSuffixConst = "__virtual.test";

        public static IContentType LanguageLSPContentTypeInstance { get; } = new TestContentType(LanguageContentTypeNameConst);
        public static Dictionary<object, object> LanguageBufferPropertiesInstance = new() { { "testKey", "testValue" } };

        public TestVirtualDocumentFactory(
            IContentTypeRegistryService contentTypeRegistryService,
            ITextBufferFactoryService textBufferFactoryService,
            ITextDocumentFactoryService textDocumentFactoryService,
            FileUriProvider fileUriProvider)
            : base(contentTypeRegistryService, textBufferFactoryService, textDocumentFactoryService, fileUriProvider) { }

        protected override IContentType LanguageContentType => LanguageLSPContentTypeInstance;

        protected override string LanguageFileNameSuffix => LanguageFileNameSuffixConst;

        protected override string HostDocumentContentTypeName => HostDocumentContentTypeNameConst;

        protected override VirtualDocument CreateVirtualDocument(Uri uri, ITextBuffer textBuffer) => new TestVirtualDocument(uri, textBuffer);

        protected override IReadOnlyDictionary<object, object> LanguageBufferProperties => LanguageBufferPropertiesInstance;
    }
}
