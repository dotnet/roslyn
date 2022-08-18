// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using static CommonLanguageServerProtocol.Framework.UnitTests.HandlerProviderTests;

namespace CommonLanguageServerProtocol.Framework.UnitTests;

public class RequestExecutionQueueTests
{
    private const string MethodName = "SomeMethod";

    private static RequestExecutionQueue<TestRequestContext> GetRequestExecutionQueue(IMethodHandler? methodHandler = null)
    {
        var handlerProvider = new Mock<IHandlerProvider>(MockBehavior.Strict);
        var handler = methodHandler ?? GetTestMethodHandler();

        handlerProvider.Setup(h => h.GetMethodHandler(MethodName, TestMethodHandler.RequestType, TestMethodHandler.ResponseType)).Returns(handler);
        var executionQueue = new RequestExecutionQueue<TestRequestContext>(NoOpLspLogger.Instance, handlerProvider.Object);
        executionQueue.Start(GetLspServices());

        return executionQueue;
    }

    private static ILspServices GetLspServices()
    {
        var requestContextFactory = new Mock<IRequestContextFactory<TestRequestContext>>(MockBehavior.Strict);
        requestContextFactory.Setup(f => f.CreateRequestContextAsync(It.IsAny<IQueueItem<TestRequestContext>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new TestRequestContext()));
        var services = new List<(Type, object)> { (typeof(IRequestContextFactory<TestRequestContext>), requestContextFactory.Object) };
        var lspServices = new TestLspServices(services, supportsRequiredServices: true, supportsGetRegisteredServices: false);

        return lspServices;
    }

    private static TestMethodHandler GetTestMethodHandler()
    {
        var methodHandler = new TestMethodHandler();

        return methodHandler;
    }

    [Fact]
    public async Task ExecuteAsync_ThrowCompletes()
    {
        var throwingHandler = new ThrowingHandler();
        var requestExecutionQueue = GetRequestExecutionQueue(throwingHandler);
        var request = 1;
        var lspServices = GetLspServices();

        await Assert.ThrowsAsync<NotImplementedException>(() => requestExecutionQueue.ExecuteAsync<int, string>(request, MethodName, lspServices, CancellationToken.None));
    }

    public class ThrowingHandler : IRequestHandler<int, string, TestRequestContext>
    {
        public bool MutatesSolutionState => false;

        public object? GetTextDocumentIdentifier(int request)
        {
            return null;
        }

        public Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task ExecuteAsync_CompletesTask()
    {
        var requestExecutionQueue = GetRequestExecutionQueue();
        var request = 1;
        var lspServices = GetLspServices();

        var response = await requestExecutionQueue.ExecuteAsync<int, string>(request, MethodName, lspServices, CancellationToken.None);

        Assert.Equal("stuff", response);
    }

    [Fact]
    public async Task Queue_DrainsOnShutdown()
    {
        var requestExecutionQueue = GetRequestExecutionQueue();
        var request = 1;
        var lspServices = GetLspServices();

        var task1 = requestExecutionQueue.ExecuteAsync<int, string>(request, MethodName, lspServices, CancellationToken.None);
        var task2 = requestExecutionQueue.ExecuteAsync<int, string>(request, MethodName, lspServices, CancellationToken.None);

        await requestExecutionQueue.DisposeAsync();

        Assert.True(task1.IsCompleted);
        Assert.True(task2.IsCompleted);
    }

    private class TestResponse
    {
    }
}
