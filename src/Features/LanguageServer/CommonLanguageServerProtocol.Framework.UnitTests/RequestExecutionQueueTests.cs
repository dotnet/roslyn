
using System;
using System.Threading.Tasks;
using Xunit;

namespace CommonLanguageServerProtocol.Framework.UnitTests;

public class RequestExecutionQueueTests
{
    private RequestExecutionQueue<TestRequestContext> GetRequestExecutionQueue()
    {
        var executionQueue = new RequestExecutionQueue<TestRequestContext>(NoOpLspLogger.Instance);

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
