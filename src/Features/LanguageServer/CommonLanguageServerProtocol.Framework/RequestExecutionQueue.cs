// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using System.Collections.Immutable;

namespace CommonLanguageServerProtocol.Framework;

/// <summary>
/// Coordinates the exectution of LSP messages to ensure correct results are sent back.
/// </summary>
/// <remarks>
/// <para>
/// When a request comes in for some data the handler must be able to access a solution state that is correct
/// at the time of the request, that takes into account any text change requests that have come in  previously
/// (via textDocument/didChange for example).
/// </para>
/// <para>
/// This class acheives this by distinguishing between mutating and non-mutating requests, and ensuring that
/// when a mutating request comes in, its processing blocks all subsequent requests. As each request comes in
/// it is added to a queue, and a queue item will not be retrieved while a mutating request is running. Before
/// any request is handled the solution state is created by merging workspace solution state, which could have
/// changes from non-LSP means (eg, adding a project reference), with the current "mutated" state.
/// When a non-mutating work item is retrieved from the queue, it is given the current solution state, but then
/// run in a fire-and-forget fashion.
/// </para>
/// <para>
/// Regardless of whether a request is mutating or not, or blocking or not, is an implementation detail of this class
/// and any consumers observing the results of the task returned from
/// <see cref="ExecuteAsync{TRequestType, TResponseType}(TRequestType, string, ILspServices, CancellationToken)"/>
/// will see the results of the handling of the request, whenever it occurred.
/// </para>
/// <para>
/// Exceptions in the handling of non-mutating requests are sent back to callers. Exceptions in the processing of
/// the queue will close the LSP connection so that the client can reconnect. Exceptions in the handling of mutating
/// requests will also close the LSP connection, as at that point the mutated solution is in an unknown state.
/// </para>
/// <para>
/// After shutdown is called, or an error causes the closing of the connection, the queue will not accept any
/// more messages, and a new queue will need to be created.
/// </para>
/// </remarks>
internal class RequestExecutionQueue<RequestContextType> : IRequestExecutionQueue<RequestContextType>
{
    protected readonly ILspLogger _logger;

    /// <summary>
    /// The queue containing the ordered LSP requests along with a combined cancellation token
    /// representing the queue's cancellation token and the individual request cancellation token.
    /// </summary>
    protected readonly AsyncQueue<(IQueueItem<RequestContextType> queueItem, CancellationToken cancellationToken)> _queue = new();

    /// <summary>
    /// For test purposes only.
    /// A task that completes when the queue processing stops.
    /// </summary>
    protected Task? _queueProcessingTask;

    public CancellationToken CancellationToken => _cancelSource.Token;

    /// <inheritdoc cref="IRequestExecutionQueue{RequestContextType}.RequestServerShutdown"/>
    public event EventHandler<RequestShutdownEventArgs>? RequestServerShutdown;

    public RequestExecutionQueue(ILspLogger logger, IHandlerProvider handlerProvider)
    {
    }

    public void Start(ILspServices lspServices)
    {
    }

    /// <summary>
    /// Queues a request to be handled by the specified handler, with mutating requests blocking subsequent requests
    /// from starting until the mutation is complete.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="methodName">The name of the LSP method.</param>
    /// <param name="requestCancellationToken">A cancellation token that will cancel the handing of this request.
    /// The request could also be cancelled by the queue shutting down.</param>
    /// <returns>A task that can be awaited to observe the results of the handing of this request.</returns>
    public Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(
        TRequestType request,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
    }

    protected IRequestContextFactory<RequestContextType> GetRequestContextFactory(ILspServices lspServices)
    {
        return lspServices.GetRequiredService<IRequestContextFactory<RequestContextType>>();
    }

    /// <summary>
    /// Shuts down the queue, stops accepting new messages, and cancels any in-progress or queued tasks.
    /// </summary>
    public ValueTask DisposeAsync()
    {
    }
}
