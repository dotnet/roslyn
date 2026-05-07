// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

public class GuestProjectPathProviderTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [UIFact]
    public void TryGetProjectPath_GuestSessionNotActive_ReturnsFalse()
    {
        // Arrange
        var sessionAccessor = StrictMock.Of<ILiveShareSessionAccessor>(a =>
            a.IsGuestSessionActive == false);
        var textBuffer = StrictMock.Of<ITextBuffer>();
        var textDocument = StrictMock.Of<ITextDocument>();
        var textDocumentFactory = StrictMock.Of<ITextDocumentFactoryService>(f =>
            f.TryGetTextDocument(textBuffer, out textDocument) == true);

        var projectPathProvider = new GuestProjectPathProvider(
            JoinableTaskContext,
            textDocumentFactory,
            StrictMock.Of<IProxyAccessor>(),
            sessionAccessor);

        // Act
        var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

        // Assert
        Assert.False(result);
        Assert.Null(filePath);
    }

    [UIFact]
    public void TryGetProjectPath_NoTextDocument_ReturnsFalse()
    {
        // Arrange
        var collaborationSession = StrictMock.Of<CollaborationSession>();

        var sessionAccessor = StrictMock.Of<ILiveShareSessionAccessor>(a =>
            a.IsGuestSessionActive == true &&
            a.Session == collaborationSession);

        var textBuffer = StrictMock.Of<ITextBuffer>();
        var textDocumentFactoryServiceMock = new StrictMock<ITextDocumentFactoryService>();
        textDocumentFactoryServiceMock
            .Setup(s => s.TryGetTextDocument(It.IsAny<ITextBuffer>(), out It.Ref<ITextDocument>.IsAny))
            .Returns(false);

        var projectPathProvider = new GuestProjectPathProvider(
            JoinableTaskContext,
            textDocumentFactoryServiceMock.Object,
            StrictMock.Of<IProxyAccessor>(),
            sessionAccessor);

        // Act
        var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

        // Assert
        Assert.False(result);
        Assert.Null(filePath);
    }

    [UIFact]
    public void TryGetProjectPath_NullHostProjectPath_ReturnsFalse()
    {
        // Arrange
        var documentFilePath = "/path/to/document.razor";
        var documentFilePathUri = new Uri("vsls:" + documentFilePath);
        var projectFilePath = "/path/to/project.razor";
        var projectFilePathUri = new Uri("vsls:" + projectFilePath);

        var collaborationSessionMock = new StrictMock<CollaborationSession>();
        collaborationSessionMock
            .Setup(x => x.ConvertLocalPathToSharedUri(documentFilePath))
            .Returns(documentFilePathUri);
        collaborationSessionMock
            .Setup(x => x.ConvertSharedUriToLocalPath(projectFilePathUri))
            .Returns(projectFilePath);

        var sessionAccessor = StrictMock.Of<ILiveShareSessionAccessor>(a =>
            a.IsGuestSessionActive == true &&
            a.Session == collaborationSessionMock.Object);

        var textBuffer = StrictMock.Of<ITextBuffer>();
        var textDocument = StrictMock.Of<ITextDocument>(d =>
            d.FilePath == documentFilePath);
        var textDocumentFactory = StrictMock.Of<ITextDocumentFactoryService>(f =>
            f.TryGetTextDocument(textBuffer, out textDocument) == true);

        var proxy = new StrictMock<IProjectHierarchyProxy>();
        proxy
            .Setup(x => x.GetProjectPathAsync(documentFilePathUri, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Uri?)null);

        var proxyAccessor = StrictMock.Of<IProxyAccessor>(a =>
            a.GetProjectHierarchyProxy() == proxy.Object);

        var projectPathProvider = new GuestProjectPathProvider(
            JoinableTaskContext,
            textDocumentFactory,
            proxyAccessor,
            sessionAccessor);

        // Act
        var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

        // Assert
        Assert.False(result);
        Assert.Null(filePath);
    }

    [UIFact]
    public void TryGetProjectPath_ValidHostProjectPath_ReturnsTrueWithGuestNormalizedPath()
    {
        // Arrange
        var documentFilePath = "/path/to/document.razor";
        var documentFilePathUri = new Uri("vsls:" + documentFilePath);
        var projectFilePath = "/path/to/project.csproj";
        var projectFilePathUri = new Uri("vsls:" + projectFilePath);

        var collaborationSessionMock = new StrictMock<CollaborationSession>();
        collaborationSessionMock
            .Setup(x => x.ConvertLocalPathToSharedUri(documentFilePath))
            .Returns(documentFilePathUri);
        collaborationSessionMock
            .Setup(x => x.ConvertSharedUriToLocalPath(projectFilePathUri))
            .Returns(projectFilePath);

        var sessionAccessor = StrictMock.Of<ILiveShareSessionAccessor>(a =>
            a.IsGuestSessionActive == true &&
            a.Session == collaborationSessionMock.Object);

        var textBuffer = StrictMock.Of<ITextBuffer>();
        var textDocument = StrictMock.Of<ITextDocument>(d =>
            d.FilePath == documentFilePath);
        var textDocumentFactory = StrictMock.Of<ITextDocumentFactoryService>(f =>
            f.TryGetTextDocument(textBuffer, out textDocument) == true);

        var proxy = new StrictMock<IProjectHierarchyProxy>();
        proxy
            .Setup(x => x.GetProjectPathAsync(documentFilePathUri, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilePathUri)
            .Verifiable();

        var proxyAccessor = StrictMock.Of<IProxyAccessor>(a =>
            a.GetProjectHierarchyProxy() == proxy.Object);

        var projectPathProvider = new GuestProjectPathProvider(
            JoinableTaskContext,
            textDocumentFactory,
            proxyAccessor,
            sessionAccessor);

        // Act
        var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

        // Assert
        Assert.True(result);
        Assert.Equal(projectFilePath, filePath);

        proxy.Verify();
    }
}
