// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        private interface QueueItem
        {
            /// <summary>
            /// Begins executing the work specified by this queue item.
            /// </summary>
            public Task CallbackAsync(RequestContext? context, CancellationToken cancellationToken);

            /// <summary>
            /// Sets the result of this queue item to an exception.  Used if we fail to enqueue this item before it starts.
            /// </summary>
            public bool TrySetException(Exception e);

            /// <summary>
            /// Cancels this queue item if possible.  Used when the queue is shutting down and needs to be cleared.
            /// </summary>
            public bool TrySetCanceled(CancellationToken cancellationToken);

            /// <inheritdoc cref="IRequestHandler.RequiresLSPSolution" />
            public bool RequiresLSPSolution { get; }

            /// <inheritdoc cref="IRequestHandler.MutatesSolutionState" />
            public bool MutatesSolutionState { get; }

            /// <inheritdoc cref="RequestContext.ClientName" />
            public string? ClientName { get; }

            public string MethodName { get; }

            /// <summary>
            /// The document identifier that will be used to find the solution and document for this request. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType}.GetTextDocumentIdentifier(RequestType)"/>.
            /// </summary>
            public TextDocumentIdentifier? TextDocument { get; }

            /// <inheritdoc cref="RequestContext.ClientCapabilities" />
            public ClientCapabilities ClientCapabilities { get; }

            /// <summary>
            /// A cancellation token that represents the client's cancellation token for the request.
            /// </summary>
            public CancellationToken RequestCancellationToken { get; }

            /// <summary>
            /// <see cref="CorrelationManager.ActivityId"/> used to properly correlate this work with the loghub
            /// tracing/logging subsystem.
            /// </summary>
            public Guid ActivityId { get; }

            public RequestMetrics Metrics { get; }
        }

        private readonly struct QueueItem<TRequestType, TResponseType> : QueueItem
        {
            private readonly ILspLogger _logger;

            private readonly TRequestType _request;
            private readonly IRequestHandler<TRequestType, TResponseType> _handler;

            /// <summary>
            /// A task completion source representing the result of this queue item's work.
            /// </summary>
            public TaskCompletionSource<TResponseType?> CompletionSource { get; }

            public bool RequiresLSPSolution { get; }

            public bool MutatesSolutionState { get; }

            public string? ClientName { get; }

            public string MethodName { get; }

            public TextDocumentIdentifier? TextDocument { get; }

            public ClientCapabilities ClientCapabilities { get; }

            public CancellationToken RequestCancellationToken { get; }

            public Guid ActivityId { get; }

            public RequestMetrics Metrics { get; }

            public QueueItem(
                bool mutatesSolutionState,
                bool requiresLSPSolution,
                ClientCapabilities clientCapabilities,
                string? clientName,
                string methodName,
                TextDocumentIdentifier? textDocument,
                TRequestType request,
                IRequestHandler<TRequestType, TResponseType> handler,
                Guid activityId,
                ILspLogger logger,
                RequestTelemetryLogger telemetryLogger,
                CancellationToken requestCancellationToken)
            {
                CompletionSource = new TaskCompletionSource<TResponseType?>();
                Metrics = new RequestMetrics(methodName, telemetryLogger);

                _handler = handler;
                _logger = logger;
                _request = request;

                ActivityId = activityId;
                MutatesSolutionState = mutatesSolutionState;
                RequiresLSPSolution = requiresLSPSolution;
                ClientCapabilities = clientCapabilities;
                ClientName = clientName;
                MethodName = methodName;
                TextDocument = textDocument;
                RequestCancellationToken = requestCancellationToken;
            }

            /// <summary>
            /// Processes the queued request. Exceptions will be sent to the task completion source
            /// representing the task that the client is waiting for, then re-thrown so that
            /// the queue can correctly handle them depending on the type of request.
            /// </summary>
            public async Task CallbackAsync(RequestContext? context, CancellationToken queueCancellationToken)
            {
                // Create a combined cancellation token so either the client cancelling it's token or the queue
                // shutting down cancels the request.
                using var combinedTokenSource = queueCancellationToken.CombineWith(queueCancellationToken);
                var cancellationToken = combinedTokenSource.Token;

                // Restore our activity id so that logging/tracking works.
                Trace.CorrelationManager.ActivityId = ActivityId;
                _logger.TraceStart($"{MethodName} - Roslyn");
                try
                {
                    // Check if cancellation was requested while this was waiting in the queue
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.TraceInformation($"{MethodName} - Canceled while in queue");
                        this.Metrics.RecordCancellation();

                        CompletionSource.SetCanceled();
                        return;
                    }

                    // If we weren't able to get a corresponding context for this request (for example, we
                    // couldn't map a doc request to a particular Document, or we couldn't find an appropriate
                    // Workspace for a global operation), then just immediately complete the request with a
                    // 'null' response.  Note: the lsp spec was checked to ensure that 'null' is valid for all
                    // the requests this could happen for.  However, this assumption may not hold in the future.
                    // If that turns out to be the case, we could defer to the individual handler to decide
                    // what to do.
                    if (context == null)
                    {
                        _logger.TraceWarning($"Could not get request context for {MethodName}");
                        this.Metrics.RecordFailure();

                        CompletionSource.SetResult(default);
                        return;
                    }

                    var result = await _handler.HandleRequestAsync(_request, context.Value, cancellationToken).ConfigureAwait(false);

                    this.Metrics.RecordSuccess();

                    CompletionSource.SetResult(result);
                }
                catch (OperationCanceledException ex)
                {
                    // Record logs + metrics on cancellation.
                    _logger.TraceInformation($"{MethodName} - Canceled");
                    this.Metrics.RecordCancellation();

                    // Only cancel the task completion source so the caller (client) can react.
                    // We don't need cancellation exceptions bubbling up to the request queue.
                    CompletionSource.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    // Record logs and metrics on the exception.
                    _logger.TraceException(ex);
                    this.Metrics.RecordFailure();

                    // Pass the exception to the task completion source, so the caller (client) can react
                    CompletionSource.SetException(ex);

                    // Also allow the exception to flow back to the request queue to handle as appropriate
                    throw new InvalidOperationException($"Error handling '{MethodName}' request: {ex.Message}", ex);
                }
                finally
                {
                    _logger.TraceStop($"{MethodName} - Roslyn");
                }
            }

            public bool TrySetException(Exception e)
            {
                return CompletionSource.TrySetException(e);
            }

            public bool TrySetCanceled(CancellationToken cancellationToken)
            {
                return CompletionSource.TrySetCanceled(cancellationToken);
            }
        }
    }
}
