// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Extensions;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public class DefaultLSPDocumentSynchronizerTest : ToolingTestBase
{
    private readonly ITextSnapshot _virtualDocumentSnapshot;
    private readonly ITextBuffer _virtualDocumentTextBuffer;

    public DefaultLSPDocumentSynchronizerTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        var snapshot = new StringTextSnapshot("Hello World");
        var buffer = new TestTextBuffer(snapshot);
        _virtualDocumentTextBuffer = buffer;
        snapshot.TextBuffer = buffer;
        _virtualDocumentSnapshot = snapshot;
    }

    private TrackingLSPDocumentManager GetDocumentManager(bool useDocumentManager = false, LSPDocumentSnapshot documentSnapshot = null)
    {
        var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
        if (useDocumentManager)
        {
            documentManager.Setup(m => m.TryGetDocument(It.IsAny<Uri>(), out documentSnapshot))
                .Returns(true);
        }

        return documentManager.Object;
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_RemovedDocument_ReturnsFalse()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 123, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager());
        NotifyLSPDocumentAdded(lspDocument, synchronizer);
        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, virtualDocument.HostDocumentSyncVersion.Value);
        NotifyLSPDocumentRemoved(lspDocument, synchronizer);

        // Act
#pragma warning disable CS0612 // Type or member is obsolete
        var result = await synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_SynchronizedDocument_ReturnsTrue()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 123, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager());
        NotifyLSPDocumentAdded(lspDocument, synchronizer);
        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, virtualDocument.HostDocumentSyncVersion.Value);

        // Act
        var result = await synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_SynchronizesAfterUpdate_ReturnsTrue()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager())
        {
            _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
        };
        NotifyLSPDocumentAdded(lspDocument, synchronizer);

        // Act

        // Start synchronization, this will hang until we notify the buffer versions been updated because
        // the above virtual document expects host doc version 123 but the host doc is 124
        var synchronizeTask = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);

        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, lspDocument.Version);
        var result = await synchronizeTask;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_SpecificDocuments_SynchronizesAfterUpdate_ReturnsTrue()
    {
        // Arrange
        var snapshot1 = new StringTextSnapshot("doc1");
        var buffer1 = new TestTextBuffer(snapshot1);
        snapshot1.TextBuffer = buffer1;
        var snapshot2 = new StringTextSnapshot("doc2");
        var buffer2 = new TestTextBuffer(snapshot2);
        snapshot2.TextBuffer = buffer2;

        var virtualDocumentUri1 = new Uri("C:/path/to/1/file.razor__virtual.cs");
        var virtualDocument1 = new TestVirtualDocumentSnapshot(virtualDocumentUri1, 1, snapshot1, state: null);
        var virtualDocumentUri2 = new Uri("C:/path/to/2/file.razor__virtual.cs");
        var virtualDocument2 = new TestVirtualDocumentSnapshot(virtualDocumentUri2, 1, snapshot2, state: null);
        var documentUri = new Uri("C:/path/to/file.razor");
        LSPDocumentSnapshot lspDocument = new TestLSPDocumentSnapshot(documentUri, 2, virtualDocument1, virtualDocument2);

        var fileUriProvider = Mock.Of<FileUriProvider>(provider => provider.TryGet(buffer1, out virtualDocumentUri1) == true &&
            provider.TryGet(buffer2, out virtualDocumentUri2) == true, MockBehavior.Strict);
        var documentManager = Mock.Of<LSPDocumentManager>(m => m.TryGetDocument(documentUri, out lspDocument) == true, MockBehavior.Strict);

        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, documentManager)
        {
            // Slow things down so even on slow CI machines, we still validate that updating doc 1 doesn't release the task for doc 2
            _synchronizationTimeout = TimeSpan.FromSeconds(5)
        };
        NotifyLSPDocumentAdded(lspDocument, synchronizer);

        // Act

        // Start synchronization, this should block until we notify the buffer versions been updated for doc 1
        var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync<TestVirtualDocumentSnapshot>(lspDocument.Version, documentUri, virtualDocumentUri1, rejectOnNewerParallelRequest: true, DisposalToken);

        // Start synchronization for doc 2, this should block until we notify the buffer versions been updated for doc 2
        var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync<TestVirtualDocumentSnapshot>(lspDocument.Version, documentUri, virtualDocumentUri2, rejectOnNewerParallelRequest: true, DisposalToken);

        NotifyBufferVersionUpdated(buffer1, lspDocument.Version);

        var result = await synchronizeTask1;

        // Assert
        Assert.True(result.Synchronized);
        // Only virtual doc 1 should have been synchronized
        Assert.False(synchronizeTask2.IsCompleted);

        // Now update doc 2
        NotifyBufferVersionUpdated(buffer2, lspDocument.Version);
        result = await synchronizeTask2;

        // Assert
        Assert.True(result.Synchronized);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_SimultaneousEqualSynchronizationRequests_ReturnsTrue()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager())
        {
            _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
        };
        NotifyLSPDocumentAdded(lspDocument, synchronizer);

        // Act

        // Start synchronization, this will hang until we notify the buffer versions been updated because
        // the above virtual document expects host doc version 123 but the host doc is 124
        var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);
        var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);

        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, lspDocument.Version);
        var result1 = await synchronizeTask1;
        var result2 = await synchronizeTask2;

        // Assert
        Assert.True(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_SimultaneousDifferentSynchronizationRequests_CancelsFirst_ReturnsFalseThenTrue()
    {
        // Arrange
        var (originalLSPDocument, originalVirtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, originalVirtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager())
        {
            _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
        };
        NotifyLSPDocumentAdded(originalLSPDocument, synchronizer);

        // Act

        // Start synchronization, this will hang until we notify the buffer versions been updated because
        // the above virtual document expects host doc version 123 but the host doc is 124
        var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(originalLSPDocument.Version, originalVirtualDocument, DisposalToken);

        // Start another synchronization that will also hang because 124 != 125. However, this synchronization
        // request is for the same addressable virtual document (same URI) therefore requesting a second
        // synchronization with a different host doc version expectation will cancel the original synchronization
        // request resulting it returning false.
        var (newLSPDocument, newVirtualDocument) = CreateDocuments(lspDocumentVersion: 125, virtualDocumentSyncVersion: 124);
        var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(newLSPDocument.Version, newVirtualDocument, DisposalToken);

        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, newLSPDocument.Version);
        var result1 = await synchronizeTask1;
        var result2 = await synchronizeTask2;

        // Assert
        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_SimultaneousSynchronizationRequests_PlatformCancelsFirst_ReturnsFalseThenTrue()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager())
        {
            _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
        };
        NotifyLSPDocumentAdded(lspDocument, synchronizer);
        using var cts = new CancellationTokenSource();

        // Act

        // Start synchronization, this will hang until we notify the buffer versions been updated because
        // the above virtual document expects host doc version 123 but the host doc is 124
        var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, cts.Token);
        var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);

        cts.Cancel();

        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, lspDocument.Version);
        var result1 = await synchronizeTask1;
        var result2 = await synchronizeTask2;

        // Assert
        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_NewerVersionRequested_CancelsActiveRequest()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager())
        {
            _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
        };
        NotifyLSPDocumentAdded(lspDocument, synchronizer);

        // Act

        // Start synchronization, this will hang until we notify the buffer versions been updated because
        // the above virtual document expects host doc version 123 but the host doc is 124
        var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);
        var (newLSPDocument, newVirtualDocument) = CreateDocuments(lspDocumentVersion: 125, virtualDocumentSyncVersion: 124);
        var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(newLSPDocument.Version, newVirtualDocument, DisposalToken);

        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, lspDocument.Version);
        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, newLSPDocument.Version);

        var result1 = await synchronizeTask1;
        var result2 = await synchronizeTask2;

        // Assert
        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_RejectOnNewerParallelRequest_NewerVersionRequested_CancelsActiveRequest()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager())
        {
            _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
        };
        NotifyLSPDocumentAdded(lspDocument, synchronizer);

        // Act

        // Start synchronization, this will hang until we notify the buffer versions been updated because
        // the above virtual document expects host doc version 123 but the host doc is 124
        var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, rejectOnNewerParallelRequest: false, DisposalToken);
        var (newLSPDocument, newVirtualDocument) = CreateDocuments(lspDocumentVersion: 125, virtualDocumentSyncVersion: 124);
        var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(newLSPDocument.Version, newVirtualDocument, DisposalToken);

        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, lspDocument.Version);
        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, newLSPDocument.Version);

        var result1 = await synchronizeTask1;
        var result2 = await synchronizeTask2;

        // Assert
        Assert.True(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_DocumentRemoved_CancelsActiveRequests()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager())
        {
            _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
        };
        NotifyLSPDocumentAdded(lspDocument, synchronizer);

        var synchronizedTask = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);

        // Act
        NotifyLSPDocumentRemoved(lspDocument, synchronizer);
        NotifyBufferVersionUpdated(_virtualDocumentTextBuffer, lspDocument.Version);

        var result = await synchronizedTask;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TrySynchronizeVirtualDocumentAsync_Timeout_ReturnsFalse()
    {
        // Arrange
        var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 123, virtualDocumentSyncVersion: 123);
        var fileUriProvider = CreateUriProviderFor(_virtualDocumentTextBuffer, virtualDocument.Uri);
        var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider, GetDocumentManager())
        {
            _synchronizationTimeout = TimeSpan.FromMilliseconds(10)
        };
        NotifyLSPDocumentAdded(lspDocument, synchronizer);

        // We're not going to notify that the buffer version was updated so the synchronization will wait until a timeout occurs.

        // Act
        var result = await synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, DisposalToken);

        // Assert
        Assert.False(result);
    }
#pragma warning restore CS0612 // Type or member is obsolete
    private static void NotifyLSPDocumentAdded(LSPDocumentSnapshot lspDocument, DefaultLSPDocumentSynchronizer synchronizer)
    {
        synchronizer.Changed(old: null, @new: lspDocument, virtualOld: null, virtualNew: null, LSPDocumentChangeKind.Added);
    }

    private static void NotifyLSPDocumentRemoved(LSPDocumentSnapshot lspDocument, DefaultLSPDocumentSynchronizer synchronizer)
    {
        synchronizer.Changed(old: lspDocument, @new: null, virtualOld: null, virtualNew: null, LSPDocumentChangeKind.Removed);
    }

    private (TestLSPDocumentSnapshot, TestVirtualDocumentSnapshot) CreateDocuments(int lspDocumentVersion, long virtualDocumentSyncVersion)
    {
        var virtualDocumentUri = new Uri("C:/path/to/file.razor__virtual.cs");
        var virtualDocument = new TestVirtualDocumentSnapshot(virtualDocumentUri, virtualDocumentSyncVersion, _virtualDocumentSnapshot, state: null);
        var documentUri = new Uri("C:/path/to/file.razor");
        var document = new TestLSPDocumentSnapshot(documentUri, lspDocumentVersion, virtualDocument);

        return (document, virtualDocument);
    }

    private static FileUriProvider CreateUriProviderFor(ITextBuffer textBuffer, Uri bufferUri)
    {
        var fileUriProvider = Mock.Of<FileUriProvider>(provider => provider.TryGet(textBuffer, out bufferUri) == true, MockBehavior.Strict);
        return fileUriProvider;
    }

    private static void NotifyBufferVersionUpdated(ITextBuffer textBuffer, long hostDocumentSyncVersion)
    {
        textBuffer.SetHostDocumentSyncVersion(hostDocumentSyncVersion);
        var edit = textBuffer.CreateEdit();

        // Content doesn't matter, we're just trying to create an edit that notifies listeners of a changed event.
        edit.Insert(0, "Test");
        edit.Apply();
    }
}
