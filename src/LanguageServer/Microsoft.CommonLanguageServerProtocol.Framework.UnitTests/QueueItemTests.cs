// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using StreamJsonRpc;
using Xunit;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public sealed class QueueItemTests
{
    [Fact]
    public async Task QueueItem_NonMutatingException_IsReportedToClientWithoutRethrowing()
    {
        Mock<ILspLogger> mockLogger = new(MockBehavior.Strict);
        mockLogger
            .Setup(l => l.LogDebug("Starting request handler", Array.Empty<object>()))
            .Verifiable();
        mockLogger
            .Setup(l => l.LogDebug(
                It.Is<string>(message => message.Contains(ThrowLocalRpcExceptionMethodHandler.ErrorMessage)), Array.Empty<object>()))
            .Verifiable();

        var handler = new ThrowLocalRpcExceptionMethodHandler(mutatesSolutionState: false);
        var lspServices = TestLspServices.Create(
            [(typeof(IMethodHandler), handler)],
            supportsMethodHandlerProvider: false);
        var (queueItem, clientTask) = QueueItem<int>.Create(
            ThrowLocalRpcExceptionMethodHandler.Name,
            null,
            lspServices,
            mockLogger.Object,
            CancellationToken.None);

        var throwCount = 0;
        void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (ReferenceEquals(e.Exception, handler.Exception))
                Interlocked.Increment(ref throwCount);
        }

        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        try
        {
            await queueItem.StartRequestAsync<object?, object?>(
                request: null,
                context: 1,
                handler,
                CancellationToken.None);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
        }

        Assert.Equal(1, Volatile.Read(ref throwCount));

        var exception = await Assert.ThrowsAsync<LocalRpcException>(() => clientTask);
        Assert.Same(handler.Exception, exception);
        Assert.Equal(LspErrorCodes.ContentModified, exception.ErrorCode);

        mockLogger.VerifyAll();
    }

    [Fact]
    public async Task QueueItem_MutatingException_IsRethrown()
    {
        Mock<ILspLogger> mockLogger = new(MockBehavior.Strict);
        mockLogger
            .Setup(l => l.LogDebug("Starting request handler", Array.Empty<object>()))
            .Verifiable();
        mockLogger
            .Setup(l => l.LogDebug(
                It.Is<string>(message => message.Contains(ThrowLocalRpcExceptionMethodHandler.ErrorMessage)), Array.Empty<object>()))
            .Verifiable();

        var handler = new ThrowLocalRpcExceptionMethodHandler(mutatesSolutionState: true);
        var lspServices = TestLspServices.Create(
            [(typeof(IMethodHandler), handler)],
            supportsMethodHandlerProvider: false);
        var (queueItem, clientTask) = QueueItem<int>.Create(
            ThrowLocalRpcExceptionMethodHandler.Name,
            null,
            lspServices,
            mockLogger.Object,
            CancellationToken.None);

        var queueException = await Assert.ThrowsAsync<LocalRpcException>(() =>
            queueItem.StartRequestAsync<object?, object?>(
                request: null,
                context: 1,
                handler,
                CancellationToken.None));
        var clientException = await Assert.ThrowsAsync<LocalRpcException>(() => clientTask);

        Assert.Same(handler.Exception, queueException);
        Assert.Same(handler.Exception, clientException);
        Assert.Equal(LspErrorCodes.ContentModified, clientException.ErrorCode);

        mockLogger.VerifyAll();
    }

    [Fact]
    public async Task QueueItem_MutatingException_IsRethrownAfterClientCancellation()
    {
        Mock<ILspLogger> mockLogger = new(MockBehavior.Strict);
        mockLogger
            .Setup(l => l.LogDebug("Starting request handler", Array.Empty<object>()))
            .Verifiable();
        mockLogger
            .Setup(l => l.LogDebug(
                It.Is<string>(message => message.Contains(ThrowLocalRpcExceptionMethodHandler.ErrorMessage)), Array.Empty<object>()))
            .Verifiable();

        var handler = new GatedThrowLocalRpcExceptionMethodHandler();
        var lspServices = TestLspServices.Create(
            [(typeof(IMethodHandler), handler)],
            supportsMethodHandlerProvider: false);
        using var cancellationTokenSource = new CancellationTokenSource();
        var (queueItem, clientTask) = QueueItem<int>.Create(
            ThrowLocalRpcExceptionMethodHandler.Name,
            null,
            lspServices,
            mockLogger.Object,
            cancellationTokenSource.Token);

        var requestTask = queueItem.StartRequestAsync<object?, object?>(
            request: null,
            context: 1,
            handler,
            CancellationToken.None);
        await handler.Started;

        cancellationTokenSource.Cancel();
        handler.AllowFailure();

        var queueException = await Assert.ThrowsAsync<LocalRpcException>(() => requestTask);
        var clientException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientTask);

        Assert.Same(handler.Exception, queueException);
        Assert.Equal(cancellationTokenSource.Token, clientException.CancellationToken);

        mockLogger.VerifyAll();
    }

    /// <summary>
    /// A canceled request is reported to the client through the completion source, so <see
    /// cref="QueueItem{TRequestContext}.StartRequestAsync"/> must not also rethrow it.  Every caller in <see
    /// cref="RequestExecutionQueue{TRequestContext}"/> discards that exception, so rethrowing only added a
    /// first-chance exception for each request superseded by the next keystroke.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task QueueItem_Cancellation_IsReportedToClientWithoutRethrowing(bool cancelBeforeHandlerRuns)
    {
        Mock<ILspLogger> mockLogger = new(MockBehavior.Strict);
        mockLogger
            .Setup(l => l.LogDebug("Starting request handler", Array.Empty<object>()))
            .Verifiable();
        mockLogger
            .Setup(l => l.LogDebug("Request was cancelled.", Array.Empty<object>()))
            .Verifiable();

        var handler = new ThrowCancellationMethodHandler();

        var lspServices = TestLspServices.Create(
            [(typeof(IMethodHandler), handler)],
            supportsMethodHandlerProvider: false);

        var (queueItem, clientTask) = QueueItem<int>.Create(
            ThrowCancellationMethodHandler.Name,
            null,
            lspServices,
            mockLogger.Object,
            CancellationToken.None);

        using var cancellationTokenSource = new CancellationTokenSource();
        if (cancelBeforeHandlerRuns)
            cancellationTokenSource.Cancel();

        // FirstChanceException is process wide, so identify the rethrow precisely: awaiting the canceled client
        // task is the only thing that produces a TaskCanceledException referencing that exact task instance.
        var rethrowCount = 0;
        void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is TaskCanceledException { Task: { } canceledTask } && ReferenceEquals(canceledTask, clientTask))
                Interlocked.Increment(ref rethrowCount);
        }

        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        try
        {
            await queueItem.StartRequestAsync<object?, object?>(
                request: null,
                context: 1,
                handler,
                cancellationTokenSource.Token);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
        }

        Assert.Equal(0, Volatile.Read(ref rethrowCount));

        // The handler only ran when it was not already canceled, but either way the client sees cancellation.
        Assert.Equal(!cancelBeforeHandlerRuns, handler.WasInvoked);
        Assert.True(clientTask.IsCanceled);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientTask);
        Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);

        mockLogger.VerifyAll();
    }

    private sealed class ThrowLocalRpcExceptionMethodHandler(bool mutatesSolutionState) : IRequestHandler<object?, object?, int>
    {
        public const string Name = "Test/Method";
        public const string ErrorMessage = "Request resolve version does not match current version";

        public LocalRpcException Exception { get; } = new(ErrorMessage) { ErrorCode = LspErrorCodes.ContentModified };

        public bool MutatesSolutionState => mutatesSolutionState;

        public Task<object?> HandleRequestAsync(object? request, int context, CancellationToken cancellationToken)
        {
            throw Exception;
        }
    }

    private sealed class ThrowCancellationMethodHandler : IRequestHandler<object?, object?, int>
    {
        public const string Name = "Test/CanceledMethod";

        public bool MutatesSolutionState => false;

        public bool WasInvoked { get; private set; }

        public Task<object?> HandleRequestAsync(object? request, int context, CancellationToken cancellationToken)
        {
            WasInvoked = true;
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class GatedThrowLocalRpcExceptionMethodHandler : IRequestHandler<object?, object?, int>
    {
        private readonly TaskCompletionSource<object?> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<object?> _allowFailure = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public LocalRpcException Exception { get; } = new(ThrowLocalRpcExceptionMethodHandler.ErrorMessage) { ErrorCode = LspErrorCodes.ContentModified };

        public bool MutatesSolutionState => true;

        public Task Started => _started.Task;

        public void AllowFailure()
            => _allowFailure.TrySetResult(null);

        public async Task<object?> HandleRequestAsync(object? request, int context, CancellationToken cancellationToken)
        {
            _started.TrySetResult(null);
            await _allowFailure.Task.ConfigureAwait(false);
            throw Exception;
        }
    }
}
