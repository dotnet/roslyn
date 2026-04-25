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

public class DefaultLSPDocumentManagerTest : ToolingTestBase
{
    private readonly IEnumerable<Lazy<LSPDocumentChangeListener, IContentTypeMetadata>> _changeListeners;
    private readonly ITextBuffer _textBuffer;
    private readonly Uri _uri;
    private readonly FileUriProvider _uriProvider;
    private readonly LSPDocumentFactory _lspDocumentFactory;
    private readonly LSPDocument _lspDocument;
    private readonly LSPDocumentSnapshot _lspDocumentSnapshot;

    public DefaultLSPDocumentManagerTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        var contentType = Mock.Of<IContentType>(contentType =>
            contentType.IsOfType("inert") == false &&
            contentType.IsOfType("test") == true &&
            contentType.TypeName == "test",
            MockBehavior.Strict);
        _changeListeners = Enumerable.Empty<Lazy<LSPDocumentChangeListener, IContentTypeMetadata>>();
        _textBuffer = new TestTextBuffer(new StringTextSnapshot(string.Empty));
        _textBuffer.ChangeContentType(contentType, editTag: null);
        var snapshot = _textBuffer.CurrentSnapshot;

        _uri = new Uri("C:/path/to/file.razor");
        _uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(_textBuffer) == _uri, MockBehavior.Strict);
        Mock.Get(_uriProvider).Setup(p => p.Remove(It.IsAny<ITextBuffer>())).Verifiable();
        var testVirtualDocument = new TestVirtualDocument();
        var lspDocument = new DefaultLSPDocument(_uri, _textBuffer, new[] { testVirtualDocument });
        _lspDocumentSnapshot = lspDocument.CurrentSnapshot;
        _lspDocument = lspDocument;
        _lspDocumentFactory = Mock.Of<LSPDocumentFactory>(factory => factory.Create(_textBuffer) == _lspDocument, MockBehavior.Strict);
    }

    [Fact]
    public void TrackDocument_TriggersDocumentAdded()
    {
        // Arrange
        var changeListenerLazy = CreateChangeListenerForContentTypes(new[] { _lspDocumentSnapshot.Snapshot.ContentType.TypeName });

        var changeListenerMock = Mock.Get(changeListenerLazy.Value);
        changeListenerMock.Setup(l => l.Changed(null, _lspDocumentSnapshot, It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.Added));

        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, _uriProvider, _lspDocumentFactory, new[] { changeListenerLazy });

        // Act
        manager.TrackDocument(_textBuffer);

        // Assert
        changeListenerMock.Verify(l => l.Changed(null, _lspDocumentSnapshot, It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.Added),
                                       Times.Once);
    }

    [Fact]
    public void UntrackDocument_TriggersDocumentRemoved()
    {
        // Arrange
        var changeListenerLazy = CreateChangeListenerForContentTypes(new[] { _lspDocumentSnapshot.Snapshot.ContentType.TypeName });

        var changeListenerMock = Mock.Get(changeListenerLazy.Value);
        changeListenerMock.Setup(l => l.Changed(null, _lspDocumentSnapshot, It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.Added));
        changeListenerMock.Setup(l => l.Changed(_lspDocumentSnapshot, null, It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.Removed));

        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, _uriProvider, _lspDocumentFactory, new[] { changeListenerLazy });

        manager.TrackDocument(_textBuffer);

        // We're untracking which is typically paired with the buffer going to the inert content type, lets emulate that to ensure document removed happens.
        _textBuffer.ChangeContentType(TestInertContentType.Instance, editTag: false);

        // Act
        manager.UntrackDocument(_textBuffer);

        // Assert
        changeListenerMock.Verify(l => l.Changed(_lspDocumentSnapshot, null, It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.Removed),
                                       Times.Once);
    }

    [Fact]
    public void UpdateVirtualDocument_Noops_UnknownDocument()
    {
        // Arrange
        var changeListenerLazy = CreateChangeListenerForContentTypes(new[] { _lspDocumentSnapshot.Snapshot.ContentType.TypeName });

        var changeListenerMock = Mock.Get(changeListenerLazy.Value);
        changeListenerMock.Setup(l => l.Changed(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<LSPDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<LSPDocumentChangeKind>()));

        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, _uriProvider, _lspDocumentFactory, new[] { changeListenerLazy });
        var changes = new[] { new VisualStudioTextChange(1, 1, string.Empty) };

        // Act
        manager.UpdateVirtualDocument<TestVirtualDocument>(_uri, changes, 123, state: null);

        // Assert
        changeListenerMock.Verify(l => l.Changed(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<LSPDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<LSPDocumentChangeKind>()),
                                       Times.Never);
    }

    [Fact]
    public void UpdateVirtualDocument_Noops_NoChangesSameVersion()
    {
        // Arrange
        var changeListenerLazy = CreateChangeListenerForContentTypes(new[] { _lspDocumentSnapshot.Snapshot.ContentType.TypeName });

        var changeListenerMock = Mock.Get(changeListenerLazy.Value);
        changeListenerMock.Setup(l => l.Changed(null, _lspDocumentSnapshot, It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.Added));
        changeListenerMock.Setup(l => l.Changed(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<LSPDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.VirtualDocumentChanged));

        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, _uriProvider, _lspDocumentFactory, new[] { changeListenerLazy });
        manager.TrackDocument(_textBuffer);

        var changes = Array.Empty<ITextChange>();

        // Act
        manager.UpdateVirtualDocument<TestVirtualDocument>(_uri, changes, 123, state: null);

        // Assert
        changeListenerMock.Verify(l => l.Changed(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<LSPDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.VirtualDocumentChanged),
                                       Times.Never);
    }

    [Fact]
    public void UpdateVirtualDocument_InvokesVirtualDocumentChanged()
    {
        // Arrange
        var changeListenerLazy = CreateChangeListenerForContentTypes(new[] { _lspDocumentSnapshot.Snapshot.ContentType.TypeName });

        var changeListenerMock = Mock.Get(changeListenerLazy.Value);
        changeListenerMock.Setup(l => l.Changed(null, _lspDocumentSnapshot, It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.Added));
        changeListenerMock.Setup(l => l.Changed(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<LSPDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.VirtualDocumentChanged));

        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, _uriProvider, _lspDocumentFactory, new[] { changeListenerLazy });
        manager.TrackDocument(_textBuffer);

        var changes = new[] { new VisualStudioTextChange(1, 1, string.Empty) };

        // Act
        manager.UpdateVirtualDocument<TestVirtualDocument>(_uri, changes, 123, state: null);

        // Assert
        changeListenerMock.Verify(l => l.Changed(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<LSPDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.VirtualDocumentChanged),
                                       Times.Once);
    }

    [Fact]
    public void UpdateVirtualDocument_SpecificVirtualDocument_InvokesVirtualDocumentChanged()
    {
        // Arrange
        var changeListenerLazy = CreateChangeListenerForContentTypes(new[] { _lspDocumentSnapshot.Snapshot.ContentType.TypeName });

        var testVirtualDocument1 = new TestVirtualDocument(new Uri("C:/path/to/doc1.razor.g.cs"));
        var testVirtualDocument2 = new TestVirtualDocument(new Uri("C:/path/to/doc2.razor.g.cs"));

        var lspDocument = new DefaultLSPDocument(_uri, _textBuffer, new[] { testVirtualDocument1, testVirtualDocument2 });
        var lspDocumentFactory = Mock.Of<LSPDocumentFactory>(factory => factory.Create(_textBuffer) == lspDocument, MockBehavior.Strict);
        var lspDocumentSnapshot = lspDocument.CurrentSnapshot;

        var changeListenerMock = Mock.Get(changeListenerLazy.Value);
        changeListenerMock.Setup(l => l.Changed(null, lspDocumentSnapshot, It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.Added));
        changeListenerMock.Setup(l => l.Changed(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<LSPDocumentSnapshot>(), testVirtualDocument2.CurrentSnapshot, It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.VirtualDocumentChanged));

        var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(_textBuffer) == lspDocument.Uri, MockBehavior.Strict);

        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, uriProvider, lspDocumentFactory, new[] { changeListenerLazy });
        manager.TrackDocument(_textBuffer);

        var changes = new[] { new VisualStudioTextChange(1, 1, string.Empty) };

        // Act
        manager.UpdateVirtualDocument<TestVirtualDocument>(lspDocument.Uri, testVirtualDocument2.Uri, changes, 123, state: null);

        // Assert
        changeListenerMock.Verify(l => l.Changed(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<LSPDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), LSPDocumentChangeKind.VirtualDocumentChanged),
                                       Times.Once);
    }

    [Fact]
    public void TryGetDocument_TrackedDocument_ReturnsTrue()
    {
        // Arrange
        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, _uriProvider, _lspDocumentFactory, _changeListeners);
        manager.TrackDocument(_textBuffer);

        // Act
        var result = manager.TryGetDocument(_uri, out var lspDocument);

        // Assert
        Assert.True(result);
        Assert.Same(_lspDocumentSnapshot, lspDocument);
    }

    [Fact]
    public void TryGetDocument_UnknownDocument_ReturnsFalse()
    {
        // Arrange
        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, _uriProvider, _lspDocumentFactory, _changeListeners);

        // Act
        var result = manager.TryGetDocument(_uri, out var lspDocument);

        // Assert
        Assert.False(result);
        Assert.Null(lspDocument);
    }

    [Fact]
    public void TryGetDocument_UntrackedDocument_ReturnsFalse()
    {
        // Arrange
        var manager = new DefaultLSPDocumentManager(JoinableTaskContext, _uriProvider, _lspDocumentFactory, _changeListeners);
        manager.TrackDocument(_textBuffer);
        manager.UntrackDocument(_textBuffer);

        // Act
        var result = manager.TryGetDocument(_uri, out var lspDocument);

        // Assert
        Assert.False(result);
        Assert.Null(lspDocument);
    }

    private static Lazy<LSPDocumentChangeListener, IContentTypeMetadata> CreateChangeListenerForContentTypes(IEnumerable<string> contentTypes)
    {
        var changeListenerObj = Mock.Of<LSPDocumentChangeListener>(MockBehavior.Strict);

        var metadata = Mock.Of<IContentTypeMetadata>(md =>
            md.ContentTypes == contentTypes,
            MockBehavior.Strict);

        return new Lazy<LSPDocumentChangeListener, IContentTypeMetadata>(() => changeListenerObj, metadata);
    }

    private class TestVirtualDocument : VirtualDocument
    {
        private readonly Uri _uri;

        public override Uri Uri => _uri ?? throw new NotImplementedException();

        public override ITextBuffer TextBuffer => throw new NotImplementedException();

        public override VirtualDocumentSnapshot CurrentSnapshot { get; } = new TestVirtualDocumentSnapshot(new Uri("C:/path/to/something.razor.g.cs"), 123);

        public override int HostDocumentVersion => 123;

        public TestVirtualDocument(Uri uri = null)
        {
            _uri = uri;
        }

        public override VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, int hostDocumentVersion, object state)
        {
            return CurrentSnapshot;
        }

        public override void Dispose()
        {
        }
    }
}
