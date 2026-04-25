// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LiveShare;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

public class RazorGuestInitializationServiceTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly LiveShareSessionAccessor _liveShareSessionAccessor = new();

    [Fact]
    public async Task CreateServiceAsync_StartsViewImportsCopy()
    {
        // Arrange
        var service = new RazorGuestInitializationService(_liveShareSessionAccessor);
        var serviceAccessor = service.GetTestAccessor();
        var session = new StrictMock<CollaborationSession>();
        session
            .Setup(s => s.ListRootsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([])
            .Verifiable();

        // Act
        await service.CreateServiceAsync(session.Object, DisposalToken);

        // Assert
        Assert.NotNull(serviceAccessor.ViewImportsCopyTask);
        await serviceAccessor.ViewImportsCopyTask;

        session.VerifyAll();
    }

    [Fact]
    public async Task CreateServiceAsync_SessionDispose_CancelsListRootsToken()
    {
        // Arrange
        var service = new RazorGuestInitializationService(_liveShareSessionAccessor);
        var serviceAccessor = service.GetTestAccessor();
        var session = new StrictMock<CollaborationSession>();
        using var disposedServiceGate = new ManualResetEventSlim();
        var disposedService = false;
        IDisposable? sessionService = null;
        session
            .Setup(s => s.ListRootsAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>((cancellationToken) => Task.Run(() =>
                {
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                    // Make sure we don't assert the value of 'disposedService' before we know it was set
                    disposedServiceGate.Wait();

                    Assert.True(disposedService);
                    return Array.Empty<Uri>();
                }))
            .Verifiable();
        sessionService = (IDisposable)await service.CreateServiceAsync(session.Object, DisposalToken);

        // Act
        sessionService.Dispose();
        disposedService = true;
        disposedServiceGate.Set();

        // Assert
        Assert.NotNull(serviceAccessor.ViewImportsCopyTask);
        await serviceAccessor.ViewImportsCopyTask;

        session.VerifyAll();
    }

    [Fact]
    public async Task CreateServiceAsync_InitializationDispose_CancelsListRootsToken()
    {
        // Arrange
        var service = new RazorGuestInitializationService(_liveShareSessionAccessor);
        var serviceAccessor = service.GetTestAccessor();
        var session = new StrictMock<CollaborationSession>();
        using var cts = new CancellationTokenSource();
        IDisposable? sessionService = null;
        session
            .Setup(s => s.ListRootsAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>((cancellationToken) => Task.Run(() =>
                {
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                    Assert.True(cts.IsCancellationRequested);
                    return Array.Empty<Uri>();
                }))
            .Verifiable();
        sessionService = (IDisposable)await service.CreateServiceAsync(session.Object, cts.Token);

        // Act
        cts.Cancel();

        // Assert
        Assert.NotNull(serviceAccessor.ViewImportsCopyTask);
        await serviceAccessor.ViewImportsCopyTask;

        session.VerifyAll();
    }

    [Fact]
    public async Task CreateServiceAsync_EnsureViewImportsCopiedAsync_CancellationExceptionsGetSwallowed()
    {
        // Arrange
        var service = new RazorGuestInitializationService(_liveShareSessionAccessor);
        var serviceAcessor = service.GetTestAccessor();
        var session = new StrictMock<CollaborationSession>();
        using var cts = new CancellationTokenSource();
        IDisposable? sessionService = null;
        session
            .Setup(s => s.ListRootsAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>((cancellationToken) => Task.Run(() =>
                {
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                    cancellationToken.ThrowIfCancellationRequested();

                    return Array.Empty<Uri>();
                }))
            .Verifiable();
        sessionService = (IDisposable)await service.CreateServiceAsync(session.Object, cts.Token);

        // Act
        cts.Cancel();

        // Assert
        Assert.NotNull(serviceAcessor.ViewImportsCopyTask);
        await serviceAcessor.ViewImportsCopyTask;

        session.VerifyAll();
    }
}
