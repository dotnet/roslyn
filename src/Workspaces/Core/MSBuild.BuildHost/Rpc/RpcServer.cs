// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.Rpc;

/// <summary>
/// Implements the server side of the RPC channel used to communicate with the build host.
/// </summary>
/// <remarks>
/// The RPC system implemented here is pretty close to something like JSON-RPC; however since we need the Build Host to be usable in Source Build
/// scenarios, we are limited to using only what is either in .NET or can be easily made buildable in Source Build. Thus existing solutions like StreamJsonRpc 
/// are out. If at some point there is a standard RPC mechanism exposed in .NET or Source Build, we should delete this and use that instead.
/// </remarks>
internal sealed class RpcServer
{
    private readonly TextWriter _sendingStream;
    private readonly SemaphoreSlim _sendingStreamSemaphore = new SemaphoreSlim(initialCount: 1);
    private readonly TextReader _receivingStream;

    private readonly ConcurrentDictionary<int, object> _rpcTargets = [];
    private volatile int _nextRpcTargetIndex = -1; // We'll start at -1 so the first value becomes zero

    private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

    public RpcServer(Stream sendingStream, Stream receivingStream)
    {
        _sendingStream = new StreamWriter(sendingStream, JsonSettings.StreamEncoding);
        _receivingStream = new StreamReader(receivingStream, JsonSettings.StreamEncoding);
    }

    public int AddTarget(object rpcTarget)
    {
        // Loop until we successfully have a new index for this; practically we don't expect this to ever collide, since that'd mean we'd have
        // billions of long lived projects, but...
        while (true)
        {
            var nextIndex = Interlocked.Increment(ref _nextRpcTargetIndex);
            if (_rpcTargets.TryAdd(nextIndex, rpcTarget))
                return nextIndex;
        }
    }

    /// <summary>
    /// Runs the server, waiting for responses. The task is completed when the receiving stream closes (and thus no more requests can come in), or
    /// <see cref="Shutdown"/> is called.
    /// </summary>
    public async Task RunAsync()
    {
        var runningTasks = new ConcurrentSet<Task>();

        string? line;
        while ((line = await _receivingStream.TryReadLineOrReturnNullIfCancelledAsync(_shutdownTokenSource.Token).ConfigureAwait(false)) != null)
        {
            Request? request;

            try
            {
                request = JsonConvert.DeserializeObject<Request>(line);
                Contract.ThrowIfNull(request);
            }
            catch (Exception e)
            {
                throw new Exception($"Failure while deserializing '{line}'", innerException: e);
            }

            var runningTask = Task.Run(() => ProcessRequestAsync(request));

            // We'll add this task to the list of running tasks, and then create a continuation to remove it from the list again; this ensures
            // that we won't try to remove it before it was added in case the task completed by the time we got here.
            runningTasks.Add(runningTask);
            _ = runningTask.ContinueWith(
                _ => Contract.ThrowIfFalse(runningTasks.Remove(runningTask)),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        // Wait until all outstanding requests are processed; we however first must copy this into a list safely
        // since the collection might get modified while we're calling Task.WhenAll. The problem is (as of this writing)
        // ConcurrentSet implements ICollection, and all the common helpers like EnumerableExtension.ToArray(), ToList(),
        // etc. all have an optimization where if the IEnumerable implements ICollection, the helpers ask for the count
        // and pre-allocate an array. But if the collection then gets smaller, the array is never resized and so you'll end
        // up with nulls. See the comment at https://github.com/dotnet/runtime/blob/46c8a668eb4bbc66d9eb988d2988ecc84074be10/src/libraries/Common/src/System/Collections/Generic/EnumerableHelpers.cs#L29-L34
        // for example of this concern.
        var remainingTasks = new List<Task>(capacity: runningTasks.Count);
        foreach (var task in runningTasks)
            remainingTasks.Add(task);

        await Task.WhenAll(remainingTasks).ConfigureAwait(false);
    }

    private async Task ProcessRequestAsync(Request request)
    {
        Response response;

        try
        {
            Contract.ThrowIfFalse(
                _rpcTargets.TryGetValue(request.TargetObject, out var rpcTarget),
                $"Received a request for target object {request.TargetObject} but we don't have a registered object for that.");

            var method = rpcTarget.GetType().GetMethod(request.Method, BindingFlags.Public | BindingFlags.Instance);

            Contract.ThrowIfNull(method, $"The invoked method '{request.Method}' could not be found.");

            var methodParameters = method.GetParameters();

            var lastParameterIsCancellationToken = methodParameters.Length > 0 && methodParameters[^1].ParameterType == typeof(CancellationToken);

            if (lastParameterIsCancellationToken)
                Contract.ThrowIfFalse(request.Parameters.Length == methodParameters.Length - 1, $"The arguments list should contain every parameter for {request.Method} except the final CancellationToken.");
            else
                Contract.ThrowIfFalse(request.Parameters.Length == methodParameters.Length, $"The arguments list should contain every parameter for {request.Method}.");

            var arguments = new object?[methodParameters.Length];

            for (var i = 0; i < methodParameters.Length; i++)
            {
                // If the method we're calling accepts a cancellation token, we wouldn't have passed that, so add in a CancellationToken.None here. Although we could
                // remove the cancellation from the underlying method, this keeps that support around should we need it.
                if (i == methodParameters.Length - 1 && lastParameterIsCancellationToken)
                    arguments[i] = CancellationToken.None;
                else
                    arguments[i] = request.Parameters[i].ToObject(methodParameters[i].ParameterType);
            }

            var result = method.Invoke(rpcTarget, arguments);

            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                // If it's actually a Task<T> then get the result; we're looking at the declared return type because in some cases a method might
                // return a Task<T> under the covers as a workaround for the lack of TaskCompletionSource on .NET Framework but we don't want to see
                // that workaround since the result isn't intended to be seen.
                if (method.ReturnType.IsConstructedGenericType)
                {
                    result = task.GetType().GetProperty("Result")!.GetValue(task);
                }
                else
                {
                    // It's just a simple Task so no result to return
                    result = null;
                }
            }

            response = new Response { Id = request.Id, Value = result is not null ? JToken.FromObject(result) : null };
        }
        catch (Exception e)
        {
            if (e is TargetInvocationException)
                e = e.InnerException ?? e;

            response = new Response { Id = request.Id, Exception = $"An exception of type {e.GetType()} was thrown: {e.Message}" };
        }

        var responseJson = JsonConvert.SerializeObject(response, JsonSettings.SingleLineSerializerSettings);

#if DEBUG
        // Assert we didn't put a newline in this, since if we did the receiving side won't know how to parse it
        Contract.ThrowIfTrue(responseJson.Contains("\r") || responseJson.Contains("\n"));
#endif
        using (await _sendingStreamSemaphore.DisposableWaitAsync().ConfigureAwait(false))
        {
            await _sendingStream.WriteLineAsync(responseJson).ConfigureAwait(false);
            await _sendingStream.FlushAsync().ConfigureAwait(false);
        }
    }

    public void Shutdown()
    {
        _shutdownTokenSource.Cancel();
    }
}
