// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Moq;
using Nerdbank.Streams;
using StreamJsonRpc;
using Xunit;
using static Microsoft.CommonLanguageServerProtocol.Framework.UnitTests.HandlerProviderTests;
using static Microsoft.CommonLanguageServerProtocol.Framework.UnitTests.RequestExecutionQueueTests;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public class RequestExecutionQueueTests
{
    private class MockServer : AbstractLanguageServer<TestRequestContext>
    {
        public MockServer() : base(new JsonRpc(new HeaderDelimitedMessageHandler(FullDuplexStream.CreatePair().Item1)), NoOpLspLogger.Instance)
        {
        }

        protected override ILspServices ConstructLspServices()
        {
            throw new NotImplementedException();
        }
    }

    private const string MethodName = "SomeMethod";
    private const string CancellingMethod = "CancellingMethod";
    private const string CompletingMethod = "CompletingMethod";
    private const string MutatingMethod = "MutatingMethod";

    private static RequestExecutionQueue<TestRequestContext> GetRequestExecutionQueue(bool cancelInProgressWorkUponMutatingRequest, params IMethodHandler[] methodHandlers)
    {
        var handlerProvider = new Mock<IHandlerProvider>(MockBehavior.Strict);
        if (methodHandlers.Length == 0)
        {
            var handler = GetTestMethodHandler();
            handlerProvider.Setup(h => h.GetMethodHandler(MethodName, TestMethodHandler.RequestType, TestMethodHandler.ResponseType)).Returns(handler);
        }
        foreach (var methodHandler in methodHandlers)
        {
            var methodType = methodHandler.GetType();
            var methodAttribute = methodType.GetCustomAttribute<LanguageServerEndpointAttribute>();
            var method = methodAttribute.Method;

            handlerProvider.Setup(h => h.GetMethodHandler(method, typeof(int), typeof(string))).Returns(methodHandler);
        }
        var executionQueue = new TestRequestExecutionQueue(new MockServer(), NoOpLspLogger.Instance, handlerProvider.Object, cancelInProgressWorkUponMutatingRequest);
        executionQueue.Start();

        return executionQueue;
    }

    private static ILspServices GetLspServices()
    {
        var requestContextFactory = new Mock<IRequestContextFactory<TestRequestContext>>(MockBehavior.Strict);
        requestContextFactory.Setup(f => f.CreateRequestContextAsync<int>(It.IsAny<IQueueItem<TestRequestContext>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new TestRequestContext()));
        var services = new List<(Type, object)> { (typeof(IRequestContextFactory<TestRequestContext>), requestContextFactory.Object) };
        var lspServices = new TestLspServices(services, supportsGetRegisteredServices: false);

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
        // Arrange
        var throwingHandler = new ThrowingHandler();
        var requestExecutionQueue = GetRequestExecutionQueue(false, throwingHandler);
        var lspServices = GetLspServices();

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => requestExecutionQueue.ExecuteAsync<int, string>(1, MethodName, lspServices, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelInProgressWork_CancelsInProgressWorkWhenMutatingRequestArrives()
    {
        // Arrange
        var mutatingHandler = new MutatingHandler();
        var cancellingHandler = new CancellingHandler(mutatingHandler);
        var completingHandler = new CompletingHandler(mutatingHandler);
        var requestExecutionQueue = GetRequestExecutionQueue(cancelInProgressWorkUponMutatingRequest: true, methodHandlers: new IMethodHandler[] { cancellingHandler, completingHandler, mutatingHandler });
        var lspServices = GetLspServices();

        var cancellingRequestCancellationToken = new CancellationToken();
        var completingRequestCancellationToken = new CancellationToken();

        var cancellingRequestTask = requestExecutionQueue.ExecuteAsync<int, string>(1, CancellingMethod, lspServices, cancellingRequestCancellationToken);
        var completingRequestTask = requestExecutionQueue.ExecuteAsync<int, string>(1, CompletingMethod, lspServices, completingRequestCancellationToken);
        mutatingHandler.Tasks.Add(cancellingRequestTask);
        mutatingHandler.Tasks.Add(completingRequestTask);

        // Act
        await requestExecutionQueue.ExecuteAsync<int, string>(1, MutatingMethod, lspServices, CancellationToken.None);

        // Assert
        Assert.True(cancellingRequestTask.IsCanceled, "Should have been cancelled");
        Assert.True(completingRequestTask.IsCompleted, "Should have been completed");
    }

    [Fact]
    public async Task Dispose_MultipleTimes_Succeeds()
    {
        // Arrange
        var requestExecutionQueue = GetRequestExecutionQueue(false);

        // Act
        await requestExecutionQueue.DisposeAsync();
        await requestExecutionQueue.DisposeAsync();

        // Assert, it didn't fail
    }

    [Fact]
    public async Task ExecuteAsync_CompletesTask()
    {
        var requestExecutionQueue = GetRequestExecutionQueue(false);
        var request = 1;
        var lspServices = GetLspServices();

        var response = await requestExecutionQueue.ExecuteAsync<int, string>(request, MethodName, lspServices, CancellationToken.None);

        Assert.Equal("stuff", response);
    }

    [Fact]
    public async Task Queue_DrainsOnShutdown()
    {
        var requestExecutionQueue = GetRequestExecutionQueue(false);
        var request = 1;
        var lspServices = GetLspServices();

        var task1 = requestExecutionQueue.ExecuteAsync<int, string>(request, MethodName, lspServices, CancellationToken.None);
        var task2 = requestExecutionQueue.ExecuteAsync<int, string>(request, MethodName, lspServices, CancellationToken.None);

        await requestExecutionQueue.DisposeAsync();

        Assert.True(task1.IsCompleted);
        Assert.True(task2.IsCompleted);
    }

    private class TestRequestExecutionQueue : RequestExecutionQueue<TestRequestContext>
    {
        private readonly bool _cancelInProgressWorkUponMutatingRequest;

        public TestRequestExecutionQueue(AbstractLanguageServer<TestRequestContext> languageServer, ILspLogger logger, IHandlerProvider handlerProvider, bool cancelInProgressWorkUponMutatingRequest)
            : base(languageServer, logger, handlerProvider)
        {
            _cancelInProgressWorkUponMutatingRequest = cancelInProgressWorkUponMutatingRequest;
        }

        protected override bool CancelInProgressWorkUponMutatingRequest => _cancelInProgressWorkUponMutatingRequest;
    }

    [LanguageServerEndpoint(MutatingMethod)]
    public class MutatingHandler : IRequestHandler<int, string, TestRequestContext>
    {
        public MutatingHandler()
        {
        }

        public List<Task> Tasks = new List<Task>();

        public bool MutatesSolutionState => true;

        public Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
        {
            if (!Tasks.All(t => t.IsCanceled || t.IsCompleted || t.IsFaulted))
            {
                throw new InvalidOperationException("Other requests must complete before this one begins");
            }

            return Task.FromResult(string.Empty);
        }
    }

    [LanguageServerEndpoint(CompletingMethod)]
    public class CompletingHandler : IRequestHandler<int, string, TestRequestContext>
    {
        private readonly MutatingHandler _mutatingHandler;

        public CompletingHandler(MutatingHandler mutatingHandler) : base()
        {
            _mutatingHandler = mutatingHandler;
        }

        public bool MutatesSolutionState => false;

        public async Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return "I completed!";
                }
                await Task.Delay(10);
            }
        }
    }

    [LanguageServerEndpoint(CancellingMethod)]
    public class CancellingHandler : IRequestHandler<int, string, TestRequestContext>
    {
        private readonly MutatingHandler _mutatingHandler;

        public CancellingHandler(MutatingHandler mutatingHandler) : base()
        {
            _mutatingHandler = mutatingHandler;
        }

        public bool MutatesSolutionState => false;

        public async Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10);
            }
        }
    }

    [LanguageServerEndpoint(MethodName)]
    public class ThrowingHandler : IRequestHandler<int, string, TestRequestContext>
    {
        public bool MutatesSolutionState => false;

        public Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
