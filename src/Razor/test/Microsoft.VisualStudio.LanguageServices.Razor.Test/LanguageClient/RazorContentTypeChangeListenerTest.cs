// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

public class RazorContentTypeChangeListenerTest : ToolingTestBase
{
    private readonly IContentType _nonRazorContentType;
    private readonly IContentType _razorContentType;
    private readonly ITextBuffer _razorBuffer;
    private readonly ITextDocument _razorTextDocument;
    private readonly ITextBuffer _disposedRazorBuffer;

    public RazorContentTypeChangeListenerTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _nonRazorContentType = Mock.Of<IContentType>(
            c => c.IsOfType(It.IsAny<string>()) == false,
            MockBehavior.Strict);

        _razorContentType = Mock.Of<IContentType>(
            c => c.IsOfType(RazorConstants.RazorLSPContentTypeName) == true,
            MockBehavior.Strict);

        _razorBuffer ??= Mock.Of<ITextBuffer>(
            b => b.ContentType == _razorContentType && b.Properties == new PropertyCollection(),
            MockBehavior.Strict);

        _disposedRazorBuffer ??= Mock.Of<ITextBuffer>(
            b => b.ContentType == _nonRazorContentType && b.Properties == new PropertyCollection(),
            MockBehavior.Strict);

        _razorTextDocument = Mock.Of<ITextDocument>(
            td => td.TextBuffer == _razorBuffer && td.FilePath == "C:/path/to/file.razor",
            MockBehavior.Strict);
    }

    [Fact]
    public void RazorBufferCreated_TracksDocument()
    {
        // Arrange
        var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
            .Verifiable();
        var listener = CreateListener(lspDocumentManager.Object);

        // Act
        listener.RazorBufferCreated(_razorBuffer);

        // Assert
        lspDocumentManager.VerifyAll();
    }

    [Fact]
    public void RazorBufferCreated_RemoteClient_DoesNotTrackDocument()
    {
        // Arrange
        var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
            .Throws<Exception>();
        var featureDetector = Mock.Of<ILspEditorFeatureDetector>(detector => detector.IsRemoteClient() == true, MockBehavior.Strict);
        var listener = CreateListener(lspDocumentManager.Object, featureDetector);

        // Act & Assert
        listener.RazorBufferCreated(_razorBuffer);
    }

    [Fact]
    public void RazorBufferDisposed_Untracks()
    {
        // Arrange
        var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
            .Verifiable();
        var listener = CreateListener(lspDocumentManager.Object);

        // Act
        listener.RazorBufferDisposed(_disposedRazorBuffer);

        // Assert
        lspDocumentManager.VerifyAll();
    }

    [Theory]
    [InlineData(FileActionTypes.ContentSavedToDisk)]
    [InlineData(FileActionTypes.ContentLoadedFromDisk)]
    public void TextDocument_FileActionOccurred_NonRenameEvent_Noops(FileActionTypes fileActionType)
    {
        // Arrange
        var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
            .Throws<Exception>();
        lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
            .Throws<Exception>();
        var listener = CreateListener(lspDocumentManager.Object);
        var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, fileActionType);

        // Act & Assert
        listener.TextDocument_FileActionOccurred(_razorTextDocument, args);
    }

    [Fact]
    public void TextDocument_FileActionOccurred_NonTextDocument_Noops()
    {
        // Arrange
        var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
            .Throws<Exception>();
        lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
            .Throws<Exception>();
        var listener = CreateListener(lspDocumentManager.Object);
        var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, FileActionTypes.DocumentRenamed);

        // Act & Assert
        listener.TextDocument_FileActionOccurred(_razorBuffer, args);
    }

    [Fact]
    public void TextDocument_FileActionOccurred_NoAssociatedBuffer_Noops()
    {
        // Arrange
        var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
            .Throws<Exception>();
        lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
            .Throws<Exception>();
        var listener = CreateListener(lspDocumentManager.Object);
        var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, FileActionTypes.DocumentRenamed);
        var textDocument = new Mock<ITextDocument>(MockBehavior.Strict).Object;
        Mock.Get(textDocument).SetupGet(d => d.TextBuffer).Returns(value: null);

        // Act & Assert
        listener.TextDocument_FileActionOccurred(textDocument, args);
    }

    [Fact]
    public void TextDocument_FileActionOccurred_Rename_UntracksAndThenTracks()
    {
        // Arrange
        var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        var tracked = false;
        var untracked = false;
        lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
            .Callback(() =>
            {
                Assert.False(tracked);
                Assert.True(untracked);

                tracked = true;
            })
            .Verifiable();
        lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
            .Callback(() =>
            {
                Assert.False(tracked);
                Assert.False(untracked);
                untracked = true;
            })
            .Verifiable();
        var listener = CreateListener(lspDocumentManager.Object);
        var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, FileActionTypes.DocumentRenamed);

        // Act
        listener.TextDocument_FileActionOccurred(_razorTextDocument, args);

        // Assert
        lspDocumentManager.VerifyAll();
    }

    [Fact]
    public void TextDocument_FileActionOccurred_Rename_UntracksOnlyIfNotRenamedToRazorContentType()
    {
        // Arrange
        var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        var fileToContentTypeService = Mock.Of<IFileToContentTypeService>(detector => detector.GetContentTypeForFilePath(It.IsAny<string>()) == _nonRazorContentType, MockBehavior.Strict);
        var tracked = false;
        var untracked = false;
        lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
            .Callback(() =>
            {
                Assert.False(tracked);
                Assert.False(untracked);
                untracked = true;
            })
            .Verifiable();
        var listener = CreateListener(lspDocumentManager.Object, fileToContentTypeService: fileToContentTypeService);
        var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, FileActionTypes.DocumentRenamed);

        // Act
        listener.TextDocument_FileActionOccurred(_razorTextDocument, args);

        // Assert
        lspDocumentManager.VerifyAll();
    }

    private RazorContentTypeChangeListener CreateListener(
        TrackingLSPDocumentManager lspDocumentManager = null,
        ILspEditorFeatureDetector lspEditorFeatureDetector = null,
        IFileToContentTypeService fileToContentTypeService = null)
    {
        var textDocumentFactory = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict).Object;
        Mock.Get(textDocumentFactory).Setup(f => f.TryGetTextDocument(It.IsAny<ITextBuffer>(), out It.Ref<ITextDocument>.IsAny)).Returns(false);

        lspDocumentManager ??= Mock.Of<TrackingLSPDocumentManager>(MockBehavior.Strict);
        lspEditorFeatureDetector ??= Mock.Of<ILspEditorFeatureDetector>(detector =>
            detector.IsLspEditorSupported(It.IsAny<string>()) == true &&
            detector.IsRemoteClient() == false, MockBehavior.Strict);
        fileToContentTypeService ??= Mock.Of<IFileToContentTypeService>(detector => detector.GetContentTypeForFilePath(It.IsAny<string>()) == _razorContentType, MockBehavior.Strict);
        var textManager = new Mock<IVsTextManager2>(MockBehavior.Strict);
        textManager.Setup(m => m.GetUserPreferences2(null, null, It.IsAny<LANGPREFERENCES2[]>(), null)).Returns(VSConstants.E_NOTIMPL);
        var listener = new RazorContentTypeChangeListener(
            textDocumentFactory,
            lspDocumentManager,
            lspEditorFeatureDetector,
            fileToContentTypeService);

        return listener;
    }
}
