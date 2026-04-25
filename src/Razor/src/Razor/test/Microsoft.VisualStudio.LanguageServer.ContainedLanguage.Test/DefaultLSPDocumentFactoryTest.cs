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

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public class DefaultLSPDocumentFactoryTest : ToolingTestBase
{
    public DefaultLSPDocumentFactoryTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Fact]
    public void Create_BuildsLSPDocumentWithTextBufferURI()
    {
        // Arrange
        var textBuffer = Mock.Of<ITextBuffer>(MockBehavior.Strict);
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(p => p.GetOrCreate(textBuffer) == uri, MockBehavior.Strict);
        var factory = new DefaultLSPDocumentFactory(uriProvider, Enumerable.Empty<Lazy<VirtualDocumentFactory, IContentTypeMetadata>>());

        // Act
        var lspDocument = factory.Create(textBuffer);

        // Assert
        Assert.Same(uri, lspDocument.Uri);
    }

    [Fact]
    public void Create_MultipleFactories_CreatesLSPDocumentWithVirtualDocuments()
    {
        // Arrange
        var contentType = Mock.Of<IContentType>(ct =>
            ct.TypeName == "text" &&
            ct.IsOfType("text"),
            MockBehavior.Strict
        );
        var metadata = Mock.Of<IContentTypeMetadata>(md =>
            md.ContentTypes == new[] { contentType.TypeName },
            MockBehavior.Strict);
        var textBuffer = Mock.Of<ITextBuffer>(b =>
            b.ContentType == contentType,
            MockBehavior.Strict);
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(p => p.GetOrCreate(textBuffer) == uri, MockBehavior.Strict);
        var emptyVirtualDocuments = Array.Empty<VirtualDocument>();
        var virtualDocument1 = Mock.Of<VirtualDocument>(MockBehavior.Strict);
        var factory1 = Mock.Of<VirtualDocumentFactory>(f => f.TryCreateFor(textBuffer, out virtualDocument1) == true &&
            f.TryCreateMultipleFor(textBuffer, out emptyVirtualDocuments) == false, MockBehavior.Strict);
        var factory1Lazy = new Lazy<VirtualDocumentFactory, IContentTypeMetadata>(() => factory1, metadata);
        var virtualDocument2 = Mock.Of<VirtualDocument>(MockBehavior.Strict);
        var factory2 = Mock.Of<VirtualDocumentFactory>(f => f.TryCreateFor(textBuffer, out virtualDocument2) == true &&
            f.TryCreateMultipleFor(textBuffer, out emptyVirtualDocuments) == false, MockBehavior.Strict);
        var factory2Lazy = new Lazy<VirtualDocumentFactory, IContentTypeMetadata>(() => factory2, metadata);
        var factory = new DefaultLSPDocumentFactory(uriProvider, new[] { factory1Lazy, factory2Lazy });

        // Act
        var lspDocument = factory.Create(textBuffer);

        // Assert
        Assert.Collection(
            lspDocument.VirtualDocuments,
            virtualDocument => Assert.Same(virtualDocument1, virtualDocument),
            virtualDocument => Assert.Same(virtualDocument2, virtualDocument));
    }

    [Fact]
    public void Create_FiltersFactoriesByContentType()
    {
        // Arrange
        var contentType = Mock.Of<IContentType>(ct =>
            ct.TypeName == "text" &&
            ct.IsOfType("NotText") == false,
            MockBehavior.Strict
        );
        var metadata = Mock.Of<IContentTypeMetadata>(md =>
            md.ContentTypes == new[] { "NotText" },
            MockBehavior.Strict);
        var textBuffer = Mock.Of<ITextBuffer>(b =>
            b.ContentType == contentType,
            MockBehavior.Strict);
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(p => p.GetOrCreate(textBuffer) == uri, MockBehavior.Strict);
        var factory1 = Mock.Of<VirtualDocumentFactory>(MockBehavior.Strict);
        var factory1Lazy = new Lazy<VirtualDocumentFactory, IContentTypeMetadata>(() => factory1, metadata);
        var factory = new DefaultLSPDocumentFactory(uriProvider, new[] { factory1Lazy, });

        // Act
        var lspDocument = factory.Create(textBuffer);

        // Assert
        Assert.Empty(lspDocument.VirtualDocuments);
    }

    [Fact]
    public void CreateMultiple_CreatesLSPDocumentWithVirtualDocuments()
    {
        // Arrange
        var contentType = Mock.Of<IContentType>(ct =>
            ct.TypeName == "text" &&
            ct.IsOfType("text"),
            MockBehavior.Strict
        );
        var metadata = Mock.Of<IContentTypeMetadata>(md =>
            md.ContentTypes == new[] { contentType.TypeName },
            MockBehavior.Strict);
        var textBuffer = Mock.Of<ITextBuffer>(b =>
            b.ContentType == contentType,
            MockBehavior.Strict);
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(p => p.GetOrCreate(textBuffer) == uri, MockBehavior.Strict);
        var emptyVirtualDocuments = Array.Empty<VirtualDocument>();
        var virtualDocument1 = Mock.Of<VirtualDocument>(MockBehavior.Strict);
        var virtualDocument2 = Mock.Of<VirtualDocument>(MockBehavior.Strict);
        var virtualDocuments = new[] { virtualDocument1, virtualDocument2 };
        var factory1 = Mock.Of<VirtualDocumentFactory>(f => f.TryCreateMultipleFor(textBuffer, out virtualDocuments) == true, MockBehavior.Strict);
        var factory1Lazy = new Lazy<VirtualDocumentFactory, IContentTypeMetadata>(() => factory1, metadata);

        var factory = new DefaultLSPDocumentFactory(uriProvider, new[] { factory1Lazy });

        // Act
        var lspDocument = factory.Create(textBuffer);

        // Assert
        Assert.Collection(
            lspDocument.VirtualDocuments,
            virtualDocument => Assert.Same(virtualDocument1, virtualDocument),
            virtualDocument => Assert.Same(virtualDocument2, virtualDocument));
    }

    [Fact]
    public void TryRefreshVirtualDocuments_Refreshes_CreatesNewVirtualDocuments()
    {
        // Arrange
        var contentType = Mock.Of<IContentType>(ct =>
            ct.TypeName == "text" &&
            ct.IsOfType("text"),
            MockBehavior.Strict
        );
        var metadata = Mock.Of<IContentTypeMetadata>(md =>
            md.ContentTypes == new[] { contentType.TypeName },
            MockBehavior.Strict);
        var textBuffer = Mock.Of<ITextBuffer>(b =>
            b.ContentType == contentType &&
            b.CurrentSnapshot == null &&
            b.CurrentSnapshot.Version.VersionNumber == 1337,
            MockBehavior.Strict);
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(p => p.GetOrCreate(textBuffer) == uri, MockBehavior.Strict);

        var snapshot = new TestVirtualDocumentSnapshot(uri, 1337);
        var virtualDocument1 = Mock.Of<VirtualDocument>(d => d.CurrentSnapshot == snapshot, MockBehavior.Strict);
        var virtualDocument2 = Mock.Of<VirtualDocument>(d => d.CurrentSnapshot == snapshot, MockBehavior.Strict);
        var virtualDocuments = new[] { virtualDocument1 };
        IReadOnlyList<VirtualDocument> newVirtualDocuments = new[] { virtualDocument2 };
        var factory1 = Mock.Of<VirtualDocumentFactory>(f =>
            f.TryCreateMultipleFor(textBuffer, out virtualDocuments) == true &&
            f.TryRefreshVirtualDocuments(It.IsAny<LSPDocument>(), out newVirtualDocuments) == true, MockBehavior.Strict);
        var factory1Lazy = new Lazy<VirtualDocumentFactory, IContentTypeMetadata>(() => factory1, metadata);

        var factory = new DefaultLSPDocumentFactory(uriProvider, new[] { factory1Lazy });

        // Act
        var document = factory.Create(textBuffer);
        Assert.Single(document.VirtualDocuments, virtualDocument1);
        var result = factory.TryRefreshVirtualDocuments(document);

        // Assert
        Assert.True(result);
        Assert.Single(document.VirtualDocuments, virtualDocument2);
    }

    [Fact]
    public void TryRefreshVirtualDocuments_NoRefresh_KeepsPreviousSnapshots()
    {
        // Arrange
        var contentType = Mock.Of<IContentType>(ct =>
            ct.TypeName == "text" &&
            ct.IsOfType("text"),
            MockBehavior.Strict
        );
        var metadata = Mock.Of<IContentTypeMetadata>(md =>
            md.ContentTypes == new[] { contentType.TypeName },
            MockBehavior.Strict);
        var textBuffer = Mock.Of<ITextBuffer>(b =>
            b.ContentType == contentType &&
            b.CurrentSnapshot == null &&
            b.CurrentSnapshot.Version.VersionNumber == 1337,
            MockBehavior.Strict);
        var uri = new Uri("C:/path/to/file.razor");
        var uriProvider = Mock.Of<FileUriProvider>(p => p.GetOrCreate(textBuffer) == uri, MockBehavior.Strict);

        var snapshot = new TestVirtualDocumentSnapshot(uri, 1337);
        var virtualDocument1 = Mock.Of<VirtualDocument>(d => d.CurrentSnapshot == snapshot, MockBehavior.Strict);
        var virtualDocument2 = Mock.Of<VirtualDocument>(MockBehavior.Strict);
        var virtualDocuments = new[] { virtualDocument1 };
        IReadOnlyList<VirtualDocument> newVirtualDocuments = new[] { virtualDocument2 };
        var factory1 = Mock.Of<VirtualDocumentFactory>(f =>
            f.TryCreateMultipleFor(textBuffer, out virtualDocuments) == true &&
            f.TryRefreshVirtualDocuments(It.IsAny<LSPDocument>(), out newVirtualDocuments) == false, MockBehavior.Strict);
        var factory1Lazy = new Lazy<VirtualDocumentFactory, IContentTypeMetadata>(() => factory1, metadata);

        var factory = new DefaultLSPDocumentFactory(uriProvider, new[] { factory1Lazy });

        // Act
        var document = factory.Create(textBuffer);
        Assert.Single(document.VirtualDocuments, virtualDocument1);
        var result = factory.TryRefreshVirtualDocuments(document);

        // Assert
        Assert.False(result);
        Assert.Single(document.VirtualDocuments, virtualDocument1);
    }
}
