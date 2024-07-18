// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// Implements the client side of the RPC channel used to communicate with the build host, which is using RpcServer.
/// </summary>
/// <remarks>
/// The RPC system implemented here is pretty close to something like JSON-RPC; however since we need the Build Host to be usable in Source Build
/// scenarios, we are limited to using only what is either in .NET or can be easily made buildable in Source Build. Thus existing solutions like StreamJsonRpc 
/// are out. If at some point there is a standard RPC mechanism exposed in .NET or Source Build, we should delete this and use that instead.
/// </remarks>
internal sealed class RpcClient
{
    private readonly TextWriter _sendingStream;
    private readonly SemaphoreSlim _sendingStreamSemaphore = new SemaphoreSlim(initialCount: 1);
    private readonly TextReader _receivingStream;

    private readonly ConcurrentDictionary<int, (TaskCompletionSource<object?>, System.Type? expectedReturnType)> _outstandingRequests = [];
    private volatile int _nextRequestId = 0;

    private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

    public RpcClient(Stream sendingStream, Stream receivingStream)
    {
        _sendingStream = new StreamWriter(sendingStream, JsonSettings.StreamEncoding);
        _receivingStream = new StreamReader(receivingStream, JsonSettings.StreamEncoding);
    }

    public event EventHandler? Disconnected;

    public void Start()
    {
        // We'll start this and let it run until Shutdown has been called.
        Task.Run(async () =>
        {
            string? line;
            while ((line = await _receivingStream.TryReadLineOrReturnNullIfCancelledAsync(_shutdownTokenSource.Token).ConfigureAwait(false)) != null)
            {
                var response = JsonConvert.DeserializeObject<Response>(line);

                Contract.ThrowIfNull(response);

                Contract.ThrowIfFalse(_outstandingRequests.TryRemove(response.Id, out var completionSourceAndExpectedType), $"We got a response for request ID {response.Id} but that was already completed.");
                var (completionSource, expectedType) = completionSourceAndExpectedType;

                if (response.Exception != null)
                {
                    completionSource.SetException(new RemoteInvocationException(response.Exception));
                }
                else
                {
                    // If this is void-returning, then just set null
                    if (expectedType == null)
                    {
                        completionSource.SetResult(null);
                    }
                    else
                    {
                        try
                        {
                            // response.Value might be null if the response was in fact null.
                            var result = response.Value?.ToObject(expectedType);
                            completionSource.SetResult(result);
                        }
                        catch (Exception ex)
                        {
                            completionSource.SetException(new Exception("Unable to deserialize the result", ex));
                        }
                    }
                }
            }

            // We've disconnected, so cancel any remaining outstanding tasks
            foreach (var (request, _) in _outstandingRequests.Values)
                request.SetException(new System.Exception("The server disconnected unexpectedly."));

            Disconnected?.Invoke(this, EventArgs.Empty);
        });
    }

    public void Shutdown()
    {
        _shutdownTokenSource.Cancel();
    }

    public Task InvokeAsync(int targetObject, string methodName, List<object?> parameters, CancellationToken cancellationToken)
    {
        // We'll ignore the return value of InvokeCoreAsync in this case, since it'll just be filled in with null
        return InvokeCoreAsync(targetObject, methodName, parameters, expectedReturnType: null, cancellationToken);
    }

    public async Task<T?> InvokeNullableAsync<T>(int targetObject, string methodName, List<object?> parameters, CancellationToken cancellationToken) where T : class
    {
        var result = await InvokeCoreAsync(targetObject, methodName, parameters, expectedReturnType: typeof(T), cancellationToken).ConfigureAwait(false);
        return (T?)result;
    }

    public async Task<T> InvokeAsync<T>(int targetObject, string methodName, List<object?> parameters, CancellationToken cancellationToken) where T : notnull
    {
        var result = await InvokeCoreAsync(targetObject, methodName, parameters, expectedReturnType: typeof(T), cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(result, "We expected a non-null result but got null back.");
        return (T)result;
    }

    private async Task<object?> InvokeCoreAsync(int targetObject, string methodName, List<object?> parameters, Type? expectedReturnType, CancellationToken cancellationToken)
    {
        int requestId;
        var requestCompletionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Loop until we successfully have a new index for this; practically we don't expect this to ever collide, since that'd mean we'd have
        // billions of requests outstanding, but...
        do
        {
            requestId = Interlocked.Increment(ref _nextRequestId);
        }
        while (!_outstandingRequests.TryAdd(requestId, (requestCompletionSource, expectedReturnType)));

        var request = new Request
        {
            Id = requestId,
            TargetObject = targetObject,
            Method = methodName,
            Parameters = parameters.SelectAsArray(static p => p is not null ? JToken.FromObject(p) : JValue.CreateNull())
        };

        var requestJson = JsonConvert.SerializeObject(request, JsonSettings.SingleLineSerializerSettings);

        try
        {
            // The only cancellation we support is cancelling before we are able to write the request to the stream; once it's been written
            // the other side will execute it to completion. Thus cancellationToken is checked here, but nowhere else.
            using (await _sendingStreamSemaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await _sendingStream.WriteLineAsync(requestJson).ConfigureAwait(false);
#if NET8_0_OR_GREATER
                await _sendingStream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
#else
                await _sendingStream.FlushAsync().ConfigureAwait(false);
#endif
            }
        }
        catch (OperationCanceledException)
        {
            // The request was cancelled, so we don't need to hold it around anymore.
            _outstandingRequests.TryRemove(requestId, out _);
            throw;
        }

        return await requestCompletionSource.Task.ConfigureAwait(false);
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(RpcClient client)
    {
        public int GetOutstandingRequestCount()
            => client._outstandingRequests.Count;
    }
}
