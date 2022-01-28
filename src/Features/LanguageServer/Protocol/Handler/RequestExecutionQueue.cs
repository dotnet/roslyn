// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
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
    /// and any consumers observing the results of the task returned from <see cref="ExecuteAsync{TRequestType, TResponseType}(bool, bool, IRequestHandler{TRequestType, TResponseType}, TRequestType, ClientCapabilities, string?, string, CancellationToken)"/>
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
    internal partial class RequestExecutionQueue
    {
        private readonly WellKnownLspServerKinds _serverKind;
        private readonly ImmutableArray<string> _supportedLanguages;

        /// <summary>
        /// The queue containing the ordered LSP requests along with a combined cancellation token
        /// representing the queue's cancellation token and the individual request cancellation token.
        /// </summary>
        private readonly AsyncQueue<(IQueueItem queueItem, CancellationToken cancellationToken)> _queue = new();
        private readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
        private readonly RequestTelemetryLogger _requestTelemetryLogger;
        private readonly IGlobalOptionService _globalOptions;

        private readonly ILspLogger _logger;
        private readonly LspWorkspaceManager _lspWorkspaceManager;

        /// <summary>
        /// For test purposes only.
        /// A task that completes when the queue processing stops.
        /// </summary>
        private readonly Task _queueProcessingTask;

        public CancellationToken CancellationToken => _cancelSource.Token;

        /// <summary>
        /// Raised when the execution queue has failed, or the solution state its tracking is in an unknown state
        /// and so the only course of action is to shutdown the server so that the client re-connects and we can
        /// start over again.
        /// </summary>
        /// <remarks>
        /// Once this event has been fired all currently active and pending work items in the queue will be cancelled.
        /// </remarks>
        public event EventHandler<RequestShutdownEventArgs>? RequestServerShutdown;

        public RequestExecutionQueue(
            ILspLogger logger,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            LspMiscellaneousFilesWorkspace? lspMiscellaneousFilesWorkspace,
            IGlobalOptionService globalOptions,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
        {
            _logger = logger;
            _globalOptions = globalOptions;
            _supportedLanguages = supportedLanguages;
            _serverKind = serverKind;

            // Pass the language client instance type name to the telemetry logger to ensure we can
            // differentiate between the different C# LSP servers that have the same client name.
            // We also don't use the language client's name property as it is a localized user facing string
            // which is difficult to write telemetry queries for.
            _requestTelemetryLogger = new RequestTelemetryLogger(_serverKind.ToTelemetryString());

            _lspWorkspaceManager = new LspWorkspaceManager(logger, lspMiscellaneousFilesWorkspace, lspWorkspaceRegistrationService, _requestTelemetryLogger);

            // Start the queue processing
            _queueProcessingTask = ProcessQueueAsync();
        }

        /// <summary>
        /// Shuts down the queue, stops accepting new messages, and cancels any in-progress or queued tasks. Calling
        /// this multiple times won't cause any issues.
        /// </summary>
        public void Shutdown()
        {
            _cancelSource.Cancel();

            // Tell the queue not to accept any more items.
            // Note: We do not need to spin through the queue manually and cancel items as
            // 1.  New queue instances are created for each server, so items in the queue would be gc'd.
            // 2.  Their cancellation tokens are linked to the queue's _cancelSource so are also cancelled.
            _queue.Complete();

            _requestTelemetryLogger.Dispose();
            _lspWorkspaceManager.Dispose();
        }

        /// <summary>
        /// Queues a request to be handled by the specified handler, with mutating requests blocking subsequent requests
        /// from starting until the mutation is complete.
        /// </summary>
        /// <param name="mutatesSolutionState">Whether or not handling this method results in changes to the current solution state.
        /// Mutating requests will block all subsequent requests from starting until after they have
        /// completed and mutations have been applied.</param>
        /// <param name="requiresLSPSolution">Whether or not to build a solution that represents the LSP view of the world. If this
        /// is set to false, the default workspace's current solution will be used.</param>
        /// <param name="handler">The handler that will handle the request.</param>
        /// <param name="request">The request to handle.</param>
        /// <param name="clientCapabilities">The client capabilities.</param>
        /// <param name="clientName">The client name.</param>
        /// <param name="methodName">The name of the LSP method.</param>
        /// <param name="requestCancellationToken">A cancellation token that will cancel the handing of this request.
        /// The request could also be cancelled by the queue shutting down.</param>
        /// <returns>A task that can be awaited to observe the results of the handing of this request.</returns>
        public Task<TResponseType?> ExecuteAsync<TRequestType, TResponseType>(
            bool mutatesSolutionState,
            bool requiresLSPSolution,
            IRequestHandler<TRequestType, TResponseType> handler,
            TRequestType request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            string methodName,
            CancellationToken requestCancellationToken)
            where TRequestType : class
        {
            // Note: If the queue is not accepting any more items then TryEnqueue below will fail.

            var textDocument = handler.GetTextDocumentIdentifier(request);

            // Create a combined cancellation token so either the client cancelling it's token or the queue
            // shutting down cancels the request.
            var combinedTokenSource = _cancelSource.Token.CombineWith(requestCancellationToken);
            var combinedCancellationToken = combinedTokenSource.Token;

            var (item, resultTask) = QueueItem<TRequestType, TResponseType>.Create(
                mutatesSolutionState,
                requiresLSPSolution,
                clientCapabilities,
                clientName,
                methodName,
                textDocument,
                request,
                handler,
                Trace.CorrelationManager.ActivityId,
                _logger,
                _requestTelemetryLogger,
                combinedCancellationToken);

            var didEnqueue = _queue.TryEnqueue((item, combinedCancellationToken));

            // If the queue has been shut down the enqueue will fail, so we just fault the task immediately.
            // The queue itself is threadsafe (_queue.TryEnqueue and _queue.Complete use the same lock).
            if (!didEnqueue)
            {
                return Task.FromException<TResponseType?>(new InvalidOperationException($"{_serverKind.ToUserVisibleString()} was requested to shut down."));
            }

            return resultTask;
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (!_cancelSource.IsCancellationRequested)
                {

                    // First attempt to de-queue the work item in its own try-catch.
                    // This is because before we de-queue we do not have access to the queue item's linked cancellation token.
                    (IQueueItem work, CancellationToken cancellationToken) queueItem;
                    try
                    {
                        queueItem = await _queue.DequeueAsync(_cancelSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex) when (ex.CancellationToken == _cancelSource.Token)
                    {
                        // The queue's cancellation token was invoked which means we are shutting down the queue.
                        // Exit out of the loop so we stop processing new items.
                        return;
                    }

                    try
                    {
                        var (work, cancellationToken) = queueItem;
                        // Record when the work item was been de-queued and the request context preparation started.
                        work.Metrics.RecordExecutionStart();

                        // Restore our activity id so that logging/tracking works across asynchronous calls.
                        Trace.CorrelationManager.ActivityId = work.ActivityId;
                        var context = CreateRequestContext(work);

                        if (work.MutatesSolutionState)
                        {
                            // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
                            // Since we're explicitly awaiting exceptions to mutating requests will bubble up here.
                            await work.CallbackAsync(context, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // Non mutating are fire-and-forget because they are by definition readonly. Any errors
                            // will be sent back to the client but we can still capture errors in queue processing
                            // via NFW, though these errors don't put us into a bad state as far as the rest of the queue goes.
                            // Furthermore we use Task.Run here to protect ourselves against synchronous execution of work
                            // blocking the request queue for longer periods of time (it enforces parallelizabilty).
                            _ = Task.Run(() => work.CallbackAsync(context, cancellationToken), cancellationToken).ReportNonFatalErrorAsync();
                        }
                    }
                    catch (OperationCanceledException ex) when (ex.CancellationToken == queueItem.cancellationToken)
                    {
                        // Explicitly ignore this exception as cancellation occured as a result of our linked cancellation token.
                        // This means either the queue is shutting down or the request itself was cancelled.
                        //   1.  If the queue is shutting down, then while loop will exit before the next iteration since it checks for cancellation.
                        //   2.  Request cancellations are normal so no need to report anything there.
                    }
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
                // We encountered an unexpected exception in processing the queue or in a mutating request.
                // Log it, shutdown the queue, and exit the loop.
                _logger.TraceException(ex);
                OnRequestServerShutdown($"Error occurred processing queue in {_serverKind.ToUserVisibleString()}: {ex.Message}.");
                return;
            }
        }

        private void OnRequestServerShutdown(string message)
        {
            RequestServerShutdown?.Invoke(this, new RequestShutdownEventArgs(message));

            Shutdown();
        }

        private RequestContext? CreateRequestContext(IQueueItem queueItem)
        {
            var trackerToUse = queueItem.MutatesSolutionState
                ? (IDocumentChangeTracker)_lspWorkspaceManager
                : new NonMutatingDocumentChangeTracker();

            return RequestContext.Create(
                queueItem.RequiresLSPSolution,
                queueItem.TextDocument,
                queueItem.ClientName,
                _logger,
                queueItem.ClientCapabilities,
                _lspWorkspaceManager,
                trackerToUse,
                _supportedLanguages,
                _globalOptions);
        }
    }
}
