// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace CommonLanguageServerProtocol.Framework.UnitTests;

public class RequestExecutionQueueTests
{
    private RequestExecutionQueue<TestRequestContext> GetRequestExecutionQueue()
    {
        var handlerProvider = new Mock<IHandlerProvider>(MockBehavior.Strict).Object;
        var executionQueue = new RequestExecutionQueue<TestRequestContext>(NoOpLspLogger.Instance, handlerProvider);

        return executionQueue;
    }

    [Fact]
    public async Task ExecuteAsync_CompletesTask()
    {
        var requestExecutionQueue = GetRequestExecutionQueue();

        throw new NotImplementedException();
    }

    [Fact]
    public async Task Queue_DrainsOnShutdown()
    {
        throw new NotImplementedException();
    }

    private class TestRequestContext
    {
    }
}
