// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        private readonly struct QueueItem
        {
            /// <summary>
            /// Callback to call into underlying <see cref="IRequestHandler"/> to perform the actual work of this item.
            /// </summary>
            private readonly Func<RequestContext?, CancellationToken, Task> _callbackAsync;

            /// <summary>
            /// <see cref="CorrelationManager.ActivityId"/> used to properly correlate this work with the loghub
            /// tracing/logging subsystem.
            /// </summary>
            public readonly Guid ActivityId;
            private readonly ILspLogger _logger;

            /// <inheritdoc cref="IRequestHandler.MutatesSolutionState" />
            public readonly bool MutatesSolutionState;

            /// <inheritdoc cref="IRequestHandler.RequiresLSPSolution" />
            public readonly bool RequiresLSPSolution;

            /// <inheritdoc cref="RequestContext.ClientName" />
            public readonly string? ClientName;
            public readonly string MethodName;

            /// <inheritdoc cref="RequestContext.ClientCapabilities" />
            public readonly ClientCapabilities ClientCapabilities;

            /// <summary>
            /// The document identifier that will be used to find the solution and document for this request. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType}.GetTextDocumentIdentifier(RequestType)"/>.
            /// </summary>
            public readonly TextDocumentIdentifier? TextDocument;

            /// <summary>
            /// A cancellation token that will cancel the handing of this request. The request could also be cancelled by the queue shutting down.
            /// </summary>
            public readonly CancellationToken CancellationToken;

            /// <summary>
            /// An action to be called when the queue fails to begin execution of this work item.
            /// </summary>
            public readonly Action<Exception> HandleQueueFailure;

            public readonly RequestMetrics Metrics;

            private QueueItem(
                bool mutatesSolutionState,
                bool requiresLSPSolution,
                ClientCapabilities clientCapabilities,
                string? clientName,
                string methodName,
                TextDocumentIdentifier? textDocument,
                Guid activityId,
                ILspLogger logger,
                RequestTelemetryLogger telemetryLogger,
                Action<Exception> handleQueueFailure,
                Func<RequestContext?, CancellationToken, Task> callbackAsync,
                CancellationToken cancellationToken)
            {
                Metrics = new RequestMetrics(methodName, telemetryLogger);

                _callbackAsync = callbackAsync;
                _logger = logger;

                ActivityId = activityId;
                MutatesSolutionState = mutatesSolutionState;
                RequiresLSPSolution = requiresLSPSolution;
                ClientCapabilities = clientCapabilities;
                ClientName = clientName;
                MethodName = methodName;
                TextDocument = textDocument;
                HandleQueueFailure = handleQueueFailure;
                CancellationToken = cancellationToken;
            }

            public static (QueueItem queueItem, Task<TResult?> result) Create<TResult>(
                bool mutatesSolutionState,
                bool requiresLSPSolution,
                ClientCapabilities clientCapabilities,
                string? clientName,
                string methodName,
                TextDocumentIdentifier? textDocument,
                Guid activityId,
                ILspLogger logger,
                RequestTelemetryLogger telemetryLogger,
                Func<RequestContext, CancellationToken, Task<TResult>> callbackAsync,
                CancellationToken cancellationToken)
            {
                // Create a task completion source that will represent the processing of this request to the caller
                var completion = new TaskCompletionSource<TResult?>();

                // Note: If the queue is not accepting any more items then TryEnqueue below will fail.
                var item = new QueueItem(
                    mutatesSolutionState,
                    requiresLSPSolution,
                    clientCapabilities,
                    clientName,
                    methodName,
                    textDocument,
                    activityId,
                    logger,
                    telemetryLogger,
                    handleQueueFailure: exception => completion.TrySetException(exception),
                    callbackAsync: async (context, cancellationToken) =>
                    {
                        // Check if cancellation was requested while this was waiting in the queue
                        if (cancellationToken.IsCancellationRequested)
                        {
                            completion.SetCanceled();

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
                            completion.SetResult(default);
                            return;
                        }

                        try
                        {
                            var result = await callbackAsync(context.Value, cancellationToken).ConfigureAwait(false);
                            completion.SetResult(result);
                        }
                        catch (OperationCanceledException ex)
                        {
                            completion.TrySetCanceled(ex.CancellationToken);
                        }
                        catch (Exception exception)
                        {
                            // Pass the exception to the task completion source, so the caller of the ExecuteAsync method can react
                            completion.SetException(exception);

                            // Also allow the exception to flow back to the request queue to handle as appropriate
                            throw new InvalidOperationException($"Error handling '{methodName}' request: {exception.Message}", exception);
                        }
                    },
                    cancellationToken);

                return (item, completion.Task);
            }

            /// <summary>
            /// Processes the queued request. Exceptions that occur will be sent back to the requesting client, then re-thrown
            /// </summary>
            public async Task CallbackAsync(RequestContext? context, CancellationToken cancellationToken)
            {
                // Restore our activity id so that logging/tracking works.
                Trace.CorrelationManager.ActivityId = ActivityId;
                _logger.TraceStart($"{MethodName} - Roslyn");
                try
                {
                    await _callbackAsync(context, cancellationToken).ConfigureAwait(false);
                    this.Metrics.RecordSuccess();
                }
                catch (OperationCanceledException)
                {
                    _logger.TraceInformation($"{MethodName} - Canceled");
                    this.Metrics.RecordCancellation();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.TraceException(ex);
                    this.Metrics.RecordFailure();
                    throw;
                }
                finally
                {
                    _logger.TraceStop($"{MethodName} - Roslyn");
                }
            }
        }
    }
}
