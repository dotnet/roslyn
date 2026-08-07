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

    private static RequestExecutionQueue<TestRequestContext> GetRequestExecutionQueue(
        bool cancelInProgressWorkUponMutatingRequest,
        params (RequestHandlerMetadata metadata, IMethodHandler handler)[] handlers)
    {
        var provider = new TestHandlerProvider(handlers);

        var executionQueue = new TestRequestExecutionQueue(new MockServer(), provider, cancelInProgressWorkUponMutatingRequest);
        executionQueue.Start();

        return executionQueue;
    }

    private static ILspServices GetLspServices(AbstractRequestContextFactory<TestRequestContext>? requestContextFactory = null)
        => TestLspServices.Create(
            services: [
                (typeof(AbstractRequestContextFactory<TestRequestContext>), requestContextFactory ?? TestRequestContext.Factory.Instance),
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
    public async Task DeferredPreparation_DoesNotBlockLaterNonMutatingRequest()
    {
        var preparationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowPreparation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var laterRequestHandled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new CallbackRequestContextFactory((request, cancellationToken) =>
            Task.FromResult(request.Param == 1
                ? new RequestContextInfo<TestRequestContext>(new(), PrepareContextAsync)
                : new RequestContextInfo<TestRequestContext>(new())));
        var handler = new CallbackHandler(mutatesSolutionState: false, request =>
        {
            if (request.Param == 2)
                laterRequestHandled.TrySetResult(true);
        });
        var metadata = CreateMetadata("DeferredPreparation_DoesNotBlockLaterNonMutatingRequest");
        var requestExecutionQueue = GetRequestExecutionQueue(false, (metadata, handler));
        var lspServices = GetLspServices(factory);

        var firstRequest = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), metadata.MethodName, lspServices, CancellationToken.None);
        await preparationStarted.Task;

        var laterRequest = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(2)), metadata.MethodName, lspServices, CancellationToken.None);
        await laterRequestHandled.Task;
        Assert.False(firstRequest.IsCompleted);

        allowPreparation.SetResult(true);
        await Task.WhenAll(firstRequest, laterRequest);

        async Task<TestRequestContext> PrepareContextAsync(CancellationToken cancellationToken)
        {
            preparationStarted.SetResult(true);
            await allowPreparation.Task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return new TestRequestContext();
        }
    }

    [Fact]
    public async Task DeferredPreparation_DoesNotBlockLaterMutatingRequest()
    {
        var preparationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowPreparation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mutatingRequestHandled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new CallbackRequestContextFactory((request, cancellationToken) =>
            Task.FromResult(request.Param == 1
                ? new RequestContextInfo<TestRequestContext>(new(), PrepareContextAsync)
                : new RequestContextInfo<TestRequestContext>(new())));
        var nonMutatingHandler = new CallbackHandler(mutatesSolutionState: false);
        var mutatingHandler = new CallbackHandler(mutatesSolutionState: true, _ => mutatingRequestHandled.SetResult(true));
        var nonMutatingMetadata = CreateMetadata("DeferredPreparation_NonMutatingRequest");
        var mutatingMetadata = CreateMetadata("DeferredPreparation_MutatingRequest");
        var requestExecutionQueue = GetRequestExecutionQueue(false, (nonMutatingMetadata, nonMutatingHandler), (mutatingMetadata, mutatingHandler));
        var lspServices = GetLspServices(factory);

        var firstRequest = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), nonMutatingMetadata.MethodName, lspServices, CancellationToken.None);
        await preparationStarted.Task;

        var mutatingRequest = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(2)), mutatingMetadata.MethodName, lspServices, CancellationToken.None);
        await mutatingRequestHandled.Task;
        Assert.False(firstRequest.IsCompleted);

        allowPreparation.SetResult(true);
        await Task.WhenAll(firstRequest, mutatingRequest);

        async Task<TestRequestContext> PrepareContextAsync(CancellationToken cancellationToken)
        {
            preparationStarted.SetResult(true);
            await allowPreparation.Task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return new TestRequestContext();
        }
    }

    [Fact]
    public async Task DeferredPreparation_CancellationPreventsHandlerDispatch()
    {
        var preparationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowPreparation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerCalled = false;
        var factory = new CallbackRequestContextFactory((request, cancellationToken) =>
            Task.FromResult(new RequestContextInfo<TestRequestContext>(new(), PrepareContextAsync)));
        var handler = new CallbackHandler(mutatesSolutionState: false, _ => handlerCalled = true);
        var metadata = CreateMetadata("DeferredPreparation_CancellationPreventsHandlerDispatch");
        var requestExecutionQueue = GetRequestExecutionQueue(false, (metadata, handler));
        var lspServices = GetLspServices(factory);
        using var cancellationSource = new CancellationTokenSource();

        var requestTask = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), metadata.MethodName, lspServices, cancellationSource.Token);
        await preparationStarted.Task;

        cancellationSource.Cancel();
        allowPreparation.SetResult(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requestTask);
        Assert.False(handlerCalled);

        async Task<TestRequestContext> PrepareContextAsync(CancellationToken cancellationToken)
        {
            preparationStarted.SetResult(true);
            await allowPreparation.Task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return new TestRequestContext();
        }
    }

    [Fact]
    public async Task ContextFactoryWithoutDeferredPreparation_PreservesSerializedOrdering()
    {
        var contextCreationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowContextCreation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var laterRequestHandled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new CallbackRequestContextFactory(async (request, cancellationToken) =>
        {
            if (request.Param == 1)
            {
                contextCreationStarted.SetResult(true);
                await allowContextCreation.Task.ConfigureAwait(false);
            }

            return new RequestContextInfo<TestRequestContext>(new());
        });
        var handler = new CallbackHandler(mutatesSolutionState: false, request =>
        {
            if (request.Param == 2)
                laterRequestHandled.SetResult(true);
        });
        var metadata = CreateMetadata("ContextFactoryWithoutDeferredPreparation_PreservesSerializedOrdering");
        var requestExecutionQueue = GetRequestExecutionQueue(false, (metadata, handler));
        var lspServices = GetLspServices(factory);

        var firstRequest = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), metadata.MethodName, lspServices, CancellationToken.None);
        await contextCreationStarted.Task;

        var laterRequest = requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(2)), metadata.MethodName, lspServices, CancellationToken.None);
        Assert.False(laterRequestHandled.Task.IsCompleted);

        allowContextCreation.SetResult(true);
        await Task.WhenAll(firstRequest, laterRequest);
        Assert.True(laterRequestHandled.Task.IsCompleted);
    }

    [Fact]
    public async Task DeferredPreparation_MutatingRequestIsRejected()
    {
        var handlerCalled = false;
        var factory = new CallbackRequestContextFactory((request, cancellationToken) =>
            Task.FromResult(new RequestContextInfo<TestRequestContext>(new(), _ => Task.FromResult(new TestRequestContext()))));
        var handler = new CallbackHandler(mutatesSolutionState: true, _ => handlerCalled = true);
        var metadata = CreateMetadata("DeferredPreparation_MutatingRequestIsRejected");
        var requestExecutionQueue = GetRequestExecutionQueue(false, (metadata, handler));
        var lspServices = GetLspServices(factory);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            requestExecutionQueue.ExecuteAsync(JsonSerializer.SerializeToElement(new MockRequest(1)), metadata.MethodName, lspServices, CancellationToken.None));
        Assert.False(handlerCalled);
    }

    private static RequestHandlerMetadata CreateMetadata(string methodName)
        => new(methodName, TypeRef.Of<MockRequest>(), TypeRef.Of<MockResponse>(), LanguageServerConstants.DefaultLanguageName);

    private sealed class CallbackRequestContextFactory(
        Func<MockRequest, CancellationToken, Task<RequestContextInfo<TestRequestContext>>> createContextAsync) : AbstractRequestContextFactory<TestRequestContext>
    {
        public override Task<RequestContextInfo<TestRequestContext>> CreateRequestContextAsync<TRequestParam>(QueueItem<TestRequestContext> queueItem, IMethodHandler methodHandler, TRequestParam requestParam, CancellationToken cancellationToken)
            => createContextAsync((MockRequest)(object)requestParam!, cancellationToken);
    }

    private sealed class CallbackHandler(bool mutatesSolutionState, Action<MockRequest>? callback = null) : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
    {
        public bool MutatesSolutionState => mutatesSolutionState;

        public Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
        {
            callback?.Invoke(request);
            return Task.FromResult(new MockResponse(request.Param.ToString()));
        }
    }

    private sealed class TestRequestExecutionQueue : RequestExecutionQueue<TestRequestContext>
    {
        private readonly bool _cancelInProgressWorkUponMutatingRequest;

        public TestRequestExecutionQueue(AbstractLanguageServer<TestRequestContext> languageServer, AbstractHandlerProvider handlerProvider, bool cancelInProgressWorkUponMutatingRequest)
            : base(languageServer, handlerProvider)
        {
            _cancelInProgressWorkUponMutatingRequest = cancelInProgressWorkUponMutatingRequest;
        }

        protected override bool CancelInProgressWorkUponMutatingRequest => _cancelInProgressWorkUponMutatingRequest;
    }
}
