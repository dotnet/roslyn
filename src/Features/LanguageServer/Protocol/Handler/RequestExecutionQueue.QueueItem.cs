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
        private interface IQueueItem
        {
            /// <summary>
            /// Begins executing the work specified by this queue item.
            /// </summary>
            Task CallbackAsync(RequestContext? context, CancellationToken cancellationToken);

            /// <inheritdoc cref="IRequestHandler.RequiresLSPSolution" />
            bool RequiresLSPSolution { get; }

            /// <inheritdoc cref="IRequestHandler.MutatesSolutionState" />
            bool MutatesSolutionState { get; }

            string MethodName { get; }

            /// <summary>
            /// The document identifier that will be used to find the solution and document for this request. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType}.GetTextDocumentIdentifier(RequestType)"/>.
            /// </summary>
            TextDocumentIdentifier? TextDocument { get; }

            /// <inheritdoc cref="RequestContext.ClientCapabilities" />
            ClientCapabilities ClientCapabilities { get; }

            /// <summary>
            /// <see cref="CorrelationManager.ActivityId"/> used to properly correlate this work with the loghub
            /// tracing/logging subsystem.
            /// </summary>
            Guid ActivityId { get; }

            RequestMetrics Metrics { get; }
        }

        private class QueueItem<TRequestType, TResponseType> : IQueueItem
        {
            private readonly ILspLogger _logger;

            private readonly TRequestType _request;
            private readonly IRequestHandler<TRequestType, TResponseType> _handler;

            /// <summary>
            /// A task completion source representing the result of this queue item's work.
            /// This is the task that the client is waiting on.
            /// </summary>
            private readonly TaskCompletionSource<TResponseType?> _completionSource = new();

            public bool RequiresLSPSolution { get; }

            public bool MutatesSolutionState { get; }

            public string MethodName { get; }

            public TextDocumentIdentifier? TextDocument { get; }

            public ClientCapabilities ClientCapabilities { get; }

            public Guid ActivityId { get; }

            public RequestMetrics Metrics { get; }

            public QueueItem(
                bool mutatesSolutionState,
                bool requiresLSPSolution,
                ClientCapabilities clientCapabilities,
                string methodName,
                TextDocumentIdentifier? textDocument,
                TRequestType request,
                IRequestHandler<TRequestType, TResponseType> handler,
                Guid activityId,
                ILspLogger logger,
                RequestTelemetryLogger telemetryLogger,
                CancellationToken cancellationToken)
            {
                // Set the tcs state to cancelled if the token gets cancelled outside of our callback (for example the server shutting down).
                cancellationToken.Register(() => _completionSource.TrySetCanceled(cancellationToken));

                Metrics = new RequestMetrics(methodName, telemetryLogger);

                _handler = handler;
                _logger = logger;
                _request = request;

                ActivityId = activityId;
                MutatesSolutionState = mutatesSolutionState;
                RequiresLSPSolution = requiresLSPSolution;
                ClientCapabilities = clientCapabilities;
                MethodName = methodName;
                TextDocument = textDocument;
            }

            public static (IQueueItem, Task<TResponseType?>) Create(
                bool mutatesSolutionState,
                bool requiresLSPSolution,
                ClientCapabilities clientCapabilities,
                string methodName,
                TextDocumentIdentifier? textDocument,
                TRequestType request,
                IRequestHandler<TRequestType, TResponseType> handler,
                Guid activityId,
                ILspLogger logger,
                RequestTelemetryLogger telemetryLogger,
                CancellationToken cancellationToken)
            {
                var queueItem = new QueueItem<TRequestType, TResponseType>(
                    mutatesSolutionState,
                    requiresLSPSolution,
                    clientCapabilities,
                    methodName,
                    textDocument,
                    request,
                    handler,
                    activityId,
                    logger,
                    telemetryLogger,
                    cancellationToken);

                return (queueItem, queueItem._completionSource.Task);
            }

            /// <summary>
            /// Processes the queued request. Exceptions will be sent to the task completion source
            /// representing the task that the client is waiting for, then re-thrown so that
            /// the queue can correctly handle them depending on the type of request.
            /// </summary>
            public async Task CallbackAsync(RequestContext? context, CancellationToken cancellationToken)
            {
                // Restore our activity id so that logging/tracking works.
                Trace.CorrelationManager.ActivityId = ActivityId;
                _logger.TraceStart($"{MethodName} - Roslyn");
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    TResponseType? result;
                    if (context == null)
                    {
                        // If we weren't able to get a corresponding context for this request (for example, we
                        // couldn't map a doc request to a particular Document, or we couldn't find an appropriate
                        // Workspace for a global operation), then just immediately complete the request with a
                        // 'null' response.  Note: the lsp spec was checked to ensure that 'null' is valid for all
                        // the requests this could happen for.  However, this assumption may not hold in the future.
                        // If that turns out to be the case, we could defer to the individual handler to decide
                        // what to do.
                        _logger.TraceWarning($"Could not get request context for {MethodName}");
                        this.Metrics.RecordFailure();
                        result = default;
                    }
                    else
                    {
                        result = await _handler.HandleRequestAsync(_request, context.Value, cancellationToken).ConfigureAwait(false);
                        this.Metrics.RecordSuccess();
                    }

                    _completionSource.TrySetResult(result);
                }
                catch (OperationCanceledException ex)
                {
                    // Record logs + metrics on cancellation.
                    _logger.TraceInformation($"{MethodName} - Canceled");
                    this.Metrics.RecordCancellation();

                    _completionSource.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    // Record logs and metrics on the exception.
                    _logger.TraceException(ex);
                    this.Metrics.RecordFailure();

                    _completionSource.TrySetException(ex);
                }
                finally
                {
                    _logger.TraceStop($"{MethodName} - Roslyn");
                }

                // Return the result of this completion source to the caller
                // so it can decide how to handle the result / exception.
                await _completionSource.Task.ConfigureAwait(false);
            }
        }
    }
}
