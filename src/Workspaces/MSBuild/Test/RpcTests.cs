// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspaces;

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

using NamedPipeUtil = MSBuildWorkspaces::Microsoft.CodeAnalysis.NamedPipeUtil;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

/// <summary>
/// All the tests of the custom RPC system we have for talking to the build host.
/// </summary>
public sealed class RpcTests
{
    private sealed class RpcPair : IAsyncDisposable
    {
        private readonly PipeStream _clientStream;
        private readonly PipeStream _serverStream;

        public RpcPair()
        {
            // Create a pipe server and connected client; we use pipes here to match the same way we communicate with the build host in the
            // product since pipes can act a bit different than other types of streams, and that can lead to subtle bugs.
            var pipeName = Guid.NewGuid().ToString();

            var pipeServer = NamedPipeUtil.CreateServer(pipeName, PipeDirection.InOut);
            var pipeServerConnectionTask = pipeServer.WaitForConnectionAsync();
            var pipeClient = NamedPipeUtil.CreateClient(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            pipeClient.Connect();
            pipeServerConnectionTask.Wait();

            _clientStream = pipeClient;
            _serverStream = pipeServer;

            Server = new RpcServer(_serverStream);
            Client = new RpcClient(_clientStream);

            // Start the server, and shut the pipe down once it's done to simulate real behavior
            ServerCompletion = Server.RunAsync().ContinueWith(_ => _serverStream.Dispose(), TaskScheduler.Default);
            Client.Start();
        }

        public RpcServer Server { get; }
        public RpcClient Client { get; }
        public Task ServerCompletion { get; }

        public async ValueTask DisposeAsync()
        {
            _clientStream.Dispose();
            _serverStream.Dispose();

            await ServerCompletion;
        }
    }

    [Theory]
    [InlineData("World")]
    [InlineData("\0")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("🌐")]
    public async Task StringTakingAndReturningMethod(string s)
    {
        await using var rpcPair = new RpcPair();

        rpcPair.Server.AddTarget(new ObjectWithHelloMethod());
        var result = await rpcPair.Client.InvokeAsync<string>(targetObject: 0, nameof(ObjectWithHelloMethod.Hello), [s], CancellationToken.None);

        Assert.Equal("Hello " + s, result);
    }

    [Fact]
    public async Task IntegerTakingAndReturningMethod()
    {
        await using var rpcPair = new RpcPair();

        rpcPair.Server.AddTarget(new ObjectWithAddMethod());
        var result = await rpcPair.Client.InvokeAsync<int>(targetObject: 0, nameof(ObjectWithAddMethod.Add), [1, 1], CancellationToken.None);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task StringTakingAndReturningMethodWithNull()
    {
        await using var rpcPair = new RpcPair();

        rpcPair.Server.AddTarget(new ObjectWithNullableHelloMethod());

        // Test the InvokeNullableAsync with non-nulls
        var result = await rpcPair.Client.InvokeNullableAsync<string>(targetObject: 0, nameof(ObjectWithNullableHelloMethod.TryHello), ["World"], CancellationToken.None);
        Assert.Equal("Hello World", result);

        // And with nulls
        result = await rpcPair.Client.InvokeNullableAsync<string>(targetObject: 0, nameof(ObjectWithNullableHelloMethod.TryHello), [null], CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task VoidReturningMethod()
    {
        await using var rpcPair = new RpcPair();

        var rpcTarget = new ObjectWithVoidMethod();
        rpcPair.Server.AddTarget(rpcTarget);

        await rpcPair.Client.InvokeAsync(targetObject: 0, nameof(ObjectWithVoidMethod.SetMessage), ["Hello, World!"], CancellationToken.None);
        Assert.Equal("Hello, World!", rpcTarget.Message);
    }

    [Fact]
    public async Task AsyncMethods()
    {
        await using var rpcPair = new RpcPair();

        var rpcTarget = new ObjectWithAsyncHelloMethods();
        rpcPair.Server.AddTarget(rpcTarget);

        var result = await rpcPair.Client.InvokeAsync<string>(targetObject: 0, nameof(ObjectWithAsyncHelloMethods.HelloAsync), ["World"], CancellationToken.None);
        Assert.Equal("Hello World", result);

        result = await rpcPair.Client.InvokeAsync<string>(targetObject: 0, nameof(ObjectWithAsyncHelloMethods.HelloWithCancellationAsync), ["World"], CancellationToken.None);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task InterleavedCallsCompleteFirstFirst()
    {
        await using var rpcPair = new RpcPair();

        var rpcTarget = new ObjectWithRealAsyncMethod();
        rpcPair.Server.AddTarget(rpcTarget);

        var call1 = rpcPair.Client.InvokeAsync(targetObject: 0, nameof(ObjectWithRealAsyncMethod.WaitAsync), [], CancellationToken.None);
        Assert.False(call1.IsCompleted);

        // Ensure the RPC target has gotten that request before going further, otherwise the call for the second Invoke could end up to the RPC target first
        // and our test is awaiting the wrong tasks.
        rpcTarget.WaitUntilRequest(index: 0);

        var call2 = rpcPair.Client.InvokeAsync(targetObject: 0, nameof(ObjectWithRealAsyncMethod.WaitAsync), [], CancellationToken.None);
        Assert.False(call2.IsCompleted);

        rpcTarget.Complete(index: 0);

        await call1;
        Assert.True(call1.IsCompleted);
        Assert.False(call2.IsCompleted);

        rpcTarget.Complete(index: 1);

        await call2;
        Assert.True(call2.IsCompleted);
    }

    [Fact]
    public async Task InterleavedCallsCompleteSecondFirst()
    {
        await using var rpcPair = new RpcPair();

        var rpcTarget = new ObjectWithRealAsyncMethod();
        rpcPair.Server.AddTarget(rpcTarget);

        var call1 = rpcPair.Client.InvokeAsync(targetObject: 0, nameof(ObjectWithRealAsyncMethod.WaitAsync), [], CancellationToken.None);
        Assert.False(call1.IsCompleted);

        // Ensure the RPC target has gotten that request before going further, otherwise the call for the second Invoke could end up to the RPC target first
        // and our test is awaiting the wrong targets.
        rpcTarget.WaitUntilRequest(index: 0);

        var call2 = rpcPair.Client.InvokeAsync(targetObject: 0, nameof(ObjectWithRealAsyncMethod.WaitAsync), [], CancellationToken.None);
        Assert.False(call2.IsCompleted);

        rpcTarget.Complete(index: 1);

        await call2;
        Assert.True(call2.IsCompleted);
        Assert.False(call1.IsCompleted);

        rpcTarget.Complete(index: 0);

        await call1;
        Assert.True(call1.IsCompleted);
    }

    [Fact]
    public async Task ExceptionHandling()
    {
        await using var rpcPair = new RpcPair();

        rpcPair.Server.AddTarget(new ObjectWithThrowingMethod());

        var exception = await Assert.ThrowsAsync<RemoteInvocationException>(() => rpcPair.Client.InvokeAsync(targetObject: 0, nameof(ObjectWithThrowingMethod.ThrowException), [], CancellationToken.None));

        Assert.Contains("Exception thrown by test method!", exception.Message);
    }

    [Fact]
    public async Task CancelledTaskDoesNotLeakRequest()
    {
        await using var rpcPair = new RpcPair();

        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        rpcPair.Server.AddTarget(new ObjectWithHelloMethod());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await rpcPair.Client.InvokeAsync<string>(targetObject: 0, nameof(ObjectWithHelloMethod.Hello), [], tokenSource.Token));

        Assert.Equal(0, rpcPair.Client.GetTestAccessor().GetOutstandingRequestCount());
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/77040")]
    public async Task RequestThatClosesServerDoesNotThrow()
    {
        await using var rpcPair = new RpcPair();
        rpcPair.Server.AddTarget(new ObjectWithMethodThatShutsDownServer(rpcPair.Server));

        // Invoke the method, and ensure we don't get exceptions back due to the shutdown if the pipe closed too early.
        // This is not guaranteed to catch any bug, since any bug is fundamentally a race, but it at least ensures we aren't always broken.
        await rpcPair.Client.InvokeAsync(targetObject: 0, nameof(ObjectWithMethodThatShutsDownServer.Shutdown), [], CancellationToken.None);
    }

#pragma warning disable CA1822 // Mark members as static

    private sealed class ObjectWithHelloMethod { public string Hello(string name) { return "Hello " + name; } }
    private sealed class ObjectWithAddMethod { public int Add(int a, int b) { return a + b; } }
    private sealed class ObjectWithNullableHelloMethod { public string? TryHello(string? name) { return name is not null ? "Hello " + name : null; } }
    private sealed class ObjectWithVoidMethod { public string? Message; public void SetMessage(string message) { Message = message; } }
    private sealed class ObjectWithAsyncHelloMethods
    {
        public async Task<string> HelloAsync(string name) { return "Hello " + name; }
        public async Task<string> HelloWithCancellationAsync(string name, CancellationToken cancellationToken)
        {
            // We never expect to be given a cancellable cancellation token over RPC
            Assert.False(cancellationToken.CanBeCanceled);
            return "Hello " + name;
        }
    }

    private sealed class ObjectWithRealAsyncMethod
    {
        private readonly List<TaskCompletionSource<object?>> _completionSources = [];

        public Task WaitAsync()
        {
            var tcs = new TaskCompletionSource<object?>();
            lock (_completionSources)
            {
                _completionSources.Add(tcs);
                Monitor.PulseAll(_completionSources);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Completes the task for the corresponding Wait() call; blocks until that task actually exists
        /// </summary>
        public void WaitUntilRequest(int index)
        {
            // We'll wait until that index has become available
            lock (_completionSources)
            {
                while (_completionSources.Count <= index)
                    Monitor.Wait(_completionSources);
            }
        }

        /// <summary>
        /// Completes the task for the corresponding Wait() call; blocks until that task actually exists
        /// </summary>
        public void Complete(int index)
        {
            WaitUntilRequest(index);
            _completionSources[index].SetResult(null);
        }
    }

    private sealed class ObjectWithThrowingMethod { public void ThrowException() { throw new Exception("Exception thrown by test method!"); } }
    private sealed class ObjectWithMethodThatShutsDownServer(RpcServer server) { public void Shutdown() { server.Shutdown(); } }

#pragma warning restore CA1822 // Mark members as static
}
