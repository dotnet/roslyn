// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework.Example;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using StreamJsonRpc;
using Xunit;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public sealed class RequestExecutionQueueTests
{
    private sealed class MockServer : SystemTextJsonLanguageServer<TestRequestContext>
    {
        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new SystemTextJsonFormatter().JsonSerializerOptions.AddLspSerializerOptions();

        public MockServer() : base(new JsonRpc(new HeaderDelimitedMessageHandler(FullDuplexStream.CreatePair().Item1)), s_jsonSerializerOptions)
        {
        }

        protected override ILspServices ConstructLspServices() => RequestExecutionQueueTests.GetLspServices();
    }

    private static TestRequestExecutionQueue GetRequestExecutionQueue(
        bool cancelInProgressWorkUponMutatingRequest,
        params (RequestHandlerMetadata metadata, IMethodHandler handler)[] handlers)
    {
        var provider = new TestHandlerProvider(handlers);

        var executionQueue = new TestRequestExecutionQueue(new MockServer(), provider, cancelInProgressWorkUponMutatingRequest);
        executionQueue.Start();

        return executionQueue;
    }

    private static ILspServices GetLspServices()
        => TestLspServices.Create(
            services: [
                (typeof(AbstractRequestContextFactory<TestRequestContext>), (object)TestRequestContext.Factory.Instance),
                (typeof(ILspLogger), NoOpLspLogger.Instance)
            ],
            supportsMethodHandlerProvider: false);

    [Fact]
    public async Task ExecuteAsync_ThrowCompletes()
    {
        // Arrange
        var requestExecutionQueue = GetRequestExecutionQueue(false, (ThrowingHandler.Metadata, ThrowingHandler.Instance));
        var lspServices = GetLspServices();

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), ThrowingHandler.Name, lspServices, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelInProgressWork_CancelsInProgressWorkWhenMutatingRequestArrives()
    {
        // Let's try it a bunch of times to try to find timing issues.
        for (var i = 0; i < 20; i++)
        {
            // Arrange
            var requestExecutionQueue = GetRequestExecutionQueue(cancelInProgressWorkUponMutatingRequest: true, handlers:
            [
                (CancellingHandler.Metadata, CancellingHandler.Instance),
                (CompletingHandler.Metadata, CompletingHandler.Instance),
                (MutatingHandler.Metadata, MutatingHandler.Instance),
            ]);
            var lspServices = GetLspServices();

            var cancellingRequestCancellationToken = new CancellationToken();
            var completingRequestCancellationToken = new CancellationToken();

            var _ = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), CancellingHandler.Name, lspServices, cancellingRequestCancellationToken);
            var _1 = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), CompletingHandler.Name, lspServices, completingRequestCancellationToken);

            // Act & Assert
            // A Debug.Assert would throw if the tasks hadn't completed when the mutating request is called.
            await requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), MutatingHandler.Name, lspServices, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Dispose_MultipleTimes_Succeeds()
    {
        // Arrange
        var requestExecutionQueue = GetRequestExecutionQueue(false, (TestMethodHandler.Metadata, TestMethodHandler.Instance));

        // Act
        await requestExecutionQueue.DisposeAsync();
        await requestExecutionQueue.DisposeAsync();

        // Assert, it didn't fail
    }

    [Fact]
    public async Task ExecuteAsync_CompletesTask()
    {
        var requestExecutionQueue = GetRequestExecutionQueue(false, (TestMethodHandler.Metadata, TestMethodHandler.Instance));
        var lspServices = GetLspServices();

        var response = (MockResponse?)await requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), TestMethodHandler.Name, lspServices, CancellationToken.None);
        Assert.Equal("stuff", response?.Response);
    }

    [Fact]
    public async Task ExecuteAsync_CompletesTask_Parameterless()
    {
        var requestExecutionQueue = GetRequestExecutionQueue(false, (TestParameterlessMethodHandler.Metadata, TestParameterlessMethodHandler.Instance));
        var lspServices = GetLspServices();

        var response = (MockResponse?)await requestExecutionQueue.ExecuteAsync(serializedRequest: null, TestParameterlessMethodHandler.Name, lspServices, CancellationToken.None);
        Assert.Equal("true", response?.Response);
    }

    [Fact]
    public async Task ExecuteAsync_CompletesTask_Notification()
    {
        var requestExecutionQueue = GetRequestExecutionQueue(false, (TestNotificationHandler.Metadata, TestNotificationHandler.Instance));
        var lspServices = GetLspServices();

        var response = await requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), TestNotificationHandler.Name, lspServices, CancellationToken.None);
        Assert.Same(NoValue.Instance, response);
    }

    [Fact]
    public async Task ExecuteAsync_CompletesTask_Notification_Parameterless()
    {
        var requestExecutionQueue = GetRequestExecutionQueue(false, (TestParameterlessNotificationHandler.Metadata, TestParameterlessNotificationHandler.Instance));
        var lspServices = GetLspServices();

        var response = await requestExecutionQueue.ExecuteAsync(serializedRequest: null, TestParameterlessNotificationHandler.Name, lspServices, CancellationToken.None);
        Assert.Same(NoValue.Instance, response);
    }

    [Fact]
    public async Task Queue_DrainsOnShutdown()
    {
        var requestExecutionQueue = GetRequestExecutionQueue(false, (TestMethodHandler.Metadata, TestMethodHandler.Instance));
        var request = JsonSerializer.SerializeToElement(new MockRequest(1));
        var lspServices = GetLspServices();

        var task1 = requestExecutionQueue.ExecuteAsync(request, TestMethodHandler.Name, lspServices, CancellationToken.None);
        var task2 = requestExecutionQueue.ExecuteAsync(request, TestMethodHandler.Name, lspServices, CancellationToken.None);

        await requestExecutionQueue.DisposeAsync();

        Assert.True(task1.IsCompleted);
        Assert.True(task2.IsCompleted);
    }

    [Fact]
    public async Task Queue_CompletionDoesNotShutdownServer()
    {
        var requestExecutionQueue = GetRequestExecutionQueue(false, (TestMethodHandler.Metadata, TestMethodHandler.Instance));
        var lspServices = GetLspServices();

        await requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), TestMethodHandler.Name, lspServices, CancellationToken.None);

        requestExecutionQueue.Complete();
        await requestExecutionQueue.GetTestAccessor().WaitForProcessingToStopAsync();

        Assert.False(requestExecutionQueue.LanguageServer.GetTestAccessor().HasShutdownStarted());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestOperationScope_CoversQueueingAndHandlerCompletion(bool failRequest)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var handler = new GatedRequestHandler();
        var scope = new TestRequestOperationScope();
        await using var queue = new TestRequestExecutionQueue(
            new MockServer(), new TestHandlerProvider([(TestMethodHandler.Metadata, handler)]), cancelInProgressWorkUponMutatingRequest: false)
        {
            RequestOperationScopeFactory = _ => scope.Begin()
        };

        var request = queue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), TestMethodHandler.Name, GetLspServices(), CancellationToken.None);
        Assert.True(scope.HasPendingWork);
        Assert.False(handler.Started.Task.IsCompleted);

        queue.Start();
        await handler.Started.Task.WithCancellation(timeout.Token);
        Assert.True(scope.HasPendingWork);

        if (failRequest)
        {
            handler.Response.SetException(new NotImplementedException());
            await Assert.ThrowsAsync<NotImplementedException>(() => request);
        }
        else
        {
            handler.Response.SetResult(new MockResponse("finished"));
            Assert.Equal("finished", ((MockResponse?)await request)?.Response);
        }

        await scope.Completed.Task.WithCancellation(timeout.Token);
        Assert.False(scope.HasPendingWork);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestOperationScope_ClientCancellationWaitsForActualHandlerCompletion(bool mutatesSolutionState)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var handler = new GatedRequestHandler(mutatesSolutionState);
        var scope = new TestRequestOperationScope();
        await using var queue = new TestRequestExecutionQueue(
            new MockServer(), new TestHandlerProvider([(TestMethodHandler.Metadata, handler)]), cancelInProgressWorkUponMutatingRequest: false)
        {
            RequestOperationScopeFactory = _ => scope.Begin()
        };
        using var cancellationSource = new CancellationTokenSource();

        queue.Start();
        var request = queue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), TestMethodHandler.Name, GetLspServices(), cancellationSource.Token);
        await handler.Started.Task.WithCancellation(timeout.Token);

        try
        {
            cancellationSource.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
            Assert.True(scope.HasPendingWork);
        }
        finally
        {
            handler.Response.SetResult(new MockResponse("finished"));
        }

        await scope.Completed.Task.WithCancellation(timeout.Token);
        Assert.False(scope.HasPendingWork);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestOperationScope_QueuedCancellationReleasesWithoutRunningHandler(bool shutDownQueue)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var handler = new GatedRequestHandler();
        var scope = new TestRequestOperationScope();
        await using var queue = new TestRequestExecutionQueue(
            new MockServer(), new TestHandlerProvider([(TestMethodHandler.Metadata, handler)]), cancelInProgressWorkUponMutatingRequest: false)
        {
            RequestOperationScopeFactory = _ => scope.Begin()
        };
        using var cancellationSource = new CancellationTokenSource();

        var request = queue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), TestMethodHandler.Name, GetLspServices(), cancellationSource.Token);
        Assert.True(scope.HasPendingWork);

        if (shutDownQueue)
            await queue.DisposeAsync();
        else
            cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await scope.Completed.Task.WithCancellation(timeout.Token);
        Assert.False(scope.HasPendingWork);
        Assert.False(handler.Started.Task.IsCompleted);
    }

    [Fact]
    public async Task RequestOperationScope_MissingHandlerReleasesScope()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var scope = new TestRequestOperationScope();
        await using var queue = GetRequestExecutionQueue(false, (TestMethodHandler.Metadata, TestMethodHandler.Instance));
        queue.RequestOperationScopeFactory = _ => scope.Begin();

        var request = queue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), "missing", GetLspServices(), CancellationToken.None);
        await Assert.ThrowsAsync<LocalRpcException>(() => request);
        await scope.Completed.Task.WithCancellation(timeout.Token);
        Assert.False(scope.HasPendingWork);
    }

    private sealed class GatedRequestHandler(bool mutatesSolutionState = false) : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
    {
        public TaskCompletionSource<object?> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<MockResponse> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool MutatesSolutionState => mutatesSolutionState;

        public Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
        {
            Started.SetResult(null);
            // Keep executing until released, even when the client has cancelled its response.
            return Response.Task;
        }
    }

    private sealed class TestRequestOperationScope : IDisposable
    {
        private int _active;

        public TaskCompletionSource<object?> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HasPendingWork => Volatile.Read(ref _active) != 0;

        public IDisposable Begin()
        {
            Assert.Equal(0, Interlocked.Exchange(ref _active, 1));
            return this;
        }

        public void Dispose()
        {
            Assert.Equal(1, Interlocked.Exchange(ref _active, 0));
            Completed.SetResult(null);
        }
    }

    private sealed class TestRequestExecutionQueue : RequestExecutionQueue<TestRequestContext>
    {
        private readonly bool _cancelInProgressWorkUponMutatingRequest;

        public TestRequestExecutionQueue(MockServer languageServer, AbstractHandlerProvider handlerProvider, bool cancelInProgressWorkUponMutatingRequest)
            : base(languageServer, handlerProvider)
        {
            _cancelInProgressWorkUponMutatingRequest = cancelInProgressWorkUponMutatingRequest;
            LanguageServer = languageServer;
        }

        public MockServer LanguageServer { get; }
        public Func<string, IDisposable?>? RequestOperationScopeFactory { get; set; }

        protected override bool CancelInProgressWorkUponMutatingRequest => _cancelInProgressWorkUponMutatingRequest;

        protected override IDisposable? CreateRequestOperationScope(string methodName)
            => RequestOperationScopeFactory?.Invoke(methodName);

        public void Complete()
            => _queue.Complete();
    }
}
