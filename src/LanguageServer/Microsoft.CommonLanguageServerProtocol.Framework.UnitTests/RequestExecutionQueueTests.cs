// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Nerdbank.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using Xunit;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public sealed class RequestExecutionQueueTests
{
    private sealed class MockServer : NewtonsoftLanguageServer<TestRequestContext>
    {
        public MockServer() : base(new JsonRpc(new HeaderDelimitedMessageHandler(FullDuplexStream.CreatePair().Item1)), JsonSerializer.CreateDefault(), NoOpLspLogger.Instance)
        {
        }

        protected override ILspServices ConstructLspServices()
        {
            throw new NotImplementedException();
        }
    }

    private static RequestExecutionQueue<TestRequestContext> GetRequestExecutionQueue(
        bool cancelInProgressWorkUponMutatingRequest,
        params (RequestHandlerMetadata metadata, IMethodHandler handler)[] handlers)
    {
        var provider = new TestHandlerProvider(handlers);

        var executionQueue = new TestRequestExecutionQueue(new MockServer(), NoOpLspLogger.Instance, provider, cancelInProgressWorkUponMutatingRequest);
        executionQueue.Start();

        return executionQueue;
    }

    private static ILspServices GetLspServices()
        => TestLspServices.Create(
            services: new[] { (typeof(AbstractRequestContextFactory<TestRequestContext>), (object)TestRequestContext.Factory.Instance) },
            supportsMethodHandlerProvider: false);

    [Fact]
    public async Task ExecuteAsync_ThrowCompletes()
    {
        // Arrange
        var requestExecutionQueue = GetRequestExecutionQueue(false, (ThrowingHandler.Metadata, ThrowingHandler.Instance));
        var lspServices = GetLspServices();

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => requestExecutionQueue.ExecuteAsync(JToken.FromObject(new MockRequest(1)), ThrowingHandler.Name, lspServices, CancellationToken.None));
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

            var _ = requestExecutionQueue.ExecuteAsync(JToken.FromObject(new MockRequest(1)), CancellingHandler.Name, lspServices, cancellingRequestCancellationToken);
            var _1 = requestExecutionQueue.ExecuteAsync(JToken.FromObject(new MockRequest(1)), CompletingHandler.Name, lspServices, completingRequestCancellationToken);

            // Act & Assert
            // A Debug.Assert would throw if the tasks hadn't completed when the mutating request is called.
            await requestExecutionQueue.ExecuteAsync(JToken.FromObject(new MockRequest(1)), MutatingHandler.Name, lspServices, CancellationToken.None);
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

        var response = (MockResponse?)await requestExecutionQueue.ExecuteAsync(JToken.FromObject(new MockRequest(1)), TestMethodHandler.Name, lspServices, CancellationToken.None);
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

        var response = await requestExecutionQueue.ExecuteAsync(JToken.FromObject(new MockRequest(1)), TestNotificationHandler.Name, lspServices, CancellationToken.None);
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
        var request = JToken.FromObject(new MockRequest(1));
        var lspServices = GetLspServices();

        var task1 = requestExecutionQueue.ExecuteAsync(request, TestMethodHandler.Name, lspServices, CancellationToken.None);
        var task2 = requestExecutionQueue.ExecuteAsync(request, TestMethodHandler.Name, lspServices, CancellationToken.None);

        await requestExecutionQueue.DisposeAsync();

        Assert.True(task1.IsCompleted);
        Assert.True(task2.IsCompleted);
    }

    private sealed class TestRequestExecutionQueue : RequestExecutionQueue<TestRequestContext>
    {
        private readonly bool _cancelInProgressWorkUponMutatingRequest;

        public TestRequestExecutionQueue(AbstractLanguageServer<TestRequestContext> languageServer, ILspLogger logger, AbstractHandlerProvider handlerProvider, bool cancelInProgressWorkUponMutatingRequest)
            : base(languageServer, logger, handlerProvider)
        {
            _cancelInProgressWorkUponMutatingRequest = cancelInProgressWorkUponMutatingRequest;
        }

        protected override bool CancelInProgressWorkUponMutatingRequest => _cancelInProgressWorkUponMutatingRequest;
    }
}
